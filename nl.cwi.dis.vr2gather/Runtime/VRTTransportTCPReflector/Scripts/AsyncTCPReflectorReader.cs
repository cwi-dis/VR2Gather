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
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;

    public class AsyncTCPReflectorReader : AsyncReader, ITransportProtocolReader, ITransportProtocolReader_Tiled
    {
        /// <summary>
        /// One incoming stream: its queue and its own stats, so every stream writes its own records,
        /// as the readers of the other transports do (#424).
        /// </summary>
        class IncomingStreamHandler : TransportProtocolTCPReflector.IncomingStreamHandler {
            AsyncTCPReflectorReader parent;
            QueueThreadSafe queue;
            int handler_index;
#if !VRT_WITHOUT_STATS
            readonly Stats stats;
#endif

            public string Name()
            {
                return $"{parent.Name()}.{handler_index}";
            }
            public IncomingStreamHandler(AsyncTCPReflectorReader _parent, int _index, QueueThreadSafe _queue) {
                parent = _parent;
                handler_index = _index;
                queue = _queue;
#if !VRT_WITHOUT_STATS
                stats = new Stats(Name());
#endif
            }

            public void HandlePacket(BaseMemoryChunk packet) {
                // Before Enqueue(): after it the queue or its consumer may have freed the packet.
                int nBytes = packet.length;
                Timestamp timestamp = packet.metadata.timestamp;
                bool didDrop = !queue.Enqueue(packet);
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
        static public ITransportProtocolReader Factory()
        {
            return new AsyncTCPReflectorReader();
        }

        static public ITransportProtocolReader_Tiled Factory_Tiled()
        {
            return new AsyncTCPReflectorReader();
        }

        IncomingTileDescription[] descriptors;
        IncomingStreamHandler[] handlers;
        private TransportProtocolTCPReflector connection;
        
        static int instanceCounter = 0;
        readonly int instanceNumber = instanceCounter++;

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public ITransportProtocolReader_Tiled Init(string remoteUrl, string userId, string streamName, string fourcc, IncomingTileDescription[] descriptors)
        {
            NoUpdateCallsNeeded();
            if (descriptors == null)
            {
                throw new System.Exception($"{Name()}: descriptors is null");
            }
            this.descriptors = descriptors;
            handlers = new IncomingStreamHandler[descriptors.Length];
            connection = TransportProtocolTCPReflector.Connect(remoteUrl);
            for (int i = 0; i < this.descriptors.Length; ++i)
            {
                this.descriptors[i].name = $"{userId}/{streamName}/{i}";
                Debug.Log($"{Name()}:  xxxjack RegisterForDataStream {i}: {this.descriptors[i].name}");
                handlers[i] = new(this, i, this.descriptors[i].outQueue);
#if !VRT_WITHOUT_STATS
                // "pull_thread": the key VRTstatistics uses to map a reader's per-tile records to its tiles.
                Statistics.Output(Name(), $"pull_thread={handlers[i].Name()}, tile={this.descriptors[i].tileNumber}, stream={this.descriptors[i].name}");
#endif
                connection.RegisterIncomingStream(this.descriptors[i].name, handlers[i]);
            }
            Start();
            Debug.Log($"{Name()}: Started {remoteUrl}/{userId}/{streamName}");
           
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
            // Close our outputs so the consumers wake up at once (cwi-dis/cwipc_unity#82, rule 1).
            for (int i = 0; i < descriptors.Length; ++i)
            {
                descriptors[i].outQueue?.Close();
            }
        }

        public override void AsyncOnStop()
        {
            // Not in Stop(): the last unregister stops the connection, which joins its threads,
            // and that must not happen on the main thread (rule 4).
            for (int i = 0; i < descriptors.Length; ++i)
            {
                descriptors[i].outQueue?.Close();
                connection.UnregisterIncomingStream(descriptors[i].name); // This should also delete the handler by removing the last ref to it.
            }
            if (handlers != null)
            {
                foreach (var h in handlers) h.Final();
            }
            base.AsyncOnStop();
        }

        protected override void AsyncUpdate()
        {
        }

#if xxxjack
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
#endif
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

    public class AsyncTCPSFUReader_PC : AsyncTCPReflectorReader
    {

    }
}
