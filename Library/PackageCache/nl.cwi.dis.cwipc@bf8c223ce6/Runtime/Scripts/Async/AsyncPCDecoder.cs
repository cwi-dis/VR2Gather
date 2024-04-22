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

    public class AsyncPCDecoder : AbstractPointCloudDecoder
    {
        /// <summary>
        /// Set this to true to colorize all points, making it easier to see where each tile is displayed.
        /// </summary>
        static public bool debugColorize = false;
        protected cwipc.decoder[] decoders;
        protected int nParallel = 1;
        protected int inDecoderIndex = 0;
        protected int outDecoderIndex = 0;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        System.DateTime[] mostRecentFeeds;

        public AsyncPCDecoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue) : base(_inQueue, _outQueue)
        {
            nParallel = CwipcConfig.Instance.decoderParallelism;
            if (nParallel == 0) nParallel = 1;
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
            try
            {
                decoders = new cwipc.decoder[nParallel];
                mostRecentFeeds = new System.DateTime[nParallel];
                for(int i=0; i<nParallel; i++)
                {
                    var d = cwipc.new_decoder();
                    if (d == null)
                    {
                        throw new System.Exception($"{Name()}: cwipc.new_decoder creation failed"); // Should not happen, should throw exception
                    }
                    decoders[i] = d;
                    mostRecentFeeds[i] = System.DateTime.MinValue;
                }
                Start();
                Debug.Log($"{Name()} Inited");

            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: Exception: {e.Message}");
                throw;
            }
            debugColorize = CwipcConfig.Instance.debugColorize;
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
                foreach(var d in decoders)
                {
                    d?.free();
                }
                decoders = null;
                if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            }
            if (debugThreading) Debug.Log($"{Name()} Stopped");
        }

        bool _FeedDecoder() {
            NativeMemoryChunk mc = (NativeMemoryChunk)inQueue.TryDequeue(0);
            if (mc == null) return false;
            mostRecentFeeds[inDecoderIndex] = System.DateTime.Now;
            var decoder = decoders[inDecoderIndex];
            inDecoderIndex = (inDecoderIndex + 1) % nParallel;
            if (nParallel > 1)
            {
                System.Threading.Thread th = new System.Threading.Thread(() =>
                {

                    decoder.feed(mc.pointer, mc.length);
                    mc.free();
                });
                th.Start();
            } 
            else
            {
                decoder.feed(mc.pointer, mc.length);
                mc.free();
            }
            return true;
        }

        protected override void AsyncUpdate()
        {
            lock (this)
            {
                // Feed data into the decoder, unless it already
                // has a pointcloud available, or a previously fed buffer hasn't been decoded yet.
                if (decoders == null) return;
                while (mostRecentFeeds[inDecoderIndex] == System.DateTime.MinValue)
                {
                    if (!_FeedDecoder()) break;
                }
            }
            if (decoders[outDecoderIndex].available(false))
            {
                // While the decoder has pointclouds available
                // push them into the output queue, and if there
                // are more input packets available feed the decoder
                // again.
                cwipc.pointcloud pc = decoders[outDecoderIndex].get();
                Timedelta decodeDuration = (Timedelta)(System.DateTime.Now - mostRecentFeeds[outDecoderIndex]).TotalMilliseconds;
                mostRecentFeeds[outDecoderIndex] = System.DateTime.MinValue;
                outDecoderIndex = (outDecoderIndex + 1) % nParallel;
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
                Timedelta queuedDuration = outQueue.QueuedDuration();
                bool dropped = !outQueue.Enqueue(pc);
#if VRT_WITH_STATS
                stats.statsUpdate(pc.count(), dropped, inQueue.QueuedDuration(), decodeDuration, queuedDuration);
#endif
                _FeedDecoder();
            }
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
                    Output($"fps={statsTotalPointclouds / Interval():F2}, fps_dropped={statsTotalDropped / Interval():F2}, points_per_cloud={(int)(statsTotalPoints / factor)}, decoder_queue_ms={(int)(statsTotalInQueueDuration / factor)}, decoder_ms={statsTotalDecodeDuration / factor:F2}, aggregate_packets={statsAggregatePackets}");
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