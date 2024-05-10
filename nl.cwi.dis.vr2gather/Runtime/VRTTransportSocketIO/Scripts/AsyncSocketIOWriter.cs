using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;
using VRT.Core;
using Cwipc;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Transport.SocketIO
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;

    public class AsyncSocketIOWriter : TransportProtocolWriter
    {
        OutgoingStreamDescription[] streams;
        bool initialized = false;

        override public TransportProtocolWriter Init(string userId, string streamName, string fourcc, OutgoingStreamDescription[] streams)
        {
            if (streams == null)
            {
                throw new System.Exception($"{Name()}: outQueue is null");
            }
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
            this.streams = streams;
            for (int i = 0; i < streams.Length; ++i)
            {
                streams[i].name = $"{userId}.{streamName}#{i}";
#if VRT_WITH_STATS
                Statistics.Output(Name(), $"streamName={streamName}, streamid={i}, tile={streams[i].tileNumber}, orientation={streams[i].orientation}, streamname={streams[i].name}");
#endif
                OrchestratorWrapper.instance.DeclareDataStream(streams[i].name);
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
            initialized = true;
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
            base.AsyncOnStop();
            for (int i = 0; i < streams.Length; ++i)
            {
                if (!streams[i].inQueue.IsClosed())
                {
                    Debug.LogWarning($"[FPA] {Name()}: inQueue not closed, closing");
                    streams[i].inQueue.Close();
                }
            }
            Debug.Log($"[FPA] {Name()}: Stopped.");
            OrchestratorWrapper.instance.RemoveDataStream("AUDIO");
        }

        protected override void AsyncUpdate()
        {
            if (OrchestratorWrapper.instance != null && OrchestratorController.Instance.ConnectedToOrchestrator)
            {
                for (int i = 0; i < streams.Length; ++i)
                {
                    BaseMemoryChunk chk = streams[i].inQueue.Dequeue();
                    if (chk == null) continue;
                    if (chk.length > 1000000)
                    {
                        // Messages > 1MB case socket.io to hang up the connection. This will create very
                        // weird errors with the current Orchestrator and BestHTTP implementations.
                        Debug.LogError($"{Name()}: Message size {chk.length} exceeds SocketIO 1MByte maximum. Dropping. ");
                        continue;
                    }
                    var hdr_timestamp = BitConverter.GetBytes(chk.metadata.timestamp);
                    var buf = new byte[chk.length+sizeof(long)];
                    Array.Copy(hdr_timestamp, buf, sizeof(long));
                    System.Runtime.InteropServices.Marshal.Copy(chk.pointer, buf, sizeof(long), chk.length);
                    OrchestratorWrapper.instance.SendData(streams[i].name, buf);
#if VRT_WITH_STATS
                    stats.statsUpdate(chk.length, i);
#endif
                    chk.free();

                }
            }
        }
        // FPA: Ask Jack about GetSyncInfo().

        public override SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            return new SyncConfig.ClockCorrespondence();
        }

#if VRT_WITH_STATS
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
