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

    public class AsyncPCNullDecoder : AbstractPointCloudDecoder
    {
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public AsyncPCNullDecoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue) : base(_inQueue, _outQueue)
        {
            Start();
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public override void Stop()
        {
            base.Stop();
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
        }

        public override void AsyncOnStop()
        {
            base.AsyncOnStop();
            lock (this)
            {
                if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            }
        }

        protected override void AsyncUpdate()
        {
            NativeMemoryChunk mc;
            lock (this)
            {
                mc = (NativeMemoryChunk)inQueue.Dequeue();
                if (mc == null) return;
            }
            System.DateTime decodeStartTime = System.DateTime.Now;
            cwipc.pointcloud pc = cwipc.from_packet(mc.pointer, (System.IntPtr)mc.length);
            System.DateTime decodeStopTime = System.DateTime.Now;
            Timedelta decodeDuration = (Timedelta)(decodeStopTime - decodeStartTime).TotalMilliseconds;
            mc.free();
            if (pc == null)
            {
                throw new System.Exception($"{Name()}: from_packet did not return a pointcloud");
            }
            Timedelta queuedDuration = outQueue.QueuedDuration();
            bool dropped = !outQueue.Enqueue(pc);
#if VRT_WITH_STATS
            stats.statsUpdate(pc.count(), dropped, inQueue.QueuedDuration(), decodeDuration, queuedDuration);
#endif
        }

#if VRT_WITH_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalPoints = 0;
            double statsTotalPointclouds = 0;
            double statsTotalDropped = 0;
            double statsTotalInQueueDuration = 0;
            double statsTotalDecodeDuration = 0;
            double statsTotalQueuedDuration = 0;
            int statsAggregatePackets = 0;

            public void statsUpdate(int pointCount, bool dropped, Timedelta inQueueDuration, Timedelta decodeDuration, Timedelta queuedDuration)
            {
                statsTotalPoints += pointCount;
                statsTotalPointclouds++;
                statsAggregatePackets++;
                statsTotalInQueueDuration += inQueueDuration;
                statsTotalDecodeDuration += decodeDuration;
                statsTotalQueuedDuration += queuedDuration;
                if (dropped) statsTotalDropped++;

                if (ShouldOutput())
                {
                    double factor = (statsTotalPointclouds == 0 ? 1 : statsTotalPointclouds);
                    Output($"fps={statsTotalPointclouds / Interval():F2}, fps_dropped={statsTotalDropped / Interval():F2}, points_per_cloud={(int)(statsTotalPoints / factor)}, decoder_queue_ms={(int)(statsTotalInQueueDuration / factor)}, decoder_ms={statsTotalDecodeDuration / factor:F2}, decoded_queue_ms={(int)(statsTotalQueuedDuration / factor)}, aggregate_packets={statsAggregatePackets}");
                    Clear();
                    statsTotalPoints = 0;
                    statsTotalPointclouds = 0;
                    statsTotalDropped = 0;
                    statsTotalInQueueDuration = 0;
                    statsTotalQueuedDuration = 0;
                    statsTotalDecodeDuration = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }
}