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
            Debug.Log("VoiceReader: Started.");
            Start();
        }

        protected override void Update() {
            base.Update();
            int bytesInAudioBuffer = 0;
            lock (this) {
                if (circularBufferWritePosition < circularBufferReadPosition) bytesInAudioBuffer = (circularBufferSize - circularBufferReadPosition) + circularBufferWritePosition;
                else bytesInAudioBuffer = circularBufferWritePosition - circularBufferReadPosition;
            }
            if (outQueue.Count < 2 && bytesInAudioBuffer>= bufferLength) {
                lock (this) {
                    FloatMemoryChunk mc = new FloatMemoryChunk( bufferLength );
                    System.Array.Copy(circularBuffer, circularBufferReadPosition, mc.buffer, 0, bufferLength);
                    mc.timeStamp = NTPTools.GetNTPTime();
                    outQueue.Enqueue(mc);
                    circularBufferReadPosition = (circularBufferReadPosition+bufferLength) % circularBufferSize;
                }
            }
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VoiceReader: Stopped.");
        }

        string device;
        int         samples;
        int         bufferLength;
        AudioClip   recorder;
        IEnumerator MicroRecorder() {
            if (Microphone.devices.Length > 0) {
                device = Microphone.devices[0];
                int currentMinFreq;
                Microphone.GetDeviceCaps(device, out currentMinFreq, out samples);
                samples = 16000;
                recorder = Microphone.Start(device, true, 1, samples);
                samples = recorder.samples;
                float[] readBuffer = new float[bufferLength];
                writeBuffer = new float[bufferLength];
                Debug.Log($"Using {device}  Frequency {samples} bufferLength {bufferLength} IsRecording {Microphone.IsRecording(device)}");

                int readPosition = 0;
                while (true) {
                    int writePosition = Microphone.GetPosition(device);
                    int available;
                    if (writePosition < readPosition)   available = (samples - readPosition) + writePosition;
                    else                                available = writePosition - readPosition;
                    while(available >= bufferLength) {
                        recorder.GetData(readBuffer, readPosition);
                        // Write all data from microphone.
                        lock (this) {
                            System.Array.Copy(readBuffer, 0, circularBuffer, circularBufferWritePosition, bufferLength);
                            circularBufferWritePosition = (circularBufferWritePosition + bufferLength) % circularBufferSize;
                            available -= bufferLength;
                        }
                        readPosition = (readPosition + bufferLength) % samples;
                    }
                    yield return null;
                }
            }
            else
                Debug.LogError("No Micros detected.");

        }
    }
}