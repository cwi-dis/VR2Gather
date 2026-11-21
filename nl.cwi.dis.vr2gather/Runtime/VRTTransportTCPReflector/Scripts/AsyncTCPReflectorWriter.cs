using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using VRT.Core;
using Cwipc;
#if !VRT_WITHOUT_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Transport.TCPReflector
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;

    public class AsyncTCPReflectorWriter : AsyncWriter, ITransportProtocolWriter
    {
        static public ITransportProtocolWriter Factory()
        {
            return new AsyncTCPReflectorWriter();
        }

        OutgoingStreamDescription[] streams;
        private TransportProtocolTCPReflector connection;

        public ITransportProtocolWriter Init(string url, string userId, string streamName, string fourcc, OutgoingStreamDescription[] streams)
        {
            if (streams == null)
            {
                throw new System.Exception($"{Name()}: outQueue is null");
            }
            connection = TransportProtocolTCPReflector.Connect(url);
#if !VRT_WITHOUT_STATS
            stats = new Stats(Name());
#endif
            this.streams = streams;
            for (int i = 0; i < streams.Length; ++i)
            {
                streams[i].name = $"{userId}/{streamName}/{streams[i].tileNumber}";
                connection.RegisterOutgoingStream(streams[i].name);
#if !VRT_WITHOUT_STATS
                Statistics.Output(Name(), $"streamName={streamName}, streamid={i}, tile={streams[i].tileNumber}, orientation={streams[i].orientation}, streamname={streams[i].name}");
#endif
            }
            try
            {
                Start();
                Debug.Log($"{Name()}: Started.");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: Exception: {e.Message}");
                throw;
            }
            return this;
        }

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }


        public override void AsyncOnStop()
        {
            if (debugThreading) Debug.Log($"{Name()}: Stopping");
            // Our input queues belong to their producers; we don't close them (cwi-dis/cwipc_unity#82, rule 1).
            for (int i = 0; i < streams.Length; ++i)
            {
                connection.UnregisterOutgoingStream(streams[i].name);
            }
            base.AsyncOnStop();
        }

        protected override void AsyncUpdate()
        {
            // Never wait on one stream while others may have data: with one stream, wait a short time
            // (cwi-dis/cwipc_unity#82, rule 2); with several, poll them all (as AsyncDashWriter does).
            int timeout = streams.Length == 1 ? DequeueTimeoutMs : 0;
            for (int i = 0; i < streams.Length; ++i)
            {
                BaseMemoryChunk chk = streams[i].inQueue.TryDequeue(timeout);
                if (chk == null) {
                    continue;
                }
                connection.SendChunk(chk, streams[i].name);
                
#if !VRT_WITHOUT_STATS
                stats.statsUpdate(chk.length, i);
#endif
                chk.free();

            }
        }

        public override SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            return new SyncConfig.ClockCorrespondence();
        }

#if !VRT_WITHOUT_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalBytes;
            double statsTotalPackets;
            int statsAggregatePackets;
           
            public void statsUpdate(int nBytes, int streamIndex)
            {
                statsTotalBytes += nBytes;
                statsTotalPackets++;
                statsAggregatePackets++;
                if (ShouldOutput())
                {
                    Output($"fps={statsTotalPackets / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}, last_stream_id={streamIndex}, aggregate_packets={statsAggregatePackets}");
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
