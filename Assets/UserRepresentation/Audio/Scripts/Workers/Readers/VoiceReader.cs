using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class VoiceReader : BaseWorker
    {
        Coroutine coroutine;
        QueueThreadSafe outQueue;

        public VoiceReader(string deviceName, MonoBehaviour monoBehaviour, int bufferLength, QueueThreadSafe _outQueue) : base(WorkerType.Init) {
            outQueue = _outQueue;
            this.bufferLength = bufferLength;
            coroutine = monoBehaviour.StartCoroutine(MicroRecorder(deviceName));
            Debug.Log($"{Name()}: Started bufferLength {bufferLength}.");
            Start();
        }

        protected override void Update() {
            base.Update();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log($"{Name()}: Stopped microphone {device}.");
            outQueue.Close();
        }

        string      device;
        int         samples;
        int         bufferLength;
        AudioClip   recorder;
        public const int wantedOutputSampleRate = 16000 * 3;
        public const int wantedOutputBufferSize = 320 * 3;

        static bool DSPIsNotReady = true;
        public static void PrepareDSP() {
            if (DSPIsNotReady) {
                DSPIsNotReady = false;
                var ac = AudioSettings.GetConfiguration();
                ac.sampleRate = wantedOutputSampleRate;
                ac.dspBufferSize = wantedOutputBufferSize;
                AudioSettings.Reset(ac);
                ac = AudioSettings.GetConfiguration();
                if (ac.sampleRate != wantedOutputSampleRate)
                {
                    Debug.LogError($"Audio output sample rate is {ac.sampleRate} in stead of {wantedOutputSampleRate}. Other participants may sound funny.");
                }
                if (ac.dspBufferSize != wantedOutputBufferSize)
                {
                    Debug.LogWarning($"PrepareDSP: audio output buffer is {ac.dspBufferSize} in stead of {wantedOutputBufferSize}");
                }
            }

        }

        IEnumerator MicroRecorder(string deviceName) {
            PrepareDSP();
            if (Microphone.devices.Length > 0) {
                if (deviceName == null) deviceName = Microphone.devices[0];
                int currentMinFreq;
                int currentMaxFreq;
                Microphone.GetDeviceCaps(deviceName, out currentMinFreq, out currentMaxFreq);
                
                recorder = Microphone.Start(deviceName, true, 1, currentMaxFreq);
                samples = recorder.samples;

                float inc = samples / 16000f;
                int neededBufferLength = (int)(bufferLength * inc);
                float[] readBuffer = new float[neededBufferLength];
                Debug.Log($"{Name()}: Using {deviceName}  Frequency {samples} bufferLength {bufferLength} IsRecording {Microphone.IsRecording(deviceName)} inc {inc}");

                int readPosition = 0;

                while ( true ) {
                    if (Microphone.IsRecording(deviceName)) {
                        int writePosition = Microphone.GetPosition(deviceName);
                        int available;
                        if (writePosition < readPosition) available = (samples - readPosition) + writePosition;
                        else available = writePosition - readPosition;
                        while (available >= neededBufferLength) {
                            if (!recorder.GetData(readBuffer, readPosition)) {
                                Debug.Log($"{Name()}: ERROR!!! IsRecording {Microphone.IsRecording(deviceName)}");
                                Debug.LogError("Error while getting audio from microphone");
                            }
                            // Write all data from microphone.
                            lock (outQueue) {
                                if (outQueue._CanEnqueue()) {
                                    FloatMemoryChunk mc = new FloatMemoryChunk(bufferLength);
                                    float idx = 0;
                                    for (int i = 0; i < bufferLength; i++) {
                                        mc.buffer[i] = readBuffer[(int)idx];
                                        idx += inc;
                                    }
                                    outQueue.Enqueue(mc);
                                }
                            }
                            readPosition = (readPosition + neededBufferLength) % samples;
                            available -= neededBufferLength;
                        }
                    } else {
                        Debug.LogWarning($"{Name()}: microphone {deviceName} stopped recording, starting again.");
                        recorder = Microphone.Start(deviceName, true, 1, samples);
                        readPosition = 0;
                    }
                    yield return null;
                }
            } else
                Debug.LogError("{Name()}: No Microphones detected.");
        }

        System.DateTime statsLastTime;
        double statsTotalUpdates = 0;
        double statsTotalSamplesInInputBuffer = 0;
        const int statsInterval = 10;

        public void statsUpdate(int samplesInInputBuffer)
        {
            if (statsLastTime == null)
            {
                statsLastTime = System.DateTime.Now;
                statsTotalUpdates = 0;
                statsTotalSamplesInInputBuffer = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(statsInterval))
            {
                double samplesInBufferAverage = statsTotalSamplesInInputBuffer / statsTotalUpdates;
                double timeInBufferAverage = samplesInBufferAverage / samples;
                Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={Name()}, fps={statsTotalUpdates / statsInterval}, input_latency_samples={(int)samplesInBufferAverage}, input_latency_ms={(int)(timeInBufferAverage*1000)}");
                statsTotalUpdates = 0;
                statsTotalSamplesInInputBuffer = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalUpdates += 1;
            statsTotalSamplesInInputBuffer += samplesInInputBuffer;
        }
    }
}