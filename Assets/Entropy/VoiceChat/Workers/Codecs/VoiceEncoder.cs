using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class VoiceEncoder : BaseWorker
    {
        public int bufferSize { get; private set; }
        int frames;
        NSpeex.SpeexEncoder encoder;
        public VoiceEncoder(int frames=1) : base(WorkerType.Run) {
            this.frames = frames;
            encoder = new NSpeex.SpeexEncoder(NSpeex.BandMode.Wide);
            bufferSize = encoder.FrameSize * frames;
            encoder.Quality = 5;
            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VoiceEncoder Sopped");
        }

        byte[]          sendBuffer;
        System.IntPtr   sendBufferPtr;
        public byte     counter = 0;
        protected override void Update() {
            const int offset = 1 + 8;
            base.Update();
            int len;
            if (token != null) {
                if (sendBuffer == null) {
                    byte[] tmp = new byte[token.currentSize];
                    len = encoder.Encode(token.currentFloatArray, 0, bufferSize, tmp, 0, token.currentSize);
                    sendBuffer = new byte[len + offset];
                    sendBufferPtr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(sendBuffer, 0);
                }
                len = encoder.Encode(token.currentFloatArray, 0, bufferSize, sendBuffer, offset, sendBuffer.Length - offset);

                token.currentByteArray = sendBuffer;
                token.currentBuffer= sendBufferPtr;
                token.currentSize = sendBuffer.Length;
                Next();                
            }
        }
    }
}
