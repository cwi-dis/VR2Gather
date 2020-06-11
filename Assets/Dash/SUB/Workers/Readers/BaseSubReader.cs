using UnityEngine;

namespace Workers {
    public class BaseSubReader : BaseWorker {

        public delegate bool NeedsSomething();

        string url;
        protected int streamCount;
        protected uint[] stream4CCs;
        sub.connection subHandle;
        protected bool isPlaying;
        int numberOfUnsuccessfulReceives;
//        object subLock = new object();
        System.DateTime subRetryNotBefore = System.DateTime.Now;
        System.TimeSpan subRetryInterval = System.TimeSpan.FromSeconds(5);

        protected QueueThreadSafe[] outQueues;
        protected int[] streamIndexes;

        // Mainly for debug messages:
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        // xxxjack
        public class SubPullThread
        {
            BaseSubReader parent;
            int stream_index;
            System.Threading.Thread myThread;
            QueueThreadSafe outQueue;
            System.DateTime lastSuccessfulReceive;
            System.TimeSpan maxNoReceives = System.TimeSpan.FromSeconds(5);
            System.TimeSpan receiveInterval = System.TimeSpan.FromMilliseconds(2); // xxxjack maybe too aggressive for PCs and video?

            public SubPullThread(BaseSubReader _parent, int _stream_index, QueueThreadSafe _outQueue)
            {
                parent = _parent;
                stream_index = _stream_index;
                outQueue = _outQueue;
                myThread = new System.Threading.Thread(run);
                myThread.Name = Name();
                lastSuccessfulReceive = System.DateTime.Now;
            }

            public string Name()
            {
                return $"{parent.Name()}.{stream_index}";
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
                try
                {
                    while (true)
                    {
                        sub.FrameInfo info = new sub.FrameInfo();
                        sub.connection subHandle = parent.getSubHandle();
                        // Shouldn't happen, but's let make sure
                        if (subHandle == null)
                        {
                            Debug.Log($"{Name()}: subHandle was closed, exiting SubPullThread");
                            return;
                        }

                        // See whether data is available, and how many bytes we need to allocate
                        int bytesNeeded = subHandle.grab_frame(stream_index, System.IntPtr.Zero, 0, ref info);

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
                        int bytesRead = subHandle.grab_frame(stream_index, mc.pointer, mc.length, ref info);
                        // We no longer need subHandle
                        subHandle.free();

                        if (bytesRead != bytesNeeded)
                        {
                            Debug.LogError($"{Name()}: sub_grab_frame returned {bytesRead} bytes after promising {bytesNeeded}");
                            mc.free();
                            continue;
                        }

                        // Push to queue
                        mc.info = info;
                        outQueue.Enqueue(mc);

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

        public override void OnStop() {
            Debug.Log($"{Name()}: Stopping");
            _DeinitDash(true);
            base.OnStop();
            Debug.Log($"{Name()}: Stopped");
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
            // Get stream information
            //
            streamCount = subHandle.get_stream_count();
            stream4CCs = new uint[streamCount];
            for (int i = 0; i < streamCount; i++)
            {
                stream4CCs[i] = subHandle.get_stream_4cc(i);
            }
            Debug.Log($"{Name()}: sub.play({url}) successful, {streamCount} streams.");
            //
            // Start threads
            //
            int threadCount = streamIndexes.Length;
            threads = new SubPullThread[threadCount];
            for (int i=0; i<threadCount; i++)
            {
                threads[i] = new SubPullThread(this, streamIndexes[i], outQueues[i]);
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
            if (closeQueues)
            {
                foreach (var oq in outQueues)
                {
                    oq.Close();
                }
            }
            if (threads == null) return;
            foreach (var t in threads)
            {
                t.Join();
            }
            threads = null;
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

