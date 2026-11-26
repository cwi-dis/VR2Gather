using System.Collections;
using System.Collections.Generic;
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

    public class AsyncVoiceDecoder : AsyncWorker
    {
        // One per participant: a number to tell them apart in logs and stats records (#424).
        static int instanceCounter = 0;
        readonly int instanceNumber = instanceCounter++;

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        QueueThreadSafe inQueue;
        QueueThreadSafe outQueue;

        NSpeex.SpeexDecoder decoder;
        public AsyncVoiceDecoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue) : base()
        {
#if !VRT_WITHOUT_STATS
            stats = new Stats(Name());
#endif
            inQueue = _inQueue;
            outQueue = _outQueue;
            decoder = new NSpeex.SpeexDecoder(NSpeex.BandMode.Wide);
            // playerFrequency = decoder.SampleRate;
            Start();
        }

        public override void Stop()
        {
            // Close our output so the consumer wakes up at once (cwi-dis/cwipc_unity#82, rule 1).
            outQueue.Close();
            base.Stop();
        }

        public override void AsyncOnStop()
        {
            outQueue.Close();
#if !VRT_WITHOUT_STATS
            stats.Final();
#endif
            base.AsyncOnStop();
        }

        float[] temporalBuffer;
        float[] receiveBuffer2;
        NTPTools.NTPTime tempTime;
        protected override void AsyncUpdate()
        {
            if (inQueue.IsClosed())
            {
                // End of input: pass it on downstream.
                outQueue.Close();
                return;
            }
            // No flushing of an overfull inQueue here: the pipeline gives us a dropping queue of
            // 10 entries (VoicePipelineOther.decoderQueueSize), so it can't fill up.
            NativeMemoryChunk mcIn = (NativeMemoryChunk)inQueue.TryDequeue(DequeueTimeoutMs);
            if (mcIn == null) return;


            byte[] buffer = new byte[mcIn.length];
            if (temporalBuffer == null) temporalBuffer = new float[mcIn.length * 10]; // mcIn.length*10
            System.Runtime.InteropServices.Marshal.Copy(mcIn.pointer, buffer, 0, mcIn.length);
            int len = 0;
            var decodeStartTime = System.DateTime.Now;

            len = decoder.Decode(buffer, 0, mcIn.length, temporalBuffer, 0);

            FloatMemoryChunk mcOut = new FloatMemoryChunk(len);
            mcOut.metadata = mcIn.metadata;
            for (int i = 0; i < len; ++i)
            {
                mcOut.buffer[i] = temporalBuffer[i];
            }
            Timedelta decodeDuration = (Timedelta)(System.DateTime.Now - decodeStartTime).TotalMilliseconds;
            bool dropped = !outQueue.Enqueue(mcOut);
#if !VRT_WITHOUT_STATS
            stats.statsUpdate(decodeDuration, inQueue.QueuedDuration(), dropped);
#endif
            mcIn.free();
        }

#if !VRT_WITHOUT_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalUpdates;
            double statsTotalDecodeDuration;
            double statsTotalQueuedDuration;
            double statsDrops;

            public void statsUpdate(Timedelta decodeDuration, Timedelta queuedDuration, bool dropped)
            {
                lock (statsLock)
                {
                    statsTotalUpdates += 1;
                    statsTotalDecodeDuration += decodeDuration;
                    statsTotalQueuedDuration += queuedDuration;
                    if (dropped) statsDrops++;
                    MaybeOutput();
                }
            }

            protected override void OutputAggregate()
            {
                OutputFields(
                    Rate("fps", statsTotalUpdates, "F3"),
                    Average("decoder_ms", statsTotalDecodeDuration, statsTotalUpdates),
                    Average("decoder_queue_ms", statsTotalQueuedDuration, statsTotalUpdates, "F0"),
                    Rate("fps_dropped", statsDrops),
                    Field("n_frames", statsTotalUpdates),
                    Field("n_dropped", statsDrops));
                statsTotalUpdates = 0;
                statsTotalDecodeDuration = 0;
                statsTotalQueuedDuration = 0;
                statsDrops = 0;
            }
        }

        protected Stats stats;
#endif
    }
}