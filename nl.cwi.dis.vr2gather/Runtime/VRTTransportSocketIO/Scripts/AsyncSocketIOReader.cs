using System;
using UnityEngine;
using VRT.Orchestrator.Implementation;
using VRT.Core;
using Cwipc;
using VRT.Orchestrator;
#if !VRT_WITHOUT_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Transport.SocketIO
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;

    public class AsyncSocketIOReader : AsyncReader, ISocketReader, ITransportProtocolReader, ITransportProtocolReader_Tiled
    {
        static public ITransportProtocolReader Factory()
        {
            return new AsyncSocketIOReader();
        }

        static public ITransportProtocolReader_Tiled Factory_Tiled()
        {
            return new AsyncSocketIOReader();
        }

        IncomingTileDescription[] descriptors;
        // The user whose streams we receive (not ourselves). Needed again in Stop() to unregister.
        string remoteUserId;

        public ITransportProtocolReader_Tiled Init(string remoteUrl, string userId, string streamName, string fourcc, IncomingTileDescription[] descriptors)
        {
            NoUpdateCallsNeeded();
            if (descriptors == null)
            {
                throw new System.Exception($"{Name()}: descriptors is null");
            }
            this.descriptors = descriptors;
            this.remoteUserId = userId;
            // Before subscribing: a packet may arrive right away, and OnDataPacketReceived uses the stats.
#if !VRT_WITHOUT_STATS
            stats = new Stats(Name());
#endif
            try
            {
                for (int i = 0; i < this.descriptors.Length; ++i)
                {
                    this.descriptors[i].name = $"{userId}.{streamName}#{i}";
                    Debug.Log($"{Name()}:  RegisterForDataStream {i}: {this.descriptors[i].name} from {userId}");
                    VRTOrchestratorSingleton.Streams.RegisterForDataStream(userId, this.descriptors[i].name);
                }

                VRTOrchestratorSingleton.Streams.OnDataStreamReceived += OnDataPacketReceived;
                Start();
                Debug.Log($"{Name()}: Started {remoteUrl}.{streamName}");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: Exception: {e.Message}");
                throw;
            }
            return this;
        }

        public ITransportProtocolReader Init(string remoteUrl, string userId, string streamName, int streamNumber, string fourcc, QueueThreadSafe outQueue)
        {
            Init(
                remoteUrl,
                userId,
                streamName,
                fourcc,
                new IncomingTileDescription[]
                {
                    new IncomingTileDescription()
                    {
                        outQueue = outQueue
                    }
                }
            );
            return this;   
        }

        public override void Stop()
        {
            base.Stop();
            // Unsubscribe first: otherwise this stopped reader keeps receiving every packet of
            // its stream (stream names are the same in every scene) and drops it into its
            // closed queue, for the rest of the session (#399).
            if (VRTOrchestratorSingleton.Streams != null)
                VRTOrchestratorSingleton.Streams.OnDataStreamReceived -= OnDataPacketReceived;
            for (int i = 0; i < descriptors.Length; ++i)
            {
                descriptors[i].outQueue?.Close();
                // Unregister with the same (remote) user id we registered with in Init().
                if (VRTOrchestratorSingleton.Streams != null)
                    VRTOrchestratorSingleton.Streams.UnregisterFromDataStream(remoteUserId, descriptors[i].name);
            }
        }

        public override void AsyncOnStop()
        {
            // After Stop() has unsubscribed, so no more statsUpdate() calls (#451).
#if !VRT_WITHOUT_STATS
            stats.Final();
#endif
            base.AsyncOnStop();
        }

        protected override void AsyncUpdate()
        {
        }

        private void OnDataPacketReceived(UserDataStreamPacket pPacket)
        {
            // This callback method is called for _every_ socketIO reader. We select only the packets that are ours.
            string streamName = pPacket.dataStreamType;
            for(int i = 0; i < descriptors.Length; i++)
            {
                if (streamName == descriptors[i].name)
                {
                    // This packet is for us.
                    byte[] hdr_timestamp = new byte[sizeof(long)];
                    Array.Copy(pPacket.dataStreamPacket, hdr_timestamp, sizeof(long));
                    Timestamp timestamp = BitConverter.ToInt64(hdr_timestamp, 0);
                    BaseMemoryChunk chunk = new NativeMemoryChunk(pPacket.dataStreamPacket.Length - sizeof(long));
                    chunk.metadata.timestamp = timestamp;
                    System.Runtime.InteropServices.Marshal.Copy(pPacket.dataStreamPacket, sizeof(long), chunk.pointer, chunk.length);
                    bool didDrop = !descriptors[i].outQueue.Enqueue(chunk);
                    if (didDrop)
                    {
                        Debug.Log($"{Name()}: dropped packet for {streamName}, ts={timestamp}, queuelength is {descriptors[i].outQueue.Count()}");
                    } else
                    {
                        // Debug.Log($"{Name()}: Received packet for {streamName}, ts={timestamp}, size={chunk.length}");
                    }
#if !VRT_WITHOUT_STATS
                    stats.statsUpdate(chunk.length, didDrop, timestamp, i);
#endif
                    return;
                }
            }
           
        }

        public void OnData(byte[] data)
        {
        }

#if !VRT_WITHOUT_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalBytes = 0;
            double statsTotalPackets = 0;
            int statsAggregatePackets = 0;
            double statsTotalDrops = 0;
            int lastStreamIndex;
            Timestamp lastTimestamp;
            
            public void statsUpdate(int nBytes, bool dropped, Timestamp timestamp, int streamId)
            {
                lock (statsLock)
                {
                    statsTotalBytes += nBytes;
                    statsTotalPackets++;
                    statsAggregatePackets++;
                    if (dropped) statsTotalDrops++;
                    lastStreamIndex = streamId;
                    lastTimestamp = timestamp;
                    MaybeOutput();
                }
            }

            protected override void OutputAggregate()
            {
                OutputFields(
                    Rate("fps", statsTotalPackets),
                    Rate("fps_dropped", statsTotalDrops),
                    Average("bytes_per_packet", statsTotalBytes, statsTotalPackets, "F0"),
                    Field("last_stream_index", lastStreamIndex),
                    Field("last_timestamp", lastTimestamp),
                    Field("aggregate_packets", statsAggregatePackets),
                    Field("n_packets", statsTotalPackets),
                    Field("n_dropped", statsTotalDrops),
                    Field("bytes", statsTotalBytes));
                statsTotalBytes = 0;
                statsTotalPackets = 0;
                statsTotalDrops = 0;
            }
        }

        protected Stats stats;
#endif
    }

    public class AsyncSocketIOReader_PC : AsyncSocketIOReader
    {

    }
}
