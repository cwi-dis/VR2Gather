using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class VoiceEncoder : BaseWorker
    {
        NSpeex.SpeexEncoder encoder;
        public VoiceEncoder() : base(WorkerType.Run) {
            encoder = new NSpeex.SpeexEncoder(NSpeex.BandMode.Wide);
            encoder.Quality = 5;
            Start();
            //            bufferLeght = encoder.FrameSize;
            //            recorderFrequency = encoder.SampleRate;
        }

        public override void OnStop() {
            base.OnStop();
        }

        byte[]          sendBuffer;
        System.IntPtr   sendBufferPtr;

        protected override void Update() {
            const int offset = 1 + 8;
            base.Update();
            if (token != null) {
                if (sendBuffer == null) {
                    byte[] tmp = new byte[token.currentSize];
                    int len = encoder.Encode(token.currentFloatArray, 0, 1, tmp, 0, token.currentSize);
                    sendBuffer = new byte[len + offset];
                    sendBufferPtr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(sendBuffer, 0);
                }
                encoder.Encode(token.currentFloatArray, 0, 1, sendBuffer, offset, sendBuffer.Length - offset);
                token.currentBuffer= sendBufferPtr;
                token.currentSize = sendBuffer.Length;
                Next();                
            }
        }
    }
}
