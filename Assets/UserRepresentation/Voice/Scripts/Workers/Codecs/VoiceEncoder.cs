#define USE_SPEEX
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using VRTCore;

namespace Voice
{
    public class VoiceEncoder : BaseWorker
    {
        public int bufferSize { get; private set; }
        int frames;
        NSpeex.SpeexEncoder encoder;

        QueueThreadSafe inQueue;
        QueueThreadSafe outQueue;
        public VoiceEncoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue, int frames = 1) : base(WorkerType.Run)
        {
            inQueue = _inQueue;
            outQueue = _outQueue;
            this.frames = frames;
            encoder = new NSpeex.SpeexEncoder(NSpeex.BandMode.Wide);
            bufferSize = encoder.FrameSize * frames;
            encoder.Quality = 5;
            Debug.Log($"{Name()}: Started.");
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            outQueue?.Close();
            outQueue = null;
            Debug.Log($"{Name()}: Stopped.");
        }

        byte[] sendBuffer;
        protected override void Update()
        {
            base.Update();
            if (!inQueue.IsClosed()) {
                FloatMemoryChunk mcIn = (FloatMemoryChunk)inQueue.Dequeue();
                if (mcIn == null) return;
                if (sendBuffer == null) sendBuffer = new byte[(int)(mcIn.length)];
#if USE_SPEEX
                int len = encoder.Encode(mcIn.buffer, 0, mcIn.elements, sendBuffer, 0, sendBuffer.Length);
                NativeMemoryChunk mcOut = new NativeMemoryChunk(len);
                Marshal.Copy(sendBuffer, 0, mcOut.pointer, len);
#else
            int len = mcIn.elements;
            NativeMemoryChunk mcOut = new NativeMemoryChunk(len*4);
            Marshal.Copy(mcIn.buffer, 0, mcOut.pointer, len); // numero de elementos de la matriz.
#endif
                if (!outQueue.IsClosed())
                    outQueue.Enqueue(mcOut);
                mcIn.free();
            }
        }
    }
}
