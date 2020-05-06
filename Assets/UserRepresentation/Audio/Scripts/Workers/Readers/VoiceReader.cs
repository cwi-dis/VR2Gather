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
            Debug.Log($"VoiceReader: Started bufferLength {bufferLength}.");
            Start();
        }

        protected override void Update() {
            base.Update();
            int bytesInAudioBuffer = 0;
            lock (circularBuffer) {
                if (circularBufferWritePosition < circularBufferReadPosition) bytesInAudioBuffer = (circularBufferSize - circularBufferReadPosition) + circularBufferWritePosition;
                else bytesInAudioBuffer = circularBufferWritePosition - circularBufferReadPosition;

                if (outQueue.Count < 16 && bytesInAudioBuffer >= bufferLength) {
                    FloatMemoryChunk mc = new FloatMemoryChunk(bufferLength);
                    System.Array.Copy(circularBuffer, circularBufferReadPosition, mc.buffer, 0, bufferLength);
                    outQueue.Enqueue(mc);
                    circularBufferReadPosition = (circularBufferReadPosition + bufferLength) % circularBufferSize;
                }
            }
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log($"VoiceReader: Stopped device {device}.");
        }

        string      device;
        int         samples;
        int         bufferLength;
        AudioClip   recorder;
        float       timer;
        float       bufferTime;
        bool        recording = true;

        IEnumerator MicroRecorder() {
            if (Microphone.devices.Length > 0) {
                device = Microphone.devices[0];
                int currentMinFreq;
                Microphone.GetDeviceCaps(null, out currentMinFreq, out samples);
                samples = 16000;
                recorder = Microphone.Start(null, true, 1, samples);
                samples = recorder.samples;
                float[] readBuffer = new float[bufferLength];
                writeBuffer = new float[bufferLength];
                Debug.Log($"VoiceReader: Using {device}  Frequency {samples} bufferLength {bufferLength} IsRecording {Microphone.IsRecording(null)}");
                bufferTime = bufferLength / (float)samples;
                timer = Time.realtimeSinceStartup;

                int readPosition = 0;

                while ( true ) {
                    if (Microphone.IsRecording(null)) {
                        int writePosition = Microphone.GetPosition(null);
                        int available;
                        if (writePosition < readPosition) available = (samples - readPosition) + writePosition;
                        else available = writePosition - readPosition;
                        float lastRead = Time.realtimeSinceStartup;
                        while (available >= bufferLength) {
                            float currentRead = Time.realtimeSinceStartup;
                            lastRead = currentRead;
                            if (!recorder.GetData(readBuffer, readPosition)) {
                                Debug.LogError($"VoiceReader: ERROR!!! IsRecording {Microphone.IsRecording(null)}");
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
                        if (recording) { recording = false; Debug.LogError($"VoiceReader: microphone {device} stops recording."); }
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
                Debug.LogError("VoiceReader: No Microphones detected.");
        }
    }
}