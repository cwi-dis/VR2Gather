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
        NTPTools.NTPTime tempTime;
        protected override void Update() {
            const int offset = 1 + 8;
            base.Update();
            if (token != null) {
                if (receiveBuffer == null) receiveBuffer = new float[bufferLength];

                tempTime.SetByteArray( token.currentByteArray, 1);
                token.latency = tempTime;           

                decoder.Decode(token.currentByteArray, offset, token.currentByteArray.Length - offset, receiveBuffer);
                token.currentFloatArray = receiveBuffer;
                token.currentSize = bufferLength;
                Next();
            }
        }
    }
}