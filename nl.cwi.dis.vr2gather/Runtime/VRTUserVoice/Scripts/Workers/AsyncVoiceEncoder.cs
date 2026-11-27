using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using VRT.Core;
using Cwipc;
#if !VRT_WITHOUT_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.UserRepresentation.Voice
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;

    public class AsyncVoiceEncoder : AsyncWorker
    {
        public int minSamplesPerFrame { get; private set; }
        int frames;
        NSpeex.SpeexEncoder encoder;

        QueueThreadSafe inQueue;
        QueueThreadSafe outQueue;
        public AsyncVoiceEncoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue, int frames = 1) : base()
        {
#if !VRT_WITHOUT_STATS
            stats = new Stats(Name());
#endif
            inQueue = _inQueue;
            outQueue = _outQueue;
            this.frames = frames;
            encoder = new NSpeex.SpeexEncoder(NSpeex.BandMode.Wide);
            minSamplesPerFrame = encoder.FrameSize * frames;
            encoder.Quality = 5;
            Start();
        }

        public override void Stop()
        {
            // base.Stop() first: a failed Enqueue() after it is teardown, not a drop (#457).
            base.Stop();
            // Close our output so the consumer wakes up at once (cwi-dis/cwipc_unity#82, rule 1).
            outQueue?.Close();
        }

        public override void AsyncOnStop()
        {
            outQueue?.Close();
#if !VRT_WITHOUT_STATS
            stats.Final();
#endif
            base.AsyncOnStop();
        }

        byte[] sendBuffer;
        protected override void AsyncUpdate()
        {
            if (inQueue.IsClosed())
            {
                // End of input: pass it on downstream.
                outQueue?.Close();
                return;
            }
            FloatMemoryChunk mcIn = (FloatMemoryChunk)inQueue.TryDequeue(DequeueTimeoutMs);
            if (mcIn == null) return;
            if (sendBuffer == null) sendBuffer = new byte[mcIn.length];

            var encodeStartTime = System.DateTime.Now;
            int len = encoder.Encode(mcIn.buffer, 0, mcIn.elements, sendBuffer, 0, sendBuffer.Length);
            NativeMemoryChunk mcOut = new NativeMemoryChunk(len);
            Marshal.Copy(sendBuffer, 0, mcOut.pointer, len);
            Timedelta encodeDuration = (Timedelta)(System.DateTime.Now - encodeStartTime).TotalMilliseconds;

            mcOut.metadata = mcIn.metadata;
            if (outQueue.IsClosed())
            {
                mcOut.free();
                mcIn.free();
                return;
            }
            Timedelta queuedDuration = outQueue.QueuedDuration();
            bool ok = outQueue.Enqueue(mcOut);
#if !VRT_WITHOUT_STATS
            // A failed Enqueue() while stopping is teardown (Stop() closes the queue), not a drop (#457).
            stats.statsUpdate(encodeDuration, queuedDuration, !ok && isRunning);
#endif
            mcIn.free();
        }

#if !VRT_WITHOUT_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalUpdates;
            double statsTotalEncodeDuration;
            double statsTotalQueuedDuration;
            double statsDrops;

            public void statsUpdate(Timedelta encodeDuration, Timedelta queuedDuration, bool dropped)
            {
                lock (statsLock)
                {
                    statsTotalUpdates += 1;
                    statsTotalEncodeDuration += encodeDuration;
                    statsTotalQueuedDuration += queuedDuration;
                    if (dropped)
                    {
                        statsDrops++;
                        OutputDrop(DropReason.Full);
                    }
                    MaybeOutput();
                }
            }

            protected override void OutputAggregate()
            {
                OutputFields(
                    Rate("fps", statsTotalUpdates, "F3"),
                    Average("encoder_ms", statsTotalEncodeDuration, statsTotalUpdates),
                    Average("transmitter_queue_ms", statsTotalQueuedDuration, statsTotalUpdates, "F0"),
                    Rate("fps_dropped", statsDrops),
                    Field("n_frames", statsTotalUpdates),
                    Field("n_dropped", statsDrops));
                statsTotalUpdates = 0;
                statsTotalEncodeDuration = 0;
                statsTotalQueuedDuration = 0;
                statsDrops = 0;
            }
        }

        protected Stats stats;
#endif
    }
}
