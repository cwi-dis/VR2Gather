using UnityEngine;

namespace Workers {
    public class BaseSubReader : BaseWorker {

        public delegate bool NeedsSomething();

        protected string url;
        protected int streamCount;
        protected uint[] stream4CCs;
        protected sub.connection subHandle;
        protected bool isPlaying;
        int numberOfUnsuccessfulReceives;
//        object subLock = new object();
        System.DateTime subRetryNotBefore = System.DateTime.Now;
        System.TimeSpan subRetryInterval = System.TimeSpan.FromSeconds(5);

        public struct ReceiverInfo
        {
            public QueueThreadSafe outQueue;
            public int[] streamIndexes;
        }
        protected ReceiverInfo[] receivers;
//        protected QueueThreadSafe[] outQueues;
//        protected int[] streamIndexes;

        // Mainly for debug messages:
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        // xxxjack
        public class SubPullThread
        {
            BaseSubReader parent;
            //            int stream_index;
            //            QueueThreadSafe outQueue;
            int thread_index;
            ReceiverInfo receiverInfo;
            System.Threading.Thread myThread;
            System.DateTime lastSuccessfulReceive;
            System.TimeSpan maxNoReceives = System.TimeSpan.FromSeconds(5);
            System.TimeSpan receiveInterval = System.TimeSpan.FromMilliseconds(2); // xxxjack maybe too aggressive for PCs and video?

            public SubPullThread(BaseSubReader _parent, int _thread_index, ReceiverInfo _receiverInfo)
            {
                parent = _parent;
                thread_index = _thread_index;
                receiverInfo = _receiverInfo;
                myThread = new System.Threading.Thread(run);
                myThread.Name = Name();
                lastSuccessfulReceive = System.DateTime.Now;
            }

            public string Name()
            {
                return $"{parent.Name()}.{thread_index}";
            }

            public void Start()
            {
                myThread.Start();
            }

            public void Join()
            {
                myThread.Join();
            }

            protected void run()
            {
                Debug.Log($"{Name()}: xxxjack thread started, looking at {receiverInfo.streamIndexes.Length} streams");
                try
                {
                    while (true)
                    {
                        System.Threading.Thread.Sleep(1); // xxxjack Yield() may be better?
                        //
                        // First check whether we should terminate, and otherwise whether we have nay work to do currently.
                        //
                        if (receiverInfo.outQueue.IsClosed()) return;

                        sub.connection subHandle = parent.getSubHandle();
                        if (subHandle == null)
                        {
                            Debug.Log($"{Name()}: subHandle was closed, exiting SubPullThread");
                            return;
                        }

                        if (receiverInfo.streamIndexes.Length == 0) continue;
                        //
                        // We have work to do. Check which of our streamIndexes has data available.
                        //
                        sub.FrameInfo frameInfo = new sub.FrameInfo();

                        int stream_index = -1;
                        int bytesNeeded = 0;
                        foreach(int si in receiverInfo.streamIndexes)
                        {
                            // See whether data is available on this stream, and how many bytes we need to allocate
                            bytesNeeded = subHandle.grab_frame(si, System.IntPtr.Zero, 0, ref frameInfo);
                            if (bytesNeeded > 0)
                            {
                                stream_index = si;
                                break;
                            }

                        }

                        // If no data is available we may want to close the subHandle, or sleep a bit
                        if (bytesNeeded == 0)
                        {
                            subHandle.free();
                            System.TimeSpan noReceives = System.DateTime.Now - lastSuccessfulReceive;
                            if (noReceives > maxNoReceives)
                            {
                                Debug.LogWarning($"{Name()}: No data received for {noReceives}, closing subHandle");
                                parent.playFailed();
                                return;
                            }
                            System.Threading.Thread.Sleep(receiveInterval);
                            continue;
                        }

                        lastSuccessfulReceive = System.DateTime.Now;

                        // Allocate and read.
                        NativeMemoryChunk mc = new NativeMemoryChunk(bytesNeeded);
                        int bytesRead = subHandle.grab_frame(stream_index, mc.pointer, mc.length, ref frameInfo);
                        // We no longer need subHandle
                        subHandle.free();

                        if (bytesRead != bytesNeeded)
                        {
                            Debug.LogError($"{Name()}: sub_grab_frame returned {bytesRead} bytes after promising {bytesNeeded}");
                            mc.free();
                            continue;
                        }

                        // Push to queue
                        mc.info = frameInfo;
                        receiverInfo.outQueue.Enqueue(mc);

                        statsUpdate(bytesRead);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"{Name()}: Exception: {e.Message} Stack: {e.StackTrace}");
#if UNITY_EDITOR
                    if (UnityEditor.EditorUtility.DisplayDialog("Exception", "Exception in SubPullThread", "Stop", "Continue"))
                        UnityEditor.EditorApplication.isPlaying = false;
#endif
                }

            }

