using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using Cwipc;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using EncoderStreamDescription = StreamSupport.EncoderStreamDescription;

    public class AsyncPCNullEncoder : AbstractPointCloudEncoder
    {
        cwipc.encodergroup encoderGroup;
        System.IntPtr encoderBuffer;
        QueueThreadSafe inQueue;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        EncoderStreamDescription[] outputs;

        public AsyncPCNullEncoder(QueueThreadSafe _inQueue, EncoderStreamDescription[] _outputs) : base()
        {
            if (_inQueue == null)
            {
                throw new System.Exception("{Name()}: inQueue is null");
            }
            inQueue = _inQueue;
            outputs = _outputs;
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
            Start();
        }

        public override void AsyncOnStop()
        {
            for (int i=0; i<outputs.Length; i++) {
                outputs[i].outQueue.Close();
                outputs[i].outQueue = null;
            }
            base.AsyncOnStop();
        }

        protected override void AsyncUpdate()
        {
            cwipc.pointcloud pc = (cwipc.pointcloud)inQueue.TryDequeue(0);
            if (pc == null) return; // Terminating, or no pointcloud currently available
            for (int i=0; i<outputs.Length; i++)
            {
                Timedelta encodeDuration = 0;
                NativeMemoryChunk mc = null;
                if (outputs[i].tileFilter == 0)
                {
                    System.DateTime encodeStartTime = System.DateTime.Now;
                    int size = pc.copy_packet(System.IntPtr.Zero, 0);
                    mc = new NativeMemoryChunk(size);
                    pc.copy_packet(mc.pointer, mc.length);
                    mc.metadata.timestamp = pc.timestamp();
                    System.DateTime encodeStopTime = System.DateTime.Now;
                    encodeDuration = (Timedelta)(encodeStopTime - encodeStartTime).TotalMilliseconds;
                }
                else
                {
                    System.DateTime encodeStartTime = System.DateTime.Now;
                    cwipc.pointcloud pcTile = cwipc.tilefilter(pc, outputs[i].tileFilter);
                    int size = pcTile.copy_packet(System.IntPtr.Zero, 0);
                    mc = new NativeMemoryChunk(size);
                    pcTile.copy_packet(mc.pointer, mc.length);
                    mc.metadata.timestamp = pc.timestamp();
                    pcTile.free();
                    System.DateTime encodeStopTime = System.DateTime.Now;
                    encodeDuration = (Timedelta)(encodeStopTime - encodeStartTime).TotalMilliseconds;
                }
                QueueThreadSafe queue = outputs[i].outQueue;
                Timedelta queuedDuration = queue.QueuedDuration();
                bool dropped = !queue.Enqueue(mc);
#if VRT_WITH_STATS
                stats.statsUpdate(dropped, encodeDuration, queuedDuration);
#endif
            }
            pc.free();
        }

#if VRT_WITH_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalPointclouds = 0;
            double statsTotalDropped = 0;
            double statsTotalEncodeDuration = 0;
            double statsTotalQueuedDuration = 0;
            int statsAggregatePackets = 0;

            public void statsUpdate(bool dropped, Timedelta encodeDuration, Timedelta queuedDuration)
            {
                statsTotalPointclouds++;
                statsAggregatePackets++;
                statsTotalEncodeDuration += encodeDuration;
                statsTotalQueuedDuration += queuedDuration;
                if (dropped) statsTotalDropped++;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalPointclouds / Interval():F2}, fps_dropped={statsTotalDropped / Interval():F2}, encoder_ms={statsTotalEncodeDuration / statsTotalPointclouds:F2}, transmitter_queue_ms={statsTotalQueuedDuration / statsTotalPointclouds}, aggregate_packets={statsAggregatePackets}");
                    Clear();
                    statsTotalPointclouds = 0;
                    statsTotalDropped = 0;
                    statsTotalEncodeDuration = 0;
                    statsTotalQueuedDuration = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }
}