using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTCore;

namespace VRT.UserRepresentation.PointCloud
{
    public class PCDecoder : BaseWorker
    {
        protected cwipc.decoder decoder;
        protected QueueThreadSafe inQueue;
        protected QueueThreadSafe outQueue;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public PCDecoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue) : base(WorkerType.Run)
        {
            if (_inQueue == null)
            {
                throw new System.Exception("PCDecoder: inQueue is null");
            }
            if (_outQueue == null)
            {
                throw new System.Exception("PCDecoder: outQueue is null");
            }
            try
            {
                inQueue = _inQueue;
                outQueue = _outQueue;
                decoder = cwipc.new_decoder();
                if (decoder == null)
                    throw new System.Exception("PCSUBReader: cwipc_new_decoder creation failed"); // Should not happen, should throw exception
                else
                {
                    Start();
                    Debug.Log($"{Name()} Inited");
                }

            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: Exception: {e.Message}");
                throw e;
            }
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
                decoder?.free();
                decoder = null;
                if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            }
            if (debugThreading) Debug.Log($"{Name()} Stopped");
        }

        protected override void Update()
        {
            base.Update();
            NativeMemoryChunk mc;
            lock (this)
            {
                mc = (NativeMemoryChunk)inQueue.Dequeue();
                if (mc == null) return;
                if (decoder == null) return;
            }
            decoder.feed(mc.pointer, mc.length);
            mc.free();
            while (decoder.available(false))
            {
                cwipc.pointcloud pc = decoder.get();
                if (pc == null)
                {
                    throw new System.Exception($"{Name()}: cwipc_decoder: available() true, but did not return a pointcloud");
                }
                stats.statsUpdate(pc.count(), pc.timestamp());
                outQueue.Enqueue(pc);
            }
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