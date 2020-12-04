using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class AudioPreparer : BaseWorker {
        int bufferSize;

        QueueThreadSafe inQueue;

        public AudioPreparer(QueueThreadSafe _inQueue) : base(WorkerType.End) {
            inQueue = _inQueue;
            if (inQueue == null) Debug.LogError($"AudioPreparer: Programmer error: ERROR inQueue=NULL");
            bufferSize = 320 * 6 * 100;
            Debug.Log("AudioPreparer: Started.");
            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("AudioPreparer: Stopped");
        }

        protected override void Update() {
            base.Update();
        }

        bool firstTime = true;
        public bool GetAudioBuffer(float[] dst, int len) {
            if (!inQueue.IsClosed()) {
                FloatMemoryChunk mc = (FloatMemoryChunk)inQueue.TryDequeue(1);
                if (mc == null) return false;
                System.Array.Copy(mc.buffer, 0, dst, 0, len);
                mc.free();
            }
            return true;
        }

        System.DateTime statsLastTime;
        double statsTotalUpdates;
        double statsTotalSamplesInOutputBuffer;
        double statsDrops;
        const int statsInterval = 10;

        public void statsUpdate(int samplesInOutputBuffer, bool dropped=false)
        {
            if (statsLastTime == null)
            {
                statsLastTime = System.DateTime.Now;
                statsTotalUpdates = 0;
                statsTotalSamplesInOutputBuffer = 0;
                statsDrops = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(10))
            {
                double samplesInBufferAverage = statsTotalSamplesInOutputBuffer / statsTotalUpdates;
                double timeInBufferAverage = samplesInBufferAverage / VoiceReader.wantedOutputSampleRate;
                Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={Name()}, fps={statsTotalUpdates / 10}, playout_latency_samples={(int)samplesInBufferAverage}, playout_latency_ms={(int)(timeInBufferAverage * 1000)}, drops_per_second={statsDrops/10}");
                statsTotalUpdates = 0;
                statsTotalSamplesInOutputBuffer = 0;
                statsDrops = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalUpdates += 1;
            statsTotalSamplesInOutputBuffer += samplesInOutputBuffer;
            if (dropped) statsDrops++;
        }
    }
}
