using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Workers
{
    public class VoiceEncoder : BaseWorker
    {
        public int          bufferSize { get; private set; }
        int                 frames;
        NSpeex.SpeexEncoder encoder;

        QueueThreadSafe inQueue;
        QueueThreadSafe outQueue;
        public VoiceEncoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue, int frames =1) : base(WorkerType.Run) {
            inQueue = _inQueue;
            outQueue = _outQueue;
            this.frames = frames;
            encoder = new NSpeex.SpeexEncoder( NSpeex.BandMode.Wide );
            bufferSize = encoder.FrameSize * frames;
            encoder.Quality = 5;
            Debug.Log("VoiceEncoder: Started.");
            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VoiceEncoder: Stopped.");
        }

        byte[]          sendBuffer;
        protected override void Update() {
            base.Update();
            if (inQueue.Count >0 ) {
                FloatMemoryChunk mcIn = (FloatMemoryChunk)inQueue.Dequeue();
                if (sendBuffer == null) sendBuffer = new byte[(int)(mcIn.length)]; // La priemera vez
                // Necesito calcular el tamaño del buffer.
                if (outQueue.Count < 2) {
                    int len = encoder.Encode(mcIn.buffer, 0, mcIn.elements, sendBuffer, 0, sendBuffer.Length);
                    mcIn.free();
                    NativeMemoryChunk mcOut = new NativeMemoryChunk(len);
                    Marshal.Copy(sendBuffer, 0, mcOut.pointer, len);
                    outQueue.Enqueue(mcOut);
                    Debug.Log("FPA_TEMP: VoiceEncoder: Enqueue compressed buffer.");

                } else
                    mcIn.free();
            }
        }
    }
}
