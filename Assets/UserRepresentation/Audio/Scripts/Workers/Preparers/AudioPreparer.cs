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

        public int available {
            get {
                lock (inQueue) {
                    if (!inQueue.IsClosed())
                        return inQueue._Count * VoiceReader.wantedOutputBufferSize * 2;
                    else
                        return 0;
                }
            }
        }

        bool firstTime = true;
        public bool GetAudioBuffer(float[] dst, int len) {
            lock (inQueue) {
                if (!inQueue.IsClosed()) {
                    FloatMemoryChunk mc = (FloatMemoryChunk)inQueue.TryDequeue(1);
                    if (mc == null) return false;
                    System.Array.Copy(mc.buffer, 0, dst, 0, len);
                }
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
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: {Name()}: {statsTotalUpdates / 10} fps, {(int)samplesInBufferAverage} samples output latency, {(int)(timeInBufferAverage * 1000)} ms output latency, {statsDrops/10} drops per second");
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
