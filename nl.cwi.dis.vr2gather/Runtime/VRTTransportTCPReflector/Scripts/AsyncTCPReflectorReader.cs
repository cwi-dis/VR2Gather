using System;
using UnityEngine;
using VRT.Core;
using Cwipc;
#if VRT_WITH_STATS
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
        class IncomingStreamHandler : TransportProtocolTCPReflector.IncomingStreamHandler {
            AsyncTCPReflectorReader parent;
            QueueThreadSafe queue;
            int handler_index;

            public string Name()
            {
                return $"{parent.Name()}.{handler_index}";
            }
            public IncomingStreamHandler(AsyncTCPReflectorReader _parent, int _index, QueueThreadSafe _queue) {
                parent = _parent;
                handler_index = _index;
                queue = _queue;
            }

            public void HandlePacket(BaseMemoryChunk packet) {
                bool didDrop = !queue.Enqueue(packet);
#if VRT_WITH_STATS
                parent.stats.statsUpdate(packet.length, didDrop, packet.metadata.timestamp, handler_index);
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
        bool initialized = false;
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
            connection = TransportProtocolTCPReflector.Connect(remoteUrl);
            for (int i = 0; i < this.descriptors.Length; ++i)
            {
                this.descriptors[i].name = $"{userId}/{streamName}/{i}";
                Debug.Log($"{Name()}:  xxxjack RegisterForDataStream {i}: {this.descriptors[i].name}");
                IncomingStreamHandler handler = new(this, i, this.descriptors[i].outQueue);
                connection.RegisterIncomingStream(this.descriptors[i].name, handler);
            }
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
            Start();
            Debug.Log($"{Name()}: Started {remoteUrl}/{userId}/{streamName}");
           
            initialized = true;
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
            initialized = true;
            return this;   
        }

        public override void Stop()
        {
            base.Stop();
            for (int i = 0; i < descriptors.Length; ++i)
            {
                descriptors[i].outQueue?.Close();
                connection.UnregisterIncomingStream(descriptors[i].name); // This should also delete the handler by removing the last ref to it.
            }
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
#if VRT_WITH_STATS
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

#if VRT_WITH_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalBytes = 0;
            double statsTotalPackets = 0;
            int statsAggregatePackets = 0;
            double statsTotalDrops = 0;
            
            public void statsUpdate(int nBytes, bool dropped, Timestamp timestamp, int streamId)
            {
                statsTotalBytes += nBytes;
                statsTotalPackets++;
                statsAggregatePackets++;
                if (dropped) statsTotalDrops++;
                if (ShouldOutput())
                {
                    Output($"fps={statsTotalPackets / Interval():F2}, fps_dropped={statsTotalDrops / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}, last_stream_index={streamId}, last_timestamp={timestamp}, aggregate_packets={statsAggregatePackets}");
                    Clear();
                    statsTotalBytes = 0;
                    statsTotalPackets = 0;
                    statsTotalDrops = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }

    public class AsyncTCPSFUReader_PC : AsyncTCPReflectorReader
    {

    }
}
