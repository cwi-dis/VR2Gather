using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class VoiceReader : BaseWorker
    {
        Coroutine coroutine;
        QueueThreadSafe outQueue;

        public VoiceReader(string deviceName, int sampleRate, int fps, int minBufferSize, MonoBehaviour monoBehaviour, QueueThreadSafe _outQueue) : base()
        {
            stats = new Stats(Name());
            outQueue = _outQueue;
            device = deviceName;
            coroutine = monoBehaviour.StartCoroutine(MicroRecorder(deviceName, sampleRate, fps, minBufferSize));
            Debug.Log($"{Name()}: Started bufferLength {bufferLength}.");
            Start();
        }

        public int getBufferSize()
        {
            return bufferLength;
        }

        long sampleTimestamp(int nSamplesInInputBuffer)
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            double timestamp = sinceEpoch.TotalMilliseconds;
            timestamp -= (1000 * nSamplesInInputBuffer / wantedSampleRate);
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

        int wantedSampleRate;
        
        public static void PrepareDSP(int _sampleRate, int _bufferSize)
        {
            var ac = AudioSettings.GetConfiguration();
            if (_sampleRate == 0) _sampleRate = ac.sampleRate;
            if (_bufferSize != 0) _bufferSize = ac.dspBufferSize;

            if (_sampleRate != ac.sampleRate || _bufferSize != ac.dspBufferSize)
            {
                ac.sampleRate = _sampleRate;
                ac.dspBufferSize = _bufferSize;
                AudioSettings.Reset(ac);
                ac = AudioSettings.GetConfiguration();
                if (ac.sampleRate != _sampleRate)
                {
                    Debug.LogError($"Audio output sample rate is {ac.sampleRate} in stead of {_sampleRate}. Other participants may sound funny.");
                }
                if (ac.dspBufferSize != _bufferSize)
                {
                    Debug.LogWarning($"PrepareDSP: audio output buffer is {ac.dspBufferSize} in stead of {_bufferSize}");
                }
              
            }

        }

        IEnumerator MicroRecorder(string deviceName, int _sampleRate, int _fps, int _minBufferSize)
        {
            wantedSampleRate = _sampleRate;
            bufferLength = wantedSampleRate / _fps;
            if (_minBufferSize > 0 && bufferLength % _minBufferSize != 0)
            {
                // Round up to a multiple of _minBufferSize
                bufferLength = ((bufferLength + _minBufferSize - 1) / _minBufferSize) * _minBufferSize;
                float actualFps = (float)wantedSampleRate / bufferLength;
                Debug.LogWarning($"{Name()}: adapted bufferSize={bufferLength}, fps={actualFps}");
            }
            if (wantedSampleRate % bufferLength != 0)
            {
                Debug.LogWarning($"{Name()}: non-integral number of buffers per second. This may not work.");
            }
            PrepareDSP(wantedSampleRate, bufferLength);
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
                                readPosition = (readPosition + bufferLength) % recorderBufferSize;
                                available -= bufferLength;
                                mc.info = new FrameInfo();
                                // We need to compute timestamp of this audio frame
                                // by using system clock and adjusting with "available".
                                mc.info.timestamp = sampleTimestamp(available);
                                double timeRemainingInBuffer = (double)available / wantedSampleRate;
                                bool ok = outQueue.Enqueue(mc);
                                stats.statsUpdate(timeRemainingInBuffer, !ok, outQueue.QueuedDuration());
                            }
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
            double statsTotalTimeInInputBuffer;
            double statsTotalQueuedDuration;
            double statsDrops;

            public void statsUpdate(double timeInInputBuffer, bool dropped, ulong queuedDuration)
            {

                statsTotalUpdates += 1;
                statsTotalTimeInInputBuffer += timeInInputBuffer;
                statsTotalQueuedDuration += queuedDuration;
                if (dropped) statsDrops++;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalUpdates / Interval():F3}, record_latency_ms={(int)(statsTotalTimeInInputBuffer * 1000 / statsTotalUpdates)}, output_queue_ms={(int)(statsTotalQueuedDuration / statsTotalUpdates)}, fps_dropped={statsDrops / Interval()}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalUpdates = 0;
                    statsTotalTimeInInputBuffer = 0;
                    statsTotalQueuedDuration = 0;
                    statsDrops = 0;
                }
            }
        }

        protected Stats stats;
    }
}