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

   

    public class AsyncDashReader : AsyncReader, ITransportProtocolReader
    {
        protected static bool initialized = false;
        static public ITransportProtocolReader Factory()
        {
            if (!initialized)
            {
                initialized = true;
                var version = lldplay.get_version();
#if VRT_WITH_STATS
                Statistics.Output("AsyncDashReader", $"module=lldash-playout, version={version}");
#endif
            }
            return new AsyncDashReader();
        }

        public delegate bool NeedsSomething();


        protected string url;
        protected int streamCount;
        protected uint[] stream4CCs;
        protected lldplay.connection lldplayHandle;
        protected bool isPlaying;

        public class TileOrMediaInfo
        {
            public QueueThreadSafe outQueue;
            public List<int> streamIndexes;
            public object tileDescriptor;
            public int tileNumber = -1;
            public int currentStreamIndex = 0;
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

            protected void getDataFromStream(int stream_index, int bytesNeeded)
            {
                lldplay.connection lldplayHandle = parent.lldplayHandle;
                lldplay.DashFrameMetaData frameInfo = new lldplay.DashFrameMetaData();

                // Allocate and read.
                NativeMemoryChunk mc = new NativeMemoryChunk(bytesNeeded);
                int bytesRead = lldplayHandle.grab_frame(stream_index, mc.pointer, mc.length, ref frameInfo);

                if (bytesRead != bytesNeeded)
                {
                    Debug.LogError($"{Name()}: programmer error: lldplay.grab_frame returned {bytesRead} bytes after promising {bytesNeeded}");
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
                Timestamp deltaReceivedTimestamp = frameInfo.timestamp - mostRecentDashTimestamp;
                if (deltaReceivedTimestamp < 0)
                {
                    Debug.LogWarning($"{Name()}: received frame with timestamp {frameInfo.timestamp}, previous frame with timestamp {mostRecentDashTimestamp}, delta= {deltaReceivedTimestamp}");
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
                stats.statsUpdate(bytesRead, didDrop, mostRecentDashTimestamp, deltaReceivedTimestamp, network_latency_ms, stream_index);
#endif
            }

            public bool getDataForTile()
            {
                lldplay.connection subHandle = parent.lldplayHandle;
                if (receiverInfo.streamIndexes == null)
                {
                    Debug.LogWarning($"{Name()}: no streamIndexes");
                    return false;
                }
                bool received_anything = false;
                foreach (int stream_index in receiverInfo.streamIndexes)
                {
                    lldplay.DashFrameMetaData frameInfo = new lldplay.DashFrameMetaData();
                    int bytesNeeded = 0;

                    // See whether data is available on this stream, and how many bytes we need to allocate
                    bytesNeeded = subHandle.grab_frame(stream_index, System.IntPtr.Zero, 0, ref frameInfo);
                    // Debug.Log($"{Name()}: xxxjack stream {stream_index}: {bytesNeeded} bytes available");
                    
                    // If no data is available on this stream we try the next
                    if (bytesNeeded == 0)
                    {
                        continue;
                    }
                    received_anything = true;
                    getDataFromStream(stream_index, bytesNeeded);
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

                double statsTotalDelta;
                
                public void statsUpdate(int nBytes, bool didDrop, Timestamp timeStamp, Timestamp timeDelta, Timedelta latency, int stream_index)
                {
                    statsTotalBytes += nBytes;
                    statsTotalDelta += timeDelta;
                    statsTotalPackets++;
                    statsAggregatePackets++;
                    statsTotalLatency += latency;
                    if (didDrop) statsTotalDrops++;
                    if (ShouldOutput())
                    {
                        Output($"fps={statsTotalPackets / Interval():F2}, fps_dropped={statsTotalDrops / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}, network_latency_ms={(int)(statsTotalLatency / statsTotalPackets)}, gap_ms={(int)(statsTotalDelta / statsTotalPackets)}, last_stream_index={stream_index}, last_timestamp={timeStamp}, aggregate_packets={statsAggregatePackets}");
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalDelta = 0;
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
        
        SyncConfig.ClockCorrespondence clockCorrespondence; // Allows mapping stream clock to wall clock
        bool clockCorrespondenceReceived = false;

        protected void _Init(string _url, string _streamName)
        {
            lldplay.LogLevel = VRTConfig.Instance.TransportDash.logLevel;
            _url = TransportProtocolDash.CombineUrl(_url, _streamName, true);
            lock (this)
            {
                joinTimeout = 999999; // xxxjack Dash can be very slow stopping currently (Dec 2025).


                if (_url == "" || _url == null || _streamName == "")
                {
                    Debug.LogError($"{Name()}: configuration error: url or streamName not set");
                    throw new System.Exception($"{Name()}: configuration error: url or streamName not set");
                }
                
                url = _url;
               
            }
        }

        public ITransportProtocolReader Init(string _url, string userId, string _streamName, int streamIndex, string fourcc, QueueThreadSafe outQueue)
        {
            this._Init(_url, _streamName);
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
            return this;
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
            _DeinitLLDPlay(true);
            base.AsyncOnStop();
        }


        
        protected virtual void _streamInfoAvailable()
        {
            lock (this)
            {
                //
                // Get stream information
                //
                streamCount = lldplayHandle.get_stream_count();
                stream4CCs = new uint[streamCount];
                for (int i = 0; i < streamCount; i++)
                {
                    stream4CCs[i] = lldplayHandle.get_stream_4cc(i);
                }
            }
        }

        protected bool InitLLDPlay()
        {
            lock (this)
            {
             
                //
                // Create lldplay instance
                //
                if (lldplayHandle != null)
                {
                    Debug.LogError($"{Name()}: Programmer error: InitLLDPlay() called but lldplayHandle != null");
                }
                lldplayHandle = lldplay.create(Name());
                if (lldplayHandle == null) throw new System.Exception($"{Name()}: lldplay.create() failed");
                Debug.Log($"{Name()}: lldplay.create() successful.");
                //
                // Start playing
                //
                isPlaying = lldplayHandle.play(url);
                if (!isPlaying)
                {
                    
                    return false;
                }
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
                foreach (var t in perTileHandler)
                {
                    t.Start();
                }
            }
        }

        private void _DeinitLLDPlay(bool closeQueues)
        {
            if (closeQueues) _closeQueues();
            lock (this)
            {
                lldplayHandle?.free();
                lldplayHandle = null;
            }
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
            

            lock (this)
            {
                // If we are not playing we start
                if (lldplayHandle == null)
                {
                    InitLLDPlay();
                    {
                        InitThread();
                    }
                }
            }
            handleReceives();
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

        protected bool handleReceives()
        {
            bool received_anything = false;
            for(int i= 0; i < perTileInfo.Length; i++)
            {
                var receiverInfo = perTileInfo[i];
                var receiverHandler = perTileHandler[i];
                if (receiverInfo.outQueue.IsClosed())
                {
                    Debug.Log($"{Name()}: skip tile {i} which is closed");
                    continue;
                }
                    
                //
                // Check whether we have incoming data for this set of streams. 
                //

                if(receiverHandler.getDataForTile())
                {
                    received_anything = true;
                }
            }
            return received_anything;
        }
    }
}

