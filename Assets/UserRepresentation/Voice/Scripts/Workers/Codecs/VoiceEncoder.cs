using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class VoiceEncoder : BaseWorker
    {
        public int minSamplesPerFrame { get; private set; }
        int frames;
        NSpeex.SpeexEncoder encoder;

        QueueThreadSafe inQueue;
        QueueThreadSafe outQueue;
        public VoiceEncoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue, int frames = 1) : base()
        {
            stats = new Stats(Name());
            inQueue = _inQueue;
            outQueue = _outQueue;
            this.frames = frames;
            encoder = new NSpeex.SpeexEncoder(NSpeex.BandMode.Wide);
            minSamplesPerFrame = encoder.FrameSize * frames;
            encoder.Quality = 5;
            Debug.Log($"{Name()}: Started.");
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            outQueue?.Close();
            outQueue = null;
            Debug.Log($"{Name()}: Stopped.");
        }

        byte[] sendBuffer;
        protected override void Update()
        {
            base.Update();
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

            mcOut.info.timestamp = mcIn.info.timestamp;
            if (outQueue.IsClosed())
            {
                mcOut.free();
                return;
            }
            bool ok = outQueue.Enqueue(mcOut);
            stats.statsUpdate(encodeDuration, outQueue.QueuedDuration(), !ok);
            mcIn.free();
        }

        protected class Stats : VRT.Core.BaseStats
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
                    Output($"fps={statsTotalUpdates / Interval():F3}, encoder_ms={(int)(statsTotalEncodeDuration / statsTotalUpdates)}, transmitter_queue_ms={(int)(statsTotalQueuedDuration / statsTotalUpdates)}, fps_dropped={statsDrops / Interval()}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalUpdates = 0;
                    statsTotalEncodeDuration = 0;
                    statsTotalQueuedDuration = 0;
                    statsDrops = 0;
                }
            }
        }

        protected Stats stats;
    }
}
