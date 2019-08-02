using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class VoiceReader : BaseWorker
    {
        MonoBehaviour monoBehaviour;
        Coroutine coroutine;
        bool bReady;
        float[] buffer;
        public VoiceReader(MonoBehaviour monoBehaviour) : base(WorkerType.Init) {
            this.monoBehaviour = monoBehaviour;
            coroutine = monoBehaviour.StartCoroutine(MicroRecorder());
            Start();
        }

        protected override void Update() {
            base.Update();
            if (token != null && bReady) {
                token.currentFloatArray = buffer;
                token.currentSize = buffer.Length;
                bReady = false;
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
                bufferLength = 320;//codec.bufferLeght;

                recorder = Microphone.Start(device, true, 1, samples);
                samples = recorder.samples;
                buffer = new float[bufferLength];
                Debug.Log($"Using {device}  Frequency {samples} bufferLength {bufferLength} IsRecording {Microphone.IsRecording(device)}");

                int readPosition = 0;
                while (true) {
                    if (token != null && !bReady) {
                        int writePosition = Microphone.GetPosition(device);
                        int available;
                        if (writePosition < readPosition)   available = (samples - readPosition) + writePosition;
                        else                                available = writePosition - readPosition;
                        if (available > bufferLength) {
                            recorder.GetData(buffer, readPosition);
                            token.latency = NTPTools.GetNTPTime();
                            readPosition = (readPosition + bufferLength) % samples;
                            bReady = true;
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