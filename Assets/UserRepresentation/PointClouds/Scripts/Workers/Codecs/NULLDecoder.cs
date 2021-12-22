using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class NULLDecoder : BaseWorker
    {
        protected QueueThreadSafe inQueue;
        protected QueueThreadSafe outQueue;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public NULLDecoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue) : base(WorkerType.Run)
        {
            if (_inQueue == null)
            {
                throw new System.Exception("NULLDecoder: inQueue is null");
            }
            if (_outQueue == null)
            {
                throw new System.Exception("NULLDecoder: outQueue is null");
            }
            inQueue = _inQueue;
            outQueue = _outQueue;
            Start();
            Debug.Log($"{Name()} Inited");
            stats = new Stats(Name());
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

        public override void OnStop()
        {
            base.OnStop();
            lock (this)
            {
                if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            }
        }

        protected override void Update()
        {
            base.Update();
            NativeMemoryChunk mc;
            lock (this)
            {
                mc = (NativeMemoryChunk)inQueue.Dequeue();
                if (mc == null) return;
            }
            cwipc.pointcloud pc = cwipc.from_packet(mc.pointer, (System.IntPtr)mc.length);
            mc.free();
            if (pc == null)
            {
                throw new System.Exception($"{Name()}: from_packet did not return a pointcloud");
            }
            bool dropped = !outQueue.Enqueue(pc);
            stats.statsUpdate(pc.count(), dropped, outQueue.QueuedDuration());
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalPoints = 0;
            double statsTotalPointclouds = 0;
            double statsTotalDropped = 0;
            double statsTotalQueuedDuration = 0;

            public void statsUpdate(int pointCount, bool dropped, ulong queuedDuration)
            {
                statsTotalPoints += pointCount;
                statsTotalPointclouds++;
                statsTotalQueuedDuration += queuedDuration;
                if (dropped) statsTotalDropped++;

                if (ShouldOutput())
                {
                    double factor = (statsTotalPointclouds == 0 ? 1 : statsTotalPointclouds);
                    Output($"fps={statsTotalPointclouds / Interval():F2}, fps_dropped={statsTotalDropped / Interval():F2}, points_per_cloud={(int)(statsTotalPoints / factor)}, decoder_queue_ms={statsTotalQueuedDuration / factor}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPoints = 0;
                    statsTotalPointclouds = 0;
                    statsTotalDropped = 0;
                    statsTotalQueuedDuration = 0;
                }
            }
        }
        protected Stats stats;

    }
}