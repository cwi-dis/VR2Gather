using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class VoiceDecoder : BaseWorker
    {
        int bufferLength;
        
        NSpeex.SpeexDecoder decoder;
        public VoiceDecoder() : base(WorkerType.Run) {            
            decoder = new NSpeex.SpeexDecoder(NSpeex.BandMode.Wide);
            // playerFrequency = decoder.SampleRate;
            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VoiceDecoder Stopped");
        }

        float[] receiveBuffer;
        float[] receiveBuffer2;
        NTPTools.NTPTime tempTime;
        protected override void Update() {
            const int offset = 1 + 8;
            base.Update();
            if (token != null) {
                lock (token) {
                    int ret;
                    if (receiveBuffer == null) {
                        int max = (token.currentSize - offset) * 8;
                        float[] aux = new float[max];
                        bufferLength = decoder.Decode(token.currentByteArray, offset, token.currentSize - offset, aux);
                        receiveBuffer = new float[bufferLength];
                        receiveBuffer2 = new float[bufferLength * 3 * 2]; // Frequency*stereo
                    }

                    //tempTime.SetByteArray(token.currentByteArray, 0); // NTP Reading
                    int len = token.currentSize - offset;
                    ret = decoder.Decode(token.currentByteArray, offset, token.currentSize - offset, receiveBuffer);
                    // Fix frequency and stereo.
                    for (int i = 0; i < bufferLength; ++i) {
                        receiveBuffer2[i * 6 + 0] = receiveBuffer2[i * 6 + 1] = receiveBuffer2[i * 6 + 2] = receiveBuffer2[i * 6 + 3] = receiveBuffer2[i * 6 + 4] = receiveBuffer2[i * 6 + 5] = receiveBuffer[i];
                    }
                    //token.latency = tempTime;
                    //Debug.Log(NTPTools.GetNTPTime().time - tempTime.time);
                    token.currentFloatArray = receiveBuffer2;
                    token.currentSize = receiveBuffer2.Length;
                    Next();
                }
            }
        }
    }
}