            System.DateTime statsLastTime;
            double statsTotalBytes;
            double statsTotalPackets;

            public void statsUpdate(int nBytes)
            {
                if (statsLastTime == null)
                {
                    statsLastTime = System.DateTime.Now;
                    statsTotalBytes = 0;
                    statsTotalPackets = 0;
                }
                if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(10))
                {
                    Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: {Name()}: {statsTotalPackets / 10} fps, {(int)(statsTotalBytes / statsTotalPackets)} bytes per packet");
                    statsTotalBytes = 0;
                    statsTotalPackets = 0;
                    statsLastTime = System.DateTime.Now;
                }
                statsTotalBytes += nBytes;
                statsTotalPackets += 1;
            }
        }
        SubPullThread[] threads;

        protected BaseSubReader(string _url, string _streamName, int _initialDelay) : base(WorkerType.Init) { // Orchestrator Based SUB
            // closing the SUB may take long. Cater for that.
            joinTimeout = 20000;

            if (_url == "" || _url == null || _streamName == "")
            {
                Debug.LogError($"{Name()}: configuration error: url or streamName not set");
                throw new System.Exception($"{Name()}: configuration error: url or streamName not set");
            }
            if (_streamName != null)
            {
                if (!_streamName.Contains(".mpd")) _streamName += ".mpd";
                _url += _streamName;
            }
            url = _url;
            if (_initialDelay != 0)
            {
                // We do not try to start play straight away, to work around bugs when creating the SUB before
                // the dash data is stable. To be removed at some point in the future (Jack, 20200123)
                Debug.Log($"{Name()}: Delaying {_initialDelay} seconds before playing {url}");
                subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(_initialDelay);
            }
            Start();
        }

        public override string Name()
        {
            return $"{this.GetType().Name}#{instanceNumber}";
        }

        public override void Stop()
        {
            base.Stop();
            _closeQueues();
        }

        public override void OnStop() {
            if (debugThreading) Debug.Log($"{Name()}: Stopping");
            _DeinitDash(true);
            base.OnStop();
            if (debugThreading) Debug.Log($"{Name()}: Stopped");
        }

        protected sub.connection getSubHandle()
        {
            lock(this)
            {
                if (!isPlaying) return null;
                return (sub.connection)subHandle.AddRef();
            }
        }


        protected void playFailed()
        {
            lock(this)
            {
                isPlaying = false;
            }
        }

        protected virtual void _streamInfoAvailable()
        {
            //
            // Get stream information
            //
            streamCount = subHandle.get_stream_count();
            stream4CCs = new uint[streamCount];
            for (int i = 0; i < streamCount; i++)
            {
                stream4CCs[i] = subHandle.get_stream_4cc(i);
            }
            Debug.Log($"{Name()}: sub.play({url}) successful, {streamCount} streams.");
        }

        protected void InitDash() {
            if (System.DateTime.Now < subRetryNotBefore) return;
            subRetryNotBefore = System.DateTime.Now + subRetryInterval;
            //
            // Create SUB instance
            //
            subHandle = sub.create(Name());
            if (subHandle == null) throw new System.Exception($"{Name()}: sub_create() failed");
            Debug.Log($"{Name()}: retry sub.create() successful.");
            //
            // Start playing
            //
            isPlaying = subHandle.play(url);
            if (!isPlaying) {
                subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(5);
                Debug.Log($"{Name()}: sub.play({url}) failed, will try again later");
                return;
            }
            //
            // Stream information is available. Allow subclasses to act on it to reconfigure.
            //
            _streamInfoAvailable();

            //
            // Start threads
            //
            int threadCount = receivers.Length;
            Debug.Log($"{Name()}: xxxjack starting {threadCount} threads");
            threads = new SubPullThread[threadCount];
            for (int i=0; i<threadCount; i++)
            {
                threads[i] = new SubPullThread(this, i, receivers[i]);
            }
            foreach(var t in threads)
            {
                t.Start();
            }
        }

        private void _DeinitDash(bool closeQueues)
        {
            lock (this)
            {
                subHandle?.free();
                subHandle = null;
                isPlaying = false;
            }
            if (closeQueues) _closeQueues();
            if (threads == null) return;
            foreach (var t in threads)
            {
                t.Join();
            }
            threads = null;
        }

        private void _closeQueues()
        {
            foreach (var r in receivers)
            {
                var oq = r.outQueue;
                if (!oq.IsClosed()) oq.Close();
            }
        }

        protected override void Update() {
            base.Update();
            // If we should stop playing we stop
            if (!isPlaying)
            {
                _DeinitDash(false);
            }
            // If we are not playing we start
            if (subHandle == null)
            {
                InitDash();
            }
        }
    }
}

