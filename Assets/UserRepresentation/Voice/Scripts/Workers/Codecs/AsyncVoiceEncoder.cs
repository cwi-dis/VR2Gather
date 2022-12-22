using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using VRT.Core;
using Cwipc;
#if VRT_WITH_STATS
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
#if VRT_WITH_STATS
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

        public override void AsyncOnStop()
        {
            base.AsyncOnStop();
            outQueue?.Close();
            outQueue = null;
        }

        byte[] sendBuffer;
        protected override void AsyncUpdate()
        {
            if (inQueue.IsClosed())
            {
                if (outQueue != null && !outQueue.IsClosed())
                {
                    outQueue.Close();
                    outQueue = null;
                }
                return;
            }
            FloatMemoryChunk mcIn = (FloatMemoryChunk)inQueue.Dequeue();
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
                return;
            }
            Timedelta queuedDuration = outQueue.QueuedDuration();
            bool ok = outQueue.Enqueue(mcOut);
#if VRT_WITH_STATS
            stats.statsUpdate(encodeDuration, queuedDuration, !ok);
#endif
            mcIn.free();
        }

#if VRT_WITH_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalUpdates;
            double statsTotalEncodeDuration;
            double statsTotalQueuedDuration;
            double statsDrops;

            public void statsUpdate(Timedelta encodeDuration, Timedelta queuedDuration, bool dropped)
            {

                statsTotalUpdates += 1;
                statsTotalEncodeDuration += encodeDuration;
                statsTotalQueuedDuration += queuedDuration;
                if (dropped) statsDrops++;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalUpdates / Interval():F3}, encoder_ms={(statsTotalEncodeDuration / statsTotalUpdates):F2}, transmitter_queue_ms={(int)(statsTotalQueuedDuration / statsTotalUpdates)}, fps_dropped={statsDrops / Interval()}");
                    Clear();
                    statsTotalUpdates = 0;
                    statsTotalEncodeDuration = 0;
                    statsTotalQueuedDuration = 0;
                    statsDrops = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }
}
