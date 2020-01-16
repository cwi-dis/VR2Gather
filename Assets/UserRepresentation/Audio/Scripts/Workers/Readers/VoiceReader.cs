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

        public VoiceReader(MonoBehaviour monoBehaviour, int bufferLength) : base(WorkerType.Init) {
            this.bufferLength = bufferLength;
            circularBufferSize = 320 * 100;
            this.circularBuffer = new float[circularBufferSize];
            this.monoBehaviour = monoBehaviour;
            coroutine = monoBehaviour.StartCoroutine(MicroRecorder());
            Start();
        }

        protected override void Update() {
            base.Update();
            int bytesInAudioBuffer = 0;
            lock (this) {
                if (circularBufferWritePosition < circularBufferReadPosition) bytesInAudioBuffer = (circularBufferSize - circularBufferReadPosition) + circularBufferWritePosition;
                else bytesInAudioBuffer = circularBufferWritePosition - circularBufferReadPosition;
            }
            if (token != null && bytesInAudioBuffer>= bufferLength) {
                lock (this) {
                    System.Array.Copy(circularBuffer, circularBufferReadPosition, writeBuffer, 0, bufferLength);
                    token.currentFloatArray = writeBuffer;
                    token.currentSize = bufferLength;
                    token.latency = NTPTools.GetNTPTime();
                    circularBufferReadPosition = (circularBufferReadPosition+bufferLength) % circularBufferSize;
                }
                Next();
            }
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VoiceReader Sopped");
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
                samples = 16000;//codec.recorderFrequency;1
                // bufferLength = 320;// * 4;//codec.bufferLeght;

                recorder = Microphone.Start(device, true, 1, samples);
                samples = recorder.samples;
                float[] readBuffer = new float[bufferLength];
                writeBuffer = new float[bufferLength];
                Debug.Log($"Using {device}  Frequency {samples} bufferLength {bufferLength} IsRecording {Microphone.IsRecording(device)}");

                int readPosition = 0;
                while (true) {
                    if (token != null ) {
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
                    }
                    yield return null;
                }
            }
            else
                Debug.LogError("No Micros detected.");

        }
    }
}