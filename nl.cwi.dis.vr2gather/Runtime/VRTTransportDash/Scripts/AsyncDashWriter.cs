using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
#if !VRT_WITHOUT_STATS
using Statistics = Cwipc.Statistics;
#endif
using Cwipc;
using VRT.Core;

namespace VRT.Transport.Dash
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
    using SyncConfig = Cwipc.SyncConfig;
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;

    public class AsyncDashWriter : AsyncWriter, ITransportProtocolWriter
    {
        static bool initialized = false;
        static public ITransportProtocolWriter Factory()
        {
            if (!initialized)
            {
                initialized = true;
                var version = lldpkg.get_version();
#if !VRT_WITHOUT_STATS
                Statistics.Output("AsyncDashWriter", $"module=lldash-srd-packager, version={version}");
#endif
            }
            return new AsyncDashWriter();
        }

        // Set when the application quits, so the lldash#102 sleep in AsyncOnStop() ends early instead
        // of being aborted by Unity's shutdown (#448).
        static ManualResetEvent applicationQuitting = new ManualResetEvent(false);

        // Also on every entry into Play mode: without a domain reload the event stays set from the last exit.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void WatchApplicationQuitting()
        {
            applicationQuitting.Reset();
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        static void OnApplicationQuitting()
        {
            applicationQuitting.Set();
        }

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public lldpkg.connection lldpkgHandle;
        public string url;
        OutgoingStreamDescription[] descriptions;
        DashStreamPusher[] streamPushers;


        public ITransportProtocolWriter Init(string _url, string userId, string _streamName, string fourcc, OutgoingStreamDescription[] _descriptions)
        {
            lldpkg.LogLevel = VRTConfig.Instance.TransportDashConfig.logLevel;
            _url = TransportProtocolDash.CombineUrl(_url, _streamName, false);

            int _segmentSize = VRTConfig.Instance.TransportDashConfig.segmentSize;
            int _segmentLife = VRTConfig.Instance.TransportDashConfig.segmentLife;
            if (_descriptions == null || _descriptions.Length == 0)
            {
                throw new System.Exception($"{Name()}: descriptions is null or empty");
            }
            if (fourcc.Length != 4)
            {
                throw new System.Exception($"{Name()}: 4CC is \"{fourcc}\" which is not exactly 4 characters");
            }
            uint fourccInt = StreamSupport.VRT_4CC(fourcc[0], fourcc[1], fourcc[2], fourcc[3]);
            descriptions = _descriptions;
            try
            {
                //if (cfg.fileMirroring) bw = new BinaryWriter(new FileStream($"{Application.dataPath}/../{cfg.streamName}.dashdump", FileMode.Create));
                url = _url;
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(_streamName))
                {
                    Debug.LogError($"{Name()}: configuration error: url or streamName not set");
                    throw new System.Exception($"{Name()}: configuration error: url or streamName not set");
                }
                // xxxjack Is this the correct way to initialize an array of structs?
                Debug.Log($"xxxjack {Name()}: {descriptions.Length} output streams");
                DashStreamDescriptor[] b2dDescriptors = new DashStreamDescriptor[descriptions.Length];
                for (int i = 0; i < descriptions.Length; i++)
                {
                    int nx = (int)(descriptions[i].orientation.x * 1000);
                    int ny = (int)(descriptions[i].orientation.y * 1000);
                    int nz = (int)(descriptions[i].orientation.z * 1000);
                    b2dDescriptors[i] = new DashStreamDescriptor
                    {
                        MP4_4CC = fourccInt,
                        tileNumber = descriptions[i].tileNumber,
                        nx = nx,
                        ny = ny,
                        nz = nz
                    };
                    if (descriptions[i].inQueue == null)
                    {
                        throw new System.Exception($"{Name()}.{i}: inQueue");
                    }
                }
                lldpkgHandle = lldpkg.create(_streamName, b2dDescriptors, url, _segmentSize, _segmentLife);
                if (lldpkgHandle != null)
                {
                    Debug.Log($"{Name()}: started {url + _streamName}.mpd");
                    Start();
                }
                else
                    throw new System.Exception($"{Name()}: vrt_create: failed to create uploader {url + _streamName}.mpd");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}({url}) Exception:{e.Message}");
                throw;
            }
            return this;
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }


        protected override void Start()
        {
            // No longer joinTimeout = 999999 ("Dash can be very slow stopping", Dec 2025): with no
            // Thread.Abort that would block the main thread for minutes. If Dash still needs longer than
            // the default, AsyncWorker.StopAndWait() logs it (cwi-dis/VR2Gather#422, cwi-dis/cwipc_unity#88).
            
            int nStreams = descriptions.Length;
            streamPushers = new DashStreamPusher[nStreams];
            for (int i = 0; i < nStreams; i++)
            {
                streamPushers[i] = new DashStreamPusher(this, i, descriptions[i]);
#if !VRT_WITHOUT_STATS
                Statistics.Output(Name(), $"pusher={streamPushers[i].Name()}, tile={descriptions[i].tileNumber}, x={descriptions[i].orientation.x},  y={descriptions[i].orientation.y}, z={descriptions[i].orientation.z}");
#endif
            }
            base.Start();
        }

        public override void AsyncOnStop()
        {
            if (debugThreading) Debug.Log($"{Name()}: Stopping");
            // Signal that no more data is forthcoming to every pusher
            // Our input queues belong to their producers; we don't close them (cwi-dis/cwipc_unity#82, rule 1).
            foreach (var p in streamPushers)
            {
                p.FreeBuffer();
            }
            lldpkgHandle?.free();
            lldpkgHandle = null;
            // Workaround attempt for lldash#102. Not when the application quits: then the process ends
            // anyway, and Unity's shutdown would abort the sleep before base.AsyncOnStop() (#448).
            Debug.Log($"{Name()}: Sleep 4 seconds as workaround for lldash#102");
            if (applicationQuitting.WaitOne(4000))
            {
                Debug.Log($"{Name()}: Application quitting, sleep for lldash#102 cut short");
            }
            base.AsyncOnStop();
        }

        protected override void AsyncUpdate()
        {
            int nStreams = streamPushers.Length;
            bool anyWork = false;
            for (int i = 0; i < nStreams; i++)
            {
                anyWork |= streamPushers[i].LockBuffer();
            }

            if (!anyWork) return;
            for (int i = 0; i < nStreams; i++)
            {
                streamPushers[i].PushBuffer();
            }
        }

        public override SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);

            return new SyncConfig.ClockCorrespondence
            {
                wallClockTime = (Timestamp)sinceEpoch.TotalMilliseconds,
                streamClockTime = lldpkgHandle.get_media_time(0, 1000)
            };
        }

        protected class DashStreamPusher
        {
            AsyncDashWriter parent;
            int stream_index;
            OutgoingStreamDescription description;
            NativeMemoryChunk curBuffer = null;

            public DashStreamPusher(AsyncDashWriter _parent, int _stream_index, OutgoingStreamDescription _description)
            {
                parent = _parent;
                stream_index = _stream_index;
                description = _description;
#if !VRT_WITHOUT_STATS
                stats = new Stats(Name());
#endif
            }

            public string Name()
            {
                return $"{parent.Name()}.{stream_index}";
            }

            public bool LockBuffer()
            {
                lock(this)
                {
                    if (curBuffer != null)
                    {
                        curBuffer.free();
                        curBuffer = null;
                    }
                    curBuffer = (NativeMemoryChunk)description.inQueue.TryDequeue(0);
                    return curBuffer != null;
                }
            }

            /// <summary>
            /// Called when the writer stops: frees the buffer and writes the final stats record (#451).
            /// </summary>
            public void FreeBuffer()
            {
                lock(this)
                {
                    curBuffer?.free();
                    curBuffer = null;
                }
#if !VRT_WITHOUT_STATS
                stats.Final();
#endif
            }

            public void PushBuffer()
            {
                lock(this)
                {
                    if (curBuffer == null) return;
                    bool sent = parent.lldpkgHandle.push_buffer(stream_index, curBuffer.pointer, (uint)curBuffer.length);
                    if (!sent) Debug.LogError($"{Name()}({parent.url}): ERROR sending data");
#if !VRT_WITHOUT_STATS
                    // A frame lldash refused isn't sent: a drop, not counted in n_packets and bytes (#457).
                    if (sent) stats.statsUpdate(curBuffer.length);
                    else stats.statsDropSend();
#endif
                }
            }


#if !VRT_WITHOUT_STATS
            protected class Stats : Statistics
            {
                public Stats(string name) : base(name) { }

                double statsTotalBytes = 0;
                double statsTotalPackets = 0;
                double statsDrops = 0;
                int statsAggregatePackets = 0;

                public void statsUpdate(int nBytes)
                {
                    lock (statsLock)
                    {
                        statsTotalBytes += nBytes;
                        statsTotalPackets++;
                        statsAggregatePackets++;
                        MaybeOutput();
                    }
                }

                /// <summary>
                /// A frame lldash refused to send: a drop (#457).
                /// </summary>
                public void statsDropSend()
                {
                    lock (statsLock)
                    {
                        statsDrops++;
                        OutputDrop(DropReason.Send);
                    }
                }

                protected override void OutputAggregate()
                {
                    OutputFields(
                        Rate("fps", statsTotalPackets),
                        Average("bytes_per_packet", statsTotalBytes, statsTotalPackets, "F0"),
                        Field("aggregate_packets", statsAggregatePackets),
                        Field("n_packets", statsTotalPackets),
                        Field("n_dropped", statsDrops),
                        Field("bytes", statsTotalBytes));
                    statsTotalBytes = 0;
                    statsTotalPackets = 0;
                    statsDrops = 0;
                }
            }

            protected Stats stats;
#endif
        }
    }
}
