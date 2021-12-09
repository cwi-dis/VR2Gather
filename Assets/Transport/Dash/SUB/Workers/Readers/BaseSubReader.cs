using UnityEngine;
using VRT.Core;
using System.Collections.Generic;

namespace VRT.Transport.Dash
{
    public class BaseReader : BaseWorker
    {
        public BaseReader(WorkerType _type = WorkerType.Run) : base(_type) { }
        public virtual void SetSyncInfo(SyncConfig.ClockCorrespondence _clockCorrespondence)
        {
        }
    }

    public class BaseSubReader : BaseReader
    {

        public delegate bool NeedsSomething();

        protected string url;
        protected int streamCount;
        protected uint[] stream4CCs;
        protected sub.connection subHandle;
        protected bool isPlaying;
        protected int frequency=20;
        int numberOfUnsuccessfulReceives;
        //        object subLock = new object();
        System.DateTime subRetryNotBefore = System.DateTime.Now;
        System.TimeSpan subRetryInterval = System.TimeSpan.FromSeconds(5);

        public class ReceiverInfo
        {
            public QueueThreadSafe outQueue;
            public List<int> streamIndexes;
            public object tileDescriptor;
            public int tileNumber = -1;
            //public int curStreamIndex = -1;
        }
        protected ReceiverInfo[] receivers;
        //        protected QueueThreadSafe[] outQueues;
        //        protected int[] streamIndexes;

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public class SubPullThread
        {
            BaseSubReader parent;
            //            int stream_index;
            //            QueueThreadSafe outQueue;
            int thread_index;
            ReceiverInfo receiverInfo;
            int frequency = 20;
            System.Threading.Thread myThread;
            System.DateTime lastSuccessfulReceive;
            System.TimeSpan maxNoReceives = System.TimeSpan.FromSeconds(15);
            System.TimeSpan receiveInterval = System.TimeSpan.FromMilliseconds(2); // xxxjack maybe too aggressive for PCs and video?

            public SubPullThread(BaseSubReader _parent, int _thread_index, ReceiverInfo _receiverInfo, int _frenquecy)
            {
                parent = _parent;
                thread_index = _thread_index;
                receiverInfo = _receiverInfo;
                frequency = _frenquecy;
                myThread = new System.Threading.Thread(run);
                myThread.Name = Name();
                lastSuccessfulReceive = System.DateTime.Now;
                stats = new Stats(Name());
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
                bool bCanQueue = (frequency==0); // If frequency is 0 enqueue at the very begining
                System.TimeSpan oldElapsed = System.TimeSpan.Zero;
                try
                {
                    while (true)
                    {

                        System.Threading.Thread.Sleep(1); // xxxjack Yield() may be better?
                        //
                        // First check whether we should terminate, and otherwise whether we have nay work to do currently.
                        //
                        if (receiverInfo.outQueue.IsClosed())
                        {
                            return;
                        }

                        sub.connection subHandle = parent.getSubHandle();
                        if (subHandle == null)
                        {
                            Debug.Log($"{Name()}: subHandle was closed, exiting SubPullThread");
                            return;
                        }

                        if (receiverInfo.streamIndexes == null)
                        {
                            continue;
                        }
                        //
                        // We have work to do. 
                        //
                        bool received_anything = false;
                        foreach(int stream_index in receiverInfo.streamIndexes) {
                            FrameInfo frameInfo = new FrameInfo();
                            int bytesNeeded = 0;

                            // See whether data is available on this stream, and how many bytes we need to allocate
                            bytesNeeded = subHandle.grab_frame(stream_index, System.IntPtr.Zero, 0, ref frameInfo);


                            // If no data is available on this stream we try the next
                            if (bytesNeeded == 0)
                            {
                                continue;
                            }
                            received_anything = true;
                            lastSuccessfulReceive = System.DateTime.Now;

                            // Allocate and read.
                            NativeMemoryChunk mc = new NativeMemoryChunk(bytesNeeded);
                            int bytesRead = subHandle.grab_frame(stream_index, mc.pointer, mc.length, ref frameInfo);
         
                            if (bytesRead != bytesNeeded)
                            {
                                Debug.LogError($"{Name()}: programmer error: sub_grab_frame returned {bytesRead} bytes after promising {bytesNeeded}");
                                mc.free();
                                continue;
                            }

                            // If we have no clock correspondence yet we use the first received frame on any stream to set it
                            if (parent.clockCorrespondence.wallClockTime == 0)
                            {
                                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                                parent.clockCorrespondence.wallClockTime = (long)sinceEpoch.TotalMilliseconds;
                                parent.clockCorrespondence.streamClockTime = frameInfo.timestamp;
                                BaseStats.Output(parent.Name(), $"stream_timestamp={parent.clockCorrespondence.streamClockTime}, timestamp={parent.clockCorrespondence.wallClockTime}, delta={parent.clockCorrespondence.wallClockTime - parent.clockCorrespondence.streamClockTime}");
                            }
                            // Convert clock values to wallclock
                            frameInfo.timestamp = frameInfo.timestamp - parent.clockCorrespondence.streamClockTime + parent.clockCorrespondence.wallClockTime;
                            mc.info = frameInfo;

                            bool didDrop = !receiverInfo.outQueue.Enqueue(mc);
                            stats.statsUpdate(bytesRead, didDrop, frameInfo.timestamp, stream_index);

                        }

                        // We no longer need subHandle
                        subHandle.free();

                        // If no data was available on any stream we may want to close the subHandle, or sleep a bit
                        if (!received_anything)
                        {
                            System.TimeSpan noReceives = System.DateTime.Now - lastSuccessfulReceive;
                            if (noReceives > maxNoReceives)
                            {
                                Debug.LogWarning($"{Name()}: No data received for {noReceives.TotalSeconds} seconds, closing subHandle");
                                parent.playFailed();
                                return;
                            }
                            System.Threading.Thread.Sleep(receiveInterval);
                            continue;
                        }
                    }
                }
                catch (System.Exception e)
                {
#if UNITY_EDITOR
                    throw;
#else
                    Debug.Log($"{Name()}: Exception: {e.Message} Stack: {e.StackTrace}");
                    Debug.LogError("Error while receiving visual representation or audio from another participant");
#endif
                }
           }

            protected class Stats : VRT.Core.BaseStats
            {
                public Stats(string name) : base(name) { }

                double statsTotalBytes;
                double statsTotalPackets;
                double statsTotalDrops;
                
                public void statsUpdate(int nBytes, bool didDrop, long timeStamp, int stream_index)
                {
                    statsTotalBytes += nBytes;
                    statsTotalPackets++;
                    if (didDrop) statsTotalDrops++;
                    if (ShouldOutput())
                    {
                        Output($"fps={statsTotalPackets / Interval():F2}, fps_dropped={statsTotalDrops / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}, last_stream_index={stream_index}, last_timestamp={timeStamp}");
                     }
                    if (ShouldClear())
                    {
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalPackets = 0;
                        statsTotalDrops = 0;
                    }
                }
            }

            protected Stats stats;

        }
        SubPullThread[] threads;

