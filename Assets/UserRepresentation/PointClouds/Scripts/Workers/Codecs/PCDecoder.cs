using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

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
            stats = new Stats(Name());
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
                throw;
            }
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

        bool _FeedDecoder() {
            NativeMemoryChunk mc = (NativeMemoryChunk)inQueue.TryDequeue(0);
            if (mc == null) return false;
            decoder.feed(mc.pointer, mc.length);
            mc.free();
            return true;
        }

        protected override void Update()
        {
            base.Update(); 
            lock (this)
            {
                // Feed data into the decoder, unless it already
                // has a pointcloud available.
                if (decoder == null) return;
                if (!decoder.available(false))
                {
                    if (!_FeedDecoder())
                    {
                        // No pointcloud obtained.
                        // There's also no decoder output available
                        // record this in the stats and return
                        stats.statsUpdate(false, 0, 0, 0);
                        return;
                    }
                }
            }
            while (decoder.available(false))
            {
                // While the decoder has pointclouds available
                // push them into the output queue, and if there
                // are more input packets available feed the decoder
                // again.
                cwipc.pointcloud pc = decoder.get();
                if (pc == null)
                {
                    throw new System.Exception($"{Name()}: cwipc_decoder: available() true, but did not return a pointcloud");
                }
                stats.statsUpdate(true, pc.count(), pc.timestamp(), inQueue._Count);
                outQueue.Enqueue(pc);
                _FeedDecoder();
            }
        }
        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalPoints = 0;
            double statsTotalPointclouds = 0;
            double statsTotalLatency = 0;
            int statsMaxQueueSize = 0;

            public void statsUpdate(bool gotPC, int pointCount, ulong timeStamp, int queueSize)
            {
                if (gotPC)
                {
                    System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                    double latency = (sinceEpoch.TotalMilliseconds - timeStamp) / 1000.0;
                    statsTotalPoints += pointCount;
                    statsTotalPointclouds++;
                    statsTotalLatency += latency;
                }
                if (queueSize > statsMaxQueueSize) statsMaxQueueSize = queueSize;

                if (ShouldOutput())
                {
                    int msLatency = (int)(1000 * statsTotalLatency / statsTotalPointclouds);
                    Output($"fps={statsTotalPointclouds / Interval():F2}, points_per_cloud={(int)(statsTotalPoints / (statsTotalPointclouds == 0 ? 1 : statsTotalPointclouds))}, pipeline_latency_ms={msLatency}, max_decoder_queuesize={statsMaxQueueSize}");
                 }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPoints = 0;
                    statsTotalPointclouds = 0;
                    statsTotalLatency = 0;
                    statsMaxQueueSize = 0;
                }
            }
        }

        protected Stats stats;

    }
}