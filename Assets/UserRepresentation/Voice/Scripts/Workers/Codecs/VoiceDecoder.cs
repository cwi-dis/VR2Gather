using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class VoiceDecoder : BaseWorker
    {
        QueueThreadSafe inQueue;
        QueueThreadSafe outQueue;

        NSpeex.SpeexDecoder decoder;
        public VoiceDecoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue) : base()
        {
            stats = new Stats(Name());
            inQueue = _inQueue;
            outQueue = _outQueue;
            decoder = new NSpeex.SpeexDecoder(NSpeex.BandMode.Wide);
            // playerFrequency = decoder.SampleRate;
            Debug.Log($"{Name()}: Started.");

            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            outQueue.Close();
            Debug.Log($"{Name()}: Stopped");
        }

        float[] temporalBuffer;
        float[] receiveBuffer2;
        NTPTools.NTPTime tempTime;
        protected override void Update()
        {
            base.Update();
            // Wipe out the inQueue for initial burst.
            NativeMemoryChunk mcIn = (NativeMemoryChunk)inQueue.Dequeue();
            if(inQueue._Count > 100){
                Debug.LogWarning($"{Name()}: flushing overfull inQueue, size={inQueue._Count}");
                while(inQueue._Count > 1) {
                    mcIn.free();
                    mcIn = (NativeMemoryChunk)inQueue.Dequeue();
                }
            }
            if (mcIn == null) return;


            byte[] buffer = new byte[mcIn.length];
            if (temporalBuffer == null) temporalBuffer = new float[mcIn.length * 10]; // mcIn.length*10
            System.Runtime.InteropServices.Marshal.Copy(mcIn.pointer, buffer, 0, mcIn.length);
            int len = 0;
            var decodeStartTime = System.DateTime.Now;

            len = decoder.Decode(buffer, 0, mcIn.length, temporalBuffer, 0);

            FloatMemoryChunk mcOut = new FloatMemoryChunk(len);
            mcOut.info.timestamp = mcIn.info.timestamp;
            for (int i = 0; i < len; ++i)
            {
                mcOut.buffer[i] = temporalBuffer[i];
            }
            Timedelta decodeDuration = (Timedelta)(System.DateTime.Now - decodeStartTime).TotalMilliseconds;
            bool dropped = !outQueue.Enqueue(mcOut);
            stats.statsUpdate(decodeDuration, inQueue.QueuedDuration(), dropped);
            mcIn.free();
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalUpdates;
            double statsTotalEncodeDuration;
            double statsTotalQueuedDuration;
            double statsDrops;

            public void statsUpdate(Timedelta decodeDuration, Timedelta queuedDuration, bool dropped)
            {

                statsTotalUpdates += 1;
                statsTotalEncodeDuration += decodeDuration;
                statsTotalQueuedDuration += queuedDuration;
                if (dropped) statsDrops++;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalUpdates / Interval():F3}, decoder_ms={statsTotalEncodeDuration / statsTotalUpdates:F2}, decoder_queue_ms={(int)(statsTotalQueuedDuration / statsTotalUpdates)}, fps_dropped={statsDrops / Interval()}");
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