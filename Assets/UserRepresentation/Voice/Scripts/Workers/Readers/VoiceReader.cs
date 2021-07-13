﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class VoiceReader : BaseWorker
    {
        Coroutine coroutine;
        QueueThreadSafe outQueue;

        public VoiceReader(string deviceName, MonoBehaviour monoBehaviour, int bufferLength, QueueThreadSafe _outQueue) : base(WorkerType.Init)
        {
            stats = new Stats(Name());
            outQueue = _outQueue;
            this.bufferLength = bufferLength;
            device = deviceName;
            coroutine = monoBehaviour.StartCoroutine(MicroRecorder(deviceName));
            Debug.Log($"{Name()}: Started bufferLength {bufferLength}.");
            Start();
        }

        long sampleTimestamp(int nSamplesInInputBuffer)
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            double timestamp = sinceEpoch.TotalMilliseconds;
            timestamp -= (1000 * nSamplesInInputBuffer / wantedOutputSampleRate);
            return (long)timestamp;
        }
        protected override void Update()
        {
            base.Update();
        }

        public override void OnStop()
        {
            base.OnStop();
            Debug.Log($"{Name()}: Stopped microphone {device}.");
            outQueue.Close();
        }

        string device;
        int recorderBufferSize;
        int bufferLength;
        AudioClip recorder;
        public const int wantedOutputSampleRate = 48000;
        public const int wantedOutputFPS = 50;
        public const int wantedOutputBufferSize = wantedOutputSampleRate / wantedOutputFPS;

        static bool DSPIsNotReady = true;
        public static void PrepareDSP()
        {
            if (DSPIsNotReady)
            {
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

        IEnumerator MicroRecorder(string deviceName)
        {
            PrepareDSP();
            if (Microphone.devices.Length > 0)
            {
                if (deviceName == null) deviceName = Microphone.devices[0];
                int currentMinFreq;
                int currentMaxFreq;
                Microphone.GetDeviceCaps(deviceName, out currentMinFreq, out currentMaxFreq);
                // We record a looping clip of 1 second.
                const int recorderBufferDuration = 1; 
                recorder = Microphone.Start(deviceName, true, recorderBufferDuration, currentMaxFreq);
                recorderBufferSize = recorder.samples * recorder.channels;
                // We expect the recorder clip to contain an integral number of
                // buffers, because we are going to use it as a circular buffer.
                if (recorderBufferSize % bufferLength != 0)
                {
                    Debug.LogError($"VoiceReader: Incorrect clip size {recorderBufferSize} for buffer size {bufferLength}");
                }
                float inc = 1; // was: recorderBufferSize / 16000f;
                int neededBufferLength = (int)(bufferLength * inc);
                float[] readBuffer = new float[neededBufferLength];
                Debug.Log($"{Name()}: Using {deviceName}  Frequency {recorderBufferSize} bufferLength {bufferLength} IsRecording {Microphone.IsRecording(deviceName)} inc {inc}");

                int readPosition = 0;

                while (true)
                {
                    if (Microphone.IsRecording(deviceName))
                    {
                        int writePosition = Microphone.GetPosition(deviceName);
                        int available;
                        if (writePosition < readPosition) available = recorderBufferSize - readPosition + writePosition;
                        else available = writePosition - readPosition;
                        while (available >= neededBufferLength)
                        {
                            if (!recorder.GetData(readBuffer, readPosition))
                            {
                                Debug.Log($"{Name()}: ERROR!!! IsRecording {Microphone.IsRecording(deviceName)}");
                                Debug.LogError("Error while getting audio from microphone");
                            }
                            // Write all data from microphone.
                            lock (outQueue)
                            {
                                FloatMemoryChunk mc = new FloatMemoryChunk(bufferLength);
                                float idx = 0;
                                for (int i = 0; i < bufferLength; i++)
                                {
                                    mc.buffer[i] = readBuffer[(int)idx];
                                    idx += inc;
                                }
                                mc.info = new FrameInfo();
                                // We need to compute timestamp of this audio frame
                                // by using system clock and adjusting with "available".
                                mc.info.timestamp = sampleTimestamp(available);
                                bool ok = outQueue.Enqueue(mc);
                                stats.statsUpdate(available, !ok);
                            }
                            readPosition = (readPosition + neededBufferLength) % recorderBufferSize;
                            available -= neededBufferLength;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{Name()}: microphone {deviceName} stopped recording, starting again.");
                        recorder = Microphone.Start(deviceName, true, recorderBufferDuration, currentMaxFreq);
                        readPosition = 0;
                    }
                    yield return null;
                }
            }
            else
                Debug.LogError("{Name()}: No Microphones detected.");
        }
        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalUpdates;
            double statsTotalSamplesInInputBuffer;
            double statsDrops;

            public void statsUpdate(int samplesInInputBuffer, bool dropped)
            {

                statsTotalUpdates += 1;
                statsTotalSamplesInInputBuffer += samplesInInputBuffer;
                if (dropped) statsDrops++;

                if (ShouldOutput())
                {
                    double samplesInBufferAverage = statsTotalSamplesInInputBuffer / statsTotalUpdates;
                    double timeInBufferAverage = samplesInBufferAverage / VoiceReader.wantedOutputSampleRate;
                    Output($"fps={statsTotalUpdates / Interval():F3}, record_latency_samples={(int)samplesInBufferAverage}, record_latency_ms={(int)(timeInBufferAverage * 1000)}, fps_dropped={statsDrops / Interval()}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalUpdates = 0;
                    statsTotalSamplesInInputBuffer = 0;
                    statsDrops = 0;
                }
            }
        }

        protected Stats stats;
    }
}