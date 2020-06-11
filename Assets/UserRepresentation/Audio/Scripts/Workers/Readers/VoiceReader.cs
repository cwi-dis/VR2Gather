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

        public VoiceReader(MonoBehaviour monoBehaviour, int bufferLength, QueueThreadSafe _outQueue) : base(WorkerType.Init) {
            outQueue = _outQueue;
            this.bufferLength = bufferLength;
            circularBufferSize = 320 * 100;
            this.circularBuffer = new float[circularBufferSize];
            this.monoBehaviour = monoBehaviour;
            coroutine = monoBehaviour.StartCoroutine(MicroRecorder());
            Debug.Log($"{Name()}: Started bufferLength {bufferLength}.");
            Start();
        }

        protected override void Update() {
            base.Update();
            int bytesInAudioBuffer = 0;
            lock (circularBuffer) {
                if (circularBufferWritePosition < circularBufferReadPosition) bytesInAudioBuffer = (circularBufferSize - circularBufferReadPosition) + circularBufferWritePosition;
                else bytesInAudioBuffer = circularBufferWritePosition - circularBufferReadPosition;

                if (outQueue._CanEnqueue() && bytesInAudioBuffer >= bufferLength) {
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

        IEnumerator MicroRecorder() {
            var ac = AudioSettings.GetConfiguration();
            ac.sampleRate = 16000 * 3;
            ac.dspBufferSize = 320 * 3;


            AudioSettings.Reset(ac);
            if (Microphone.devices.Length > 0) {
                device = Microphone.devices[0];
                int currentMinFreq;
                Microphone.GetDeviceCaps(device, out currentMinFreq, out samples);
                samples = 16000;
                recorder = Microphone.Start(device, true, 1, samples);
                samples = recorder.samples;
                float[] readBuffer = new float[bufferLength];
                writeBuffer = new float[bufferLength];
                Debug.Log($"{Name()}: Using {device}  Frequency {samples} bufferLength {bufferLength} IsRecording {Microphone.IsRecording(device)}");
                bufferTime = bufferLength / (float)samples;
                timer = Time.realtimeSinceStartup;

                recording = Microphone.IsRecording(device);

                int readPosition = 0;

                while ( true ) {
                    if (Microphone.IsRecording(device)) {
                        int writePosition = Microphone.GetPosition(device);
                        int available;
                        if (writePosition < readPosition) available = (samples - readPosition) + writePosition;
                        else available = writePosition - readPosition;
                        float lastRead = Time.realtimeSinceStartup;
                        while (available >= bufferLength) {
                            float currentRead = Time.realtimeSinceStartup;
                            lastRead = currentRead;
                            if (!recorder.GetData(readBuffer, readPosition)) {
                                Debug.LogError($"{Name()}: ERROR!!! IsRecording {Microphone.IsRecording(device)}");
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
                        if (recording) { recording = false; Debug.LogError($"{Name()}: microphone {device} stops recording."); }
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
    }
}