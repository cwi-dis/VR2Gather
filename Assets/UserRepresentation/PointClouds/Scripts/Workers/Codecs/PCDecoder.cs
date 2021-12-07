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
        bool debugColorize = true;

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
            stats = new Stats(Name());
            debugColorize = Config.Instance.PCs.debugColorize;
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
                if (debugColorize)
                {
                    int cnum = (instanceNumber % 6) + 1;
                    uint cmask = 0;
                    if ((cnum & 1) != 0) cmask |= 0x800000;
                    if ((cnum & 2) != 0) cmask |= 0x008000;
                    if ((cnum & 4) != 0) cmask |= 0x000080;
                    cwipc.pointcloud newpc = cwipc.colormap(pc, 0, cmask);
                    pc.free();
                    pc = newpc;
                }
                bool dropped = !outQueue.Enqueue(pc);
                stats.statsUpdate(pc.count(), dropped, inQueue.QueuedDuration());
                _FeedDecoder();
            }
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