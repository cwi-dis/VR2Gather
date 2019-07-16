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
            bufferLength = 320;
            // playerFrequency = decoder.SampleRate;
            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VoiceDecoder Sopped");
        }

        float[] receiveBuffer;
        float[] receiveBuffer2;
        NTPTools.NTPTime tempTime;
        protected override void Update() {
            const int offset = 1 + 8;
            base.Update();
            if (token != null) {
                if (receiveBuffer == null)
                {
                    receiveBuffer = new float[bufferLength];
                    receiveBuffer2 = new float[bufferLength*3*2]; // Frequency*stereo
                }

                tempTime.SetByteArray( token.currentByteArray, 1);
                token.latency = tempTime;           

                decoder.Decode(token.currentByteArray, offset, token.currentByteArray.Length - offset, receiveBuffer);
                // Fix frequency and stereo.
                for( int i=0;i< bufferLength; ++i) {
                    receiveBuffer2[i * 6 + 0] = receiveBuffer2[i * 6 + 1] = receiveBuffer2[i * 6 + 2] = receiveBuffer2[i * 6 + 3] = receiveBuffer2[i * 6 + 4] = receiveBuffer2[i * 6 + 5] = receiveBuffer[i];
                }
                token.currentFloatArray = receiveBuffer2;
                token.currentSize = receiveBuffer2.Length;
                Next();
            }
        }
    }
}