        SyncConfig.ClockCorrespondence clockCorrespondence; // Allows mapping stream clock to wall clock

        protected BaseSubReader(string _url, string _streamName, int _frequency=0) : base(WorkerType.Init)
        { // Orchestrator Based SUB
            // closing the SUB may take long. Cater for that.
            lock (this)
            {
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
               
                frequency = _frequency;
            }
        }

        public BaseSubReader(string _url, string _streamName, int streamIndex, QueueThreadSafe outQueue, int _frenquecy=0) : this(_url, _streamName, _frenquecy)
        {
            lock (this)
            {
                receivers = new ReceiverInfo[]
                {
                    new ReceiverInfo()
                    {
                        outQueue = outQueue,
                        streamIndexes = new List<int> {streamIndex}
                    },
                };
                Start();

            }
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public override void Stop()
        {
            if (debugThreading) Debug.Log($"{Name()}: Stop");
            base.Stop();
            _closeQueues();
        }

        public override void OnStop()
        {
            if (debugThreading) Debug.Log($"{Name()}: Stopping");
            _DeinitDash(true);
            base.OnStop();
            if (debugThreading) Debug.Log($"{Name()}: Stopped");
        }

        protected sub.connection getSubHandle()
        {
            lock (this)
            {
                if (!isPlaying) return null;
                return (sub.connection)subHandle.AddRef();
            }
        }


        protected void playFailed()
        {
            lock (this)
            {
                isPlaying = false;
            }
        }

        protected virtual void _streamInfoAvailable()
        {
            lock (this)
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
            }
        }

        protected bool InitDash()
        {
            lock (this)
            {
                if (System.DateTime.Now < subRetryNotBefore)
                {
                    return false;
                }
                subRetryNotBefore = System.DateTime.Now + subRetryInterval;
                //
                // Create SUB instance
                //
                if (subHandle != null)
                {
                    Debug.LogError($"{Name()}: Programmer error: InitDash() called but subHandle != null");
                }
                sub.connection newSubHandle = sub.create(Name());
                if (newSubHandle == null) throw new System.Exception($"{Name()}: sub_create() failed");
                Debug.Log($"{Name()}: retry sub.create() successful.");
                //
                // Start playing
                //
                isPlaying = newSubHandle.play(url);
                if (!isPlaying)
                {
                    subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(5);
                    Debug.Log($"{Name()}: sub.play({url}) failed, will try again later");
                    newSubHandle.free();
                    return false;
                }
                subHandle = newSubHandle;
                //
                // Stream information is available. Allow subclasses to act on it to reconfigure.
                //
                _streamInfoAvailable();
                Debug.Log($"{Name()}: sub.play({url}) successful, {streamCount} streams.");
                return true;
            }
        }

        protected void InitThreads()
        {
            lock (this)
            {
                int threadCount = receivers.Length;
                threads = new SubPullThread[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    threads[i] = new SubPullThread(this, i, receivers[i], frequency);
                    string msg = $"pull_thread={threads[i].Name()}";
                    if (receivers[i].tileNumber >= 0)
                    {
                        msg += $", tile={receivers[i].tileNumber}";
                    }
                    BaseStats.Output(Name(), msg);
                }
                foreach (var t in threads)
                {
                    t.Start();
                }
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

        protected override void Update()
        {
            base.Update();
            bool shouldStop = false;
            lock (this)
            {
                // If we should stop playing we stop

                shouldStop = !isPlaying;
            }
            if (shouldStop) {
                _DeinitDash(false);
            }

            lock (this)
            {
                // If we are not playing we start
                if (subHandle == null)
                {
                    if (InitDash())
                    {
                        InitThreads();
                    }
                }
            }
        }

        public override void SetSyncInfo(SyncConfig.ClockCorrespondence _clockCorrespondence)
        {
            clockCorrespondence = _clockCorrespondence;
            BaseStats.Output(Name(), $"guessed=0, stream_timestamp={clockCorrespondence.streamClockTime}, timestamp={clockCorrespondence.wallClockTime}, delta={clockCorrespondence.wallClockTime - clockCorrespondence.streamClockTime}");
        }
    }
}

