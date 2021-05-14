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
            stats.statsUpdate(pc.count(), pc.timestamp());
            outQueue.Enqueue(pc);
        }
        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalPoints = 0;
            double statsTotalPointclouds = 0;
            double statsTotalLatency = 0;

            public void statsUpdate(int pointCount, ulong timeStamp)
            {
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                double latency = (sinceEpoch.TotalMilliseconds - timeStamp) / 1000.0;
                statsTotalPoints += pointCount;
                statsTotalPointclouds++;
                statsTotalLatency += latency;

                if (ShouldOutput())
                {
                    int msLatency = (int)(1000 * statsTotalLatency / statsTotalPointclouds);
                    Output($"fps={statsTotalPointclouds / Interval():F2}, points_per_cloud={(int)(statsTotalPoints / (statsTotalPointclouds == 0 ? 1 : statsTotalPointclouds))}, pipeline_latency_ms={msLatency}");
                 }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPoints = 0;
                    statsTotalPointclouds = 0;
                    statsTotalLatency = 0;
                }
            }
        }

        protected Stats stats;

    }
}