using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class VoicePreparer : BasePreparer
    {
        int bufferSize;

        QueueThreadSafe inQueue;

        public VoicePreparer(QueueThreadSafe _inQueue) : base(WorkerType.End)
        {
            inQueue = _inQueue;
            if (inQueue == null) Debug.LogError($"VoicePreparer: Programmer error: ERROR inQueue=NULL");
            bufferSize = 320 * 6 * 100;
            Debug.Log("VoicePreparer: Started.");
            // xxxjack stats not used? stats = new Stats(Name());
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            Debug.Log("VoicePreparer: Stopped");
        }

        protected override void Update()
        {
            base.Update();
        }
        public override void Synchronize()
        {
        }

        public override bool LatchFrame()
        {
            // xxxjack Not implemented yet for audio...
            return true;
        }

        bool firstTime = true;
        public bool GetAudioBuffer(float[] dst, int len)
        {
            if (!inQueue.IsClosed())
            {
                FloatMemoryChunk mc = (FloatMemoryChunk)inQueue.TryDequeue(1);
                if (mc == null) return false;
                System.Array.Copy(mc.buffer, 0, dst, 0, len);
                mc.free();
            }
            return true;
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalUpdates;
            double statsTotalSamplesInOutputBuffer;
            double statsDrops;

            public void statsUpdate(int samplesInOutputBuffer, bool dropped)
            {

                statsTotalUpdates += 1;
                statsTotalSamplesInOutputBuffer += samplesInOutputBuffer;
                if (dropped) statsDrops++;

                if (ShouldOutput())
                {
                    double samplesInBufferAverage = statsTotalSamplesInOutputBuffer / statsTotalUpdates;
                    double timeInBufferAverage = samplesInBufferAverage / VoiceReader.wantedOutputSampleRate;
                    Output($"fps={statsTotalUpdates / Interval():F3}, playout_latency_samples={(int)samplesInBufferAverage}, playout_latency_ms={(int)(timeInBufferAverage * 1000)}, drops_per_second={statsDrops / Interval()}");
                 }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalUpdates = 0;
                    statsTotalSamplesInOutputBuffer = 0;
                    statsDrops = 0;
                }
            }
        }

        protected Stats stats;

    }
}
