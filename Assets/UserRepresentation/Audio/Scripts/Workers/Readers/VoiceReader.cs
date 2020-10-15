using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class VoiceReader : BaseWorker
    {
        MonoBehaviour monoBehaviour;
        Coroutine coroutine;
        float[] writeBuffer;
        float[] circularBuffer;
        int circularBufferReadPosition;
        int circularBufferWritePosition;
        int circularBufferSize;
        QueueThreadSafe outQueue;

        public VoiceReader(string deviceName, MonoBehaviour monoBehaviour, int bufferLength, QueueThreadSafe _outQueue) : base(WorkerType.Init) {
            outQueue = _outQueue;
            this.bufferLength = bufferLength;
            circularBufferSize = 320 * 100;
            this.circularBuffer = new float[circularBufferSize];
            this.monoBehaviour = monoBehaviour;
            coroutine = monoBehaviour.StartCoroutine(MicroRecorder(deviceName));
            Debug.Log($"{Name()}: Started bufferLength {bufferLength}.");
            Start();
        }

        protected override void Update() {
            base.Update();
            int samplesInAudioBuffer = 0;
            lock (circularBuffer) {
                if (circularBufferWritePosition < circularBufferReadPosition)
                {
                    samplesInAudioBuffer = (circularBufferSize - circularBufferReadPosition) + circularBufferWritePosition;
                }
                else
                {
                    samplesInAudioBuffer = circularBufferWritePosition - circularBufferReadPosition;
                }

                if (outQueue._CanEnqueue() && samplesInAudioBuffer >= bufferLength) {
                    statsUpdate(samplesInAudioBuffer);
                    FloatMemoryChunk mc = new FloatMemoryChunk(bufferLength);
                    System.Array.Copy(circularBuffer, circularBufferReadPosition, mc.buffer, 0, bufferLength);
                    outQueue.Enqueue(mc);
                    circularBufferReadPosition = (circularBufferReadPosition + bufferLength) % circularBufferSize;
                }
            }
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
        float       timer;
        float       bufferTime;
        bool        recording = true;
        public const int wantedOutputSampleRate = 16000 * 3;
        public const int wantedOutputBufferSize = 320 * 3;
        public const int wantedInputSampleRate = 16000;

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
                samples = wantedInputSampleRate;
                recorder = Microphone.Start(deviceName, true, 1, samples);
                int samplesPerSecond = recorder.samples;
                float[] readBuffer = new float[bufferLength];
                writeBuffer = new float[bufferLength];
                Debug.Log($"{Name()}: Using {deviceName}  Frequency {samplesPerSecond} (wanted {wantedInputSampleRate} min {currentMinFreq} max {currentMaxFreq}) bufferLength {bufferLength} IsRecording {Microphone.IsRecording(deviceName)}");
                if (samplesPerSecond != wantedInputSampleRate)
                {
                    Debug.LogWarning($"{Name()}: audio input sample rate is {samplesPerSecond} in stead of {wantedInputSampleRate}");
                }
                bufferTime = bufferLength / (float)samples;
                timer = Time.realtimeSinceStartup;

                recording = Microphone.IsRecording(deviceName);

                int readPosition = 0;

                while ( true ) {
                    if (Microphone.IsRecording(deviceName)) {
                        int writePosition = Microphone.GetPosition(deviceName);
                        int available;
                        if (writePosition < readPosition) available = (samples - readPosition) + writePosition;
                        else available = writePosition - readPosition;
                        float lastRead = Time.realtimeSinceStartup;
                        while (available >= bufferLength) {
                            float currentRead = Time.realtimeSinceStartup;
                            lastRead = currentRead;
                            if (!recorder.GetData(readBuffer, readPosition)) {
                                Debug.Log($"{Name()}: ERROR!!! IsRecording {Microphone.IsRecording(deviceName)}");
                                Debug.LogError("Error while getting audio from microphone");
                            }
                            // Write all data from microphone.
                            lock (circularBuffer) {
                                System.Array.Copy(readBuffer, 0, circularBuffer, circularBufferWritePosition, bufferLength);
                                circularBufferWritePosition = (circularBufferWritePosition + bufferLength) % circularBufferSize;
                            }
                            readPosition = (readPosition + bufferLength) % samples;
                            available -= bufferLength;
                        }
                        timer = Time.realtimeSinceStartup;
                    } else {
                        Debug.LogWarning($"{Name()}: microphone {deviceName} stopped recording, starting again.");
                        recorder = Microphone.Start(deviceName, true, 1, samples);
                        readPosition = 0;
                        if ((Time.realtimeSinceStartup - timer) > bufferTime) {
                            timer += bufferTime;
                            lock (circularBuffer) {
                                System.Array.Clear(readBuffer, 0, bufferLength);
                                System.Array.Copy(readBuffer, 0, circularBuffer, circularBufferWritePosition, bufferLength);
                                circularBufferWritePosition = (circularBufferWritePosition + bufferLength) % circularBufferSize;
                            }
                        }

                    }
                    yield return null;
                }
            } else
                Debug.LogError("{Name()}: No Microphones detected.");
        }

        System.DateTime statsLastTime;
        double statsTotalUpdates;
        double statsTotalSamplesInInputBuffer;
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
                double timeInBufferAverage = samplesInBufferAverage / wantedInputSampleRate;
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: {Name()}: {statsTotalUpdates / statsInterval} fps, {(int)samplesInBufferAverage} samples input latency, {(int)(timeInBufferAverage*1000)} ms input latency");
                statsTotalUpdates = 0;
                statsTotalSamplesInInputBuffer = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalUpdates += 1;
            statsTotalSamplesInInputBuffer += samplesInInputBuffer;
        }
    }
}