#define USE_SPEEX
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class VoiceDecoder : BaseWorker {
        QueueThreadSafe inQueue;
        QueueThreadSafe outQueue;

        NSpeex.SpeexDecoder decoder;
        public VoiceDecoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue) : base(WorkerType.Run) {
            inQueue = _inQueue;
            outQueue = _outQueue;
            decoder = new NSpeex.SpeexDecoder(NSpeex.BandMode.Wide);
            // playerFrequency = decoder.SampleRate;
            Debug.Log("VoiceDecoder: Started.");

            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VoiceDecoder: Stopped");
        }

        float[] temporalBuffer;
        float[] receiveBuffer2;
        NTPTools.NTPTime tempTime;
        protected override void Update() {
            base.Update();
            if (inQueue._CanDequeue()) { 
                NativeMemoryChunk mcIn = (NativeMemoryChunk)inQueue.Dequeue();
                if (outQueue._CanEnqueue()) {
#if USE_SPEEX
                    byte[] buffer = new byte[mcIn.length];
                    if (temporalBuffer == null) temporalBuffer = new float[mcIn.length * 10]; // mcIn.length*10
                    System.Runtime.InteropServices.Marshal.Copy(mcIn.pointer, buffer, 0, mcIn.length);
                    int len = decoder.Decode(buffer, 0, mcIn.length, temporalBuffer, 0);
#else
                    int len = mcIn.length / 4;
                    if (temporalBuffer == null) temporalBuffer = new float[len];
                    System.Runtime.InteropServices.Marshal.Copy(mcIn.pointer, temporalBuffer, 0, len);
#endif
                    FloatMemoryChunk mcOut = new FloatMemoryChunk(len * 6);
                    for (int i = 0; i < len; ++i) {
                        mcOut.buffer[i * 6 + 0] =
                        mcOut.buffer[i * 6 + 1] =
                        mcOut.buffer[i * 6 + 2] =
                        mcOut.buffer[i * 6 + 3] =
                        mcOut.buffer[i * 6 + 4] =
                        mcOut.buffer[i * 6 + 5] = temporalBuffer[i];
                    }
                    outQueue.Enqueue(mcOut);
                }
                mcIn.free();
            }
        }
    }
}