using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if VRT_WITH_STATS
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

    public class AsyncDashWriter : TransportProtocolWriter
    {

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        bool initialized = false;
        public bin2dash.connection uploader;
        public string url;
        OutgoingStreamDescription[] descriptions;
        B2DPusher[] streamPushers;


        override public TransportProtocolWriter Init(string _url, string _streamName, string fourcc, OutgoingStreamDescription[] _descriptions)
        {
            int _segmentSize = VRTConfig.Instance.TransportDash.segmentSize;
            int _segmentLife = VRTConfig.Instance.TransportDash.segmentLife;
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
                bin2dash.StreamDesc[] b2dDescriptors = new bin2dash.StreamDesc[descriptions.Length];
                for (int i = 0; i < descriptions.Length; i++)
                {
                    int nx = (int)(descriptions[i].orientation.x * 1000);
                    int ny = (int)(descriptions[i].orientation.y * 1000);
                    int nz = (int)(descriptions[i].orientation.z * 1000);
                    b2dDescriptors[i] = new bin2dash.StreamDesc
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
                uploader = bin2dash.create(_streamName, b2dDescriptors, url, _segmentSize, _segmentLife);
                if (uploader != null)
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
            initialized = true;
            return this;
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }


        protected override void Start()
        {
            int nStreams = descriptions.Length;
            streamPushers = new B2DPusher[nStreams];
            for (int i = 0; i < nStreams; i++)
            {
                // Note: we need to copy i to a new variable, otherwise the lambda expression capture will bite us
                int stream_number = i;
                streamPushers[i] = new B2DPusher(this, i, descriptions[i]);
#if VRT_WITH_STATS
                Statistics.Output(Name(), $"pusher={streamPushers[i].Name()}, tile={descriptions[i].tileNumber}, orientation={descriptions[i].orientation}");
#endif
            }
            base.Start();
        }

        public override void AsyncOnStop()
        {
            // Signal that no more data is forthcoming to every pusher
            for (int i = 0; i < descriptions.Length; i++)
            {
                var d = descriptions[i];
                if (!d.inQueue.IsClosed())
                {
                    Debug.LogWarning($"{Name()}.{i}: input queue not closed. Closing.");
                    d.inQueue.Close();
                }
            }
            // Stop our thread
            base.AsyncOnStop();
            uploader?.free();
            uploader = null;
            Debug.Log($"{Name()} {url} Stopped");
        }

        protected override void AsyncUpdate()
        {
            int nStreams = streamPushers.Length;
            for (int i = 0; i < nStreams; i++)
            {
                if (!streamPushers[i].LockBuffer()) Stop();
            }
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
                streamClockTime = uploader.get_media_time(1000)
            };
        }

        protected class B2DPusher
        {
            AsyncDashWriter parent;
            int stream_index;
            OutgoingStreamDescription description;
            NativeMemoryChunk curBuffer = null;

            public B2DPusher(AsyncDashWriter _parent, int _stream_index, OutgoingStreamDescription _description)
            {
                parent = _parent;
                stream_index = _stream_index;
                description = _description;
#if VRT_WITH_STATS
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
                    curBuffer = (NativeMemoryChunk)description.inQueue.Dequeue();
                    return curBuffer != null;
                }
            }

            public void PushBuffer()
            {
                lock(this)
                {
                    if (curBuffer == null) return;
#if VRT_WITH_STATS
                    stats.statsUpdate(curBuffer.length);
#endif
                    if (!parent.uploader.push_buffer(stream_index, curBuffer.pointer, (uint)curBuffer.length))
                        Debug.LogError($"{Name()}({parent.url}): ERROR sending data");
                }
            }


#if VRT_WITH_STATS
            protected class Stats : Statistics
            {
                public Stats(string name) : base(name) { }

                double statsTotalBytes = 0;
                double statsTotalPackets = 0;
                int statsAggregatePackets = 0;

                public void statsUpdate(int nBytes)
                {
 
                    statsTotalBytes += nBytes;
                    statsTotalPackets++;
                    statsAggregatePackets++;

                    if (ShouldOutput())
                    {
                        Output($"fps={statsTotalPackets / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / (statsTotalPackets == 0 ? 1 : statsTotalPackets))}, aggregate_packets={statsAggregatePackets}");
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalPackets = 0;
                    }
                }
            }

            protected Stats stats;
#endif
        }
    }
}
