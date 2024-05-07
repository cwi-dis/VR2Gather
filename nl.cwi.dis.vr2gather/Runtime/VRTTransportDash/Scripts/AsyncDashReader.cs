using UnityEngine;
using VRT.Core;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using System.Collections.Generic;
using Cwipc;

namespace VRT.Transport.Dash
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using QueueThreadSafe = Cwipc.QueueThreadSafe;

   

    public class AsyncDashReader : AsyncReader
    {

        public delegate bool NeedsSomething();

        protected string url;
        protected int streamCount;
        protected uint[] stream4CCs;
        protected sub.connection subHandle;
        protected bool isPlaying;
        int numberOfUnsuccessfulReceives;
        System.DateTime subRetryNotBefore = System.DateTime.Now;
        System.TimeSpan subRetryInterval = System.TimeSpan.FromSeconds(5);

        public class TileOrMediaInfo
        {
            public QueueThreadSafe outQueue;
            public List<int> streamIndexes;
            public object tileDescriptor;
            public int tileNumber = -1;
        }
        protected TileOrMediaInfo[] perTileInfo;
   
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public class TileOrMediaHandler
        {
            AsyncDashReader parent;
            int handler_index;
            TileOrMediaInfo receiverInfo;
            public Timestamp mostRecentDashTimestamp;


            public TileOrMediaHandler(AsyncDashReader _parent, int _handler_index, TileOrMediaInfo _receiverInfo)
            {
                parent = _parent;
                handler_index = _handler_index;
                receiverInfo = _receiverInfo;
#if VRT_WITH_STATS
                stats = new Stats(Name());
#endif
            }

            public string Name()
            {
                return $"{parent.Name()}.{handler_index}";
            }

            public void Start()
            {
            }

            public void Join()
            {
            }

            protected void getDataFromStream(sub.connection subHandle, int stream_index, int bytesNeeded)
            {
                sub.FrameInfo frameInfo = new sub.FrameInfo();

                // Allocate and read.
                NativeMemoryChunk mc = new NativeMemoryChunk(bytesNeeded);
                int bytesRead = subHandle.grab_frame(stream_index, mc.pointer, mc.length, ref frameInfo);

                if (bytesRead != bytesNeeded)
                {
                    Debug.LogError($"{Name()}: programmer error: sub_grab_frame returned {bytesRead} bytes after promising {bytesNeeded}");
                    mc.free();
                    return;
                }
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                Timestamp now = (Timestamp)sinceEpoch.TotalMilliseconds;

                // If we have no clock correspondence yet we use the first received frame on any stream to set it
                if (parent.clockCorrespondence.wallClockTime == 0)
                {
                    parent.clockCorrespondence.wallClockTime = now;
                    parent.clockCorrespondence.streamClockTime = frameInfo.timestamp;
#if VRT_WITH_STATS
                    Statistics.Output(parent.Name(), $"guessed=1, stream_epoch={parent.clockCorrespondence.wallClockTime - parent.clockCorrespondence.streamClockTime}, stream_timestamp={parent.clockCorrespondence.streamClockTime}, wallclock_timestamp={parent.clockCorrespondence.wallClockTime}");
#endif
                }
                // Convert clock values to wallclock
                mostRecentDashTimestamp = frameInfo.timestamp;
                if (!parent.clockCorrespondenceReceived)
                {
                    Debug.Log($"{Name()}: no sync config received yet, returning guessed timestamp");
                }
                frameInfo.timestamp = frameInfo.timestamp - parent.clockCorrespondence.streamClockTime + parent.clockCorrespondence.wallClockTime;
                // mc.info = frameInfo;
                // xxxjack: I don't know if this is correct: copying frameInfo.dsi without reference to dsi_size.
                mc.metadata = new FrameMetadata()
                {
                    timestamp = frameInfo.timestamp,
                    dsi = frameInfo.dsi,
                    dsi_size = frameInfo.dsi_size
                };
                Timedelta network_latency_ms = now - frameInfo.timestamp;

                bool didDrop = !receiverInfo.outQueue.Enqueue(mc);
#if VRT_WITH_STATS
                stats.statsUpdate(bytesRead, didDrop, mostRecentDashTimestamp, network_latency_ms, stream_index);
#endif
            }

            public bool getDataForTile(sub.connection subHandle)
            {
                if (receiverInfo.streamIndexes == null)
                {
                    return false;
                }
                bool received_anything = false;
                foreach (int stream_index in receiverInfo.streamIndexes)
                {
                    sub.FrameInfo frameInfo = new sub.FrameInfo();
                    int bytesNeeded = 0;

                    // See whether data is available on this stream, and how many bytes we need to allocate
                    bytesNeeded = subHandle.grab_frame(stream_index, System.IntPtr.Zero, 0, ref frameInfo);


                    // If no data is available on this stream we try the next
                    if (bytesNeeded == 0)
                    {
                        continue;
                    }
                    received_anything = true;
                    getDataFromStream(subHandle, stream_index, bytesNeeded);
                }
                return received_anything;
            }

#if VRT_WITH_STATS
            protected class Stats : Statistics
            {
                public Stats(string name) : base(name) { }

                double statsTotalBytes;
                double statsTotalPackets;
                int statsAggregatePackets;
                double statsTotalDrops;
                double statsTotalLatency;
                
                public void statsUpdate(int nBytes, bool didDrop, Timestamp timeStamp, Timedelta latency, int stream_index)
                {
                    statsTotalBytes += nBytes;
                    statsTotalPackets++;
                    statsAggregatePackets++;
                    statsTotalLatency += latency;
                    if (didDrop) statsTotalDrops++;
                    if (ShouldOutput())
                    {
                        Output($"fps={statsTotalPackets / Interval():F2}, fps_dropped={statsTotalDrops / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}, network_latency_ms={(int)(statsTotalLatency / statsTotalPackets)}, last_stream_index={stream_index}, last_timestamp={timeStamp}, aggregate_packets={statsAggregatePackets}");
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalPackets = 0;
                        statsTotalDrops = 0;
                        statsTotalLatency = 0;
                    }
                }
            }

            protected Stats stats;
#endif
        }

        TileOrMediaHandler[] perTileHandler;
        System.Threading.Thread myThread;
        System.TimeSpan maxNoReceives = System.TimeSpan.FromSeconds(15);
        System.TimeSpan receiveInterval = System.TimeSpan.FromMilliseconds(100); // This parameter needs work. 2ms causes jitter with tiled pcs, but 33ms may be too high for audio 

        SyncConfig.ClockCorrespondence clockCorrespondence; // Allows mapping stream clock to wall clock
        bool clockCorrespondenceReceived = false;

        protected AsyncDashReader(string _url, string _streamName) : base()
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
                if (!_url.EndsWith("/")) {
                    _url += "/";
                }
                _url += _streamName;
                if (!_url.EndsWith("/")) {
                    _url += "/";
                }
                url = _url;
               
            }
        }

        public AsyncDashReader(string _url, string _streamName, int streamIndex, string fourcc, QueueThreadSafe outQueue) : this(_url, _streamName)
        {
            lock (this)
            {
                perTileInfo = new TileOrMediaInfo[]
                {
                    new TileOrMediaInfo()
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

        public override void AsyncOnStop()
        {
            if (debugThreading) Debug.Log($"{Name()}: Stopping");
            _DeinitDash(true);
            base.AsyncOnStop();
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

        protected void InitThread()
        {
            lock (this)
            {
                int threadCount = perTileInfo.Length;
                perTileHandler = new TileOrMediaHandler[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    perTileHandler[i] = new TileOrMediaHandler(this, i, perTileInfo[i]);
#if VRT_WITH_STATS
                    string msg = $"pull_thread={perTileHandler[i].Name()}";
                    if (perTileInfo[i].tileNumber >= 0)
                    {
                        msg += $", tile={perTileInfo[i].tileNumber}";
                    }
                    Statistics.Output(Name(), msg);
#endif
                }
                myThread = new System.Threading.Thread(ingestThreadRunner);
                myThread.Name = Name();
                myThread.Start();
                foreach (var t in perTileHandler)
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
            myThread?.Join();
            myThread = null;
            if (perTileHandler == null) return;
            foreach (var t in perTileHandler)
            {
                t.Join();
            }
            perTileHandler = null;
        }

        private void _closeQueues()
        {
            foreach (var r in perTileInfo)
            {
                var oq = r.outQueue;
                if (!oq.IsClosed()) oq.Close();
            }
        }

        protected override void AsyncUpdate()
        {
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
                        InitThread();
                    }
                }
            }
        }

        public override void SetSyncInfo(SyncConfig.ClockCorrespondence _clockCorrespondence)
        {
            Timedelta oldEpoch = 0;
            if (clockCorrespondence.wallClockTime != 0)
            {
                oldEpoch = clockCorrespondence.wallClockTime - clockCorrespondence.streamClockTime;
            }
            clockCorrespondence = _clockCorrespondence;
            Timedelta epoch = clockCorrespondence.wallClockTime - clockCorrespondence.streamClockTime;
            Timedelta delta = 0;
            if (oldEpoch != 0)
            {
                delta = epoch - oldEpoch;
            }
            clockCorrespondenceReceived = true;
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"guessed=0, stream_epoch_delta_ms={delta}, stream_epoch={epoch}, stream_timestamp={clockCorrespondence.streamClockTime}, wallclock_timestamp={clockCorrespondence.wallClockTime}");
#endif
        }

        protected void ingestThreadRunner()
        {
            System.DateTime lastSuccessfulReceive = System.DateTime.Now;
            try
            {
                while (true)
                {
                    bool received_anything = false;
                    for(int i= 0; i < perTileInfo.Length; i++)
                    {
                        var receiverInfo = perTileInfo[i];
                        var receiverHandler = perTileHandler[i];
                        if (receiverInfo.outQueue.IsClosed())
                        {
                            continue;
                        }
                        if (subHandle == null)
                        {
                            Debug.Log($"{Name()}: subHandle was closed, exiting run thread");
                            return;
                        }
                        //
                        // Check whether we have incoming data for this set of streams. 
                        //

                        if(receiverHandler.getDataForTile(subHandle))
                        {
                            Debug.Log($"{Name()}: xxxjack tile {i} received {receiverHandler.mostRecentDashTimestamp}");
                            received_anything = true;
                            lastSuccessfulReceive = System.DateTime.Now;
                        }

                    }

                    // If no data was available on any stream we may want to close the subHandle, or sleep a bit
                    if (!received_anything)
                    {
                        System.TimeSpan noReceives = System.DateTime.Now - lastSuccessfulReceive;
                        if (noReceives > maxNoReceives)
                        {
                            Debug.LogWarning($"{Name()}: No data received for {noReceives.TotalSeconds} seconds, closing subHandle");
                            playFailed();
                            return;
                        }
                        System.Threading.Thread.Sleep(receiveInterval);
                        Debug.Log($"{Name()}: xxxjack no data sleep({receiveInterval}");
                        continue;
                    }
                }
            }
#pragma warning disable CS0168
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
    }
}

