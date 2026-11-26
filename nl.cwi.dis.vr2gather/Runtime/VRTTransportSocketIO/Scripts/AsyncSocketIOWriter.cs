using System;
using UnityEngine;
using VRT.Orchestrator;
using VRT.Core;
using Cwipc;
#if !VRT_WITHOUT_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Transport.SocketIO
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;

    public class AsyncSocketIOWriter : AsyncWriter, ITransportProtocolWriter
    {
        static public ITransportProtocolWriter Factory()
        {
            return new AsyncSocketIOWriter();
        }

        OutgoingStreamDescription[] streams;
        
        public ITransportProtocolWriter Init(string url, string userId, string streamName, string fourcc, OutgoingStreamDescription[] streams)
        {
            if (streams == null)
            {
                throw new System.Exception($"{Name()}: outQueue is null");
            }
            this.streams = streams;
#if !VRT_WITHOUT_STATS
            stats = new Stats[streams.Length];
#endif
            for (int i = 0; i < streams.Length; ++i)
            {
                streams[i].name = $"{userId}.{streamName}#{i}";
#if !VRT_WITHOUT_STATS
                Statistics.Output(Name(), $"streamName={streamName}, streamid={i}, tile={streams[i].tileNumber}, orientation={streams[i].orientation}, streamname={streams[i].name}");
                stats[i] = new Stats($"{Name()}.{i}");
                // "pusher": the key VRTstatistics uses to map a writer's per-stream records to its streams.
                Statistics.Output(Name(), $"pusher={Name()}.{i}, stream={i}");
#endif
                VRTOrchestratorSingleton.Streams.DeclareDataStream(streams[i].name);
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
            if (streams != null)
            {
                for (int i = 0; i < streams.Length; ++i)
                {
                    VRTOrchestratorSingleton.Streams?.RemoveDataStream(streams[i].name);
                }
            }
#if !VRT_WITHOUT_STATS
            foreach (var st in stats) st.Final();
#endif
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
                if (chk == null) continue;
                // Read once: the orchestrator connection can be torn down on the main thread meanwhile (#408).
                var orchestratorStreams = VRTOrchestratorSingleton.Streams;
                if (orchestratorStreams == null)
                {
                    chk.free();
                    continue;
                }
                if (chk.length > 1000000)
                {
                    // Messages > 1MB case socket.io to hang up the connection. This will create very
                    // weird errors with the current Orchestrator and BestHTTP implementations.
                    Debug.LogError($"{Name()}: Message size {chk.length} exceeds SocketIO 1MByte maximum. Dropping. ");
                    chk.free();
                    continue;
                }
                var hdr_timestamp = BitConverter.GetBytes(chk.metadata.timestamp);
                var buf = new byte[chk.length+sizeof(long)];
                Array.Copy(hdr_timestamp, buf, sizeof(long));
                System.Runtime.InteropServices.Marshal.Copy(chk.pointer, buf, sizeof(long), chk.length);
                orchestratorStreams.SendData(streams[i].name, buf);
#if !VRT_WITHOUT_STATS
                stats[i].statsUpdate(chk.length);
#endif
                chk.free();
            }
        }
        // FPA: Ask Jack about GetSyncInfo().

        public override SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            return new SyncConfig.ClockCorrespondence();
        }

#if !VRT_WITHOUT_STATS
        // One per stream, named <writer>.<stream index>, so every stream writes its own records, as the
        // writers of the other transports do (#424).
        class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalBytes;
            double statsTotalPackets;
            int statsAggregatePackets;
           
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

            protected override void OutputAggregate()
            {
                OutputFields(
                    Rate("fps", statsTotalPackets),
                    Average("bytes_per_packet", statsTotalBytes, statsTotalPackets, "F0"),
                    Field("aggregate_packets", statsAggregatePackets),
                    Field("n_packets", statsTotalPackets),
                    Field("bytes", statsTotalBytes));
                statsTotalBytes = 0;
                statsTotalPackets = 0;
            }
        }

        Stats[] stats;
#endif
    }
}
