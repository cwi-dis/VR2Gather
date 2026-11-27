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

        static int instanceCounter = 0;
        readonly int instanceNumber = instanceCounter++;

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        /// <summary>
        /// One incoming stream: its queue and its own stats, so every stream writes its own records,
        /// as the readers of the other transports do (#424).
        /// </summary>
        class StreamHandler
        {
            readonly AsyncSocketIOReader parent;
            readonly int index;
            readonly QueueThreadSafe outQueue;
#if !VRT_WITHOUT_STATS
            readonly Stats stats;
#endif

            public StreamHandler(AsyncSocketIOReader _parent, int _index, QueueThreadSafe _outQueue)
            {
                parent = _parent;
                index = _index;
                outQueue = _outQueue;
#if !VRT_WITHOUT_STATS
                stats = new Stats(Name());
#endif
            }

            public string Name()
            {
                return $"{parent.Name()}.{index}";
            }

            public void HandlePacket(UserDataStreamPacket pPacket)
            {
                byte[] hdr_timestamp = new byte[sizeof(long)];
                Array.Copy(pPacket.dataStreamPacket, hdr_timestamp, sizeof(long));
                Timestamp timestamp = BitConverter.ToInt64(hdr_timestamp, 0);
                BaseMemoryChunk chunk = new NativeMemoryChunk(pPacket.dataStreamPacket.Length - sizeof(long));
                chunk.metadata.timestamp = timestamp;
                System.Runtime.InteropServices.Marshal.Copy(pPacket.dataStreamPacket, sizeof(long), chunk.pointer, chunk.length);
                // Before Enqueue(): after it the queue or its consumer may have freed the chunk.
                int nBytes = chunk.length;
                bool didDrop = !outQueue.Enqueue(chunk);
                if (didDrop)
                {
                    Debug.Log($"{Name()}: dropped packet for {pPacket.dataStreamType}, ts={timestamp}, queuelength is {outQueue.Count()}");
                }
#if !VRT_WITHOUT_STATS
                // A failed Enqueue() while stopping is teardown (Stop() closes the queue), not a drop (#457).
                stats.statsUpdate(nBytes, didDrop && parent.isRunning, timestamp);
#endif
            }

            public void Final()
            {
#if !VRT_WITHOUT_STATS
                stats.Final();
#endif
            }
        }

        IncomingTileDescription[] descriptors;
        StreamHandler[] handlers;
        // Built in Init() before subscribing, read-only afterwards.
        System.Collections.Generic.Dictionary<string, StreamHandler> handlersByName;
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
            // Before subscribing: a packet may arrive right away.
            handlers = new StreamHandler[descriptors.Length];
            handlersByName = new System.Collections.Generic.Dictionary<string, StreamHandler>();
            for (int i = 0; i < this.descriptors.Length; ++i)
            {
                this.descriptors[i].name = $"{userId}.{streamName}#{i}";
                handlers[i] = new StreamHandler(this, i, this.descriptors[i].outQueue);
                handlersByName[this.descriptors[i].name] = handlers[i];
#if !VRT_WITHOUT_STATS
                // "pull_thread": the key VRTstatistics uses to map a reader's per-tile records to its tiles.
                Statistics.Output(Name(), $"pull_thread={handlers[i].Name()}, tile={this.descriptors[i].tileNumber}, stream={this.descriptors[i].name}");
#endif
            }
            try
            {
                for (int i = 0; i < this.descriptors.Length; ++i)
                {
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
            // After Stop() has unsubscribed, so no more packets (#451).
            if (handlers != null)
            {
                foreach (var h in handlers) h.Final();
            }
            base.AsyncOnStop();
        }

        protected override void AsyncUpdate()
        {
        }

        private void OnDataPacketReceived(UserDataStreamPacket pPacket)
        {
            // This callback method is called for _every_ socketIO reader. We select only the packets that are ours.
            if (handlersByName.TryGetValue(pPacket.dataStreamType, out StreamHandler handler))
            {
                handler.HandlePacket(pPacket);
            }
        }

        public void OnData(byte[] data)
        {
        }

#if !VRT_WITHOUT_STATS
        class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalBytes = 0;
            double statsTotalPackets = 0;
            int statsAggregatePackets = 0;
            double statsTotalDrops = 0;
            Timestamp lastTimestamp;
            
            public void statsUpdate(int nBytes, bool dropped, Timestamp timestamp)
            {
                lock (statsLock)
                {
                    statsTotalBytes += nBytes;
                    statsTotalPackets++;
                    statsAggregatePackets++;
                    if (dropped)
                    {
                        statsTotalDrops++;
                        OutputDrop(DropReason.Full);
                    }
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
#endif
    }

    public class AsyncSocketIOReader_PC : AsyncSocketIOReader
    {

    }
}
