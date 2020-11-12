using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Workers {
    public class VideoPreparer : BaseWorker {
        float[] circularAudioBuffer;
        int audioBufferSize;
        byte[] circularVideoBuffer;
        System.IntPtr circularVideoBufferPtr;
        int videoBufferSize;
        int writeAudioPosition;
        int readAudioPosition;

        int writeVideoPosition;
        int readVideoPosition;
        VideoFilter RGBA2RGBFilter;


        QueueThreadSafe inVideoQueue;
        QueueThreadSafe inAudioQueue;

        public int videFrameSize;

        public VideoPreparer(QueueThreadSafe _inVideoQueue, QueueThreadSafe _inAudioQueue) : base(WorkerType.End) {
            inVideoQueue = _inVideoQueue;
            inAudioQueue = _inAudioQueue;

            audioBufferSize = 24000*8;
            circularAudioBuffer = new float[audioBufferSize];
            availableAudio = 0;
            availableVideo = 0;


            writeAudioPosition = 0;
            readAudioPosition = 0;

            videoBufferSize = 0;
            writeVideoPosition = 0;
            readVideoPosition = 0;

            videFrameSize = 0;

            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log($"{Name()}: Stopped");
        }

        protected override void Update() {
            base.Update();
            if (inVideoQueue != null && inVideoQueue._CanDequeue()) {

                NativeMemoryChunk mc = (NativeMemoryChunk)inVideoQueue._Peek();
                int len = mc.length;
                videFrameSize = len;
                if (videoBufferSize == 0) {
                    videoBufferSize = len * 15;
                    circularVideoBuffer = new byte[videoBufferSize];
                    circularVideoBufferPtr = Marshal.UnsafeAddrOfPinnedArrayElement(circularVideoBuffer, 0);
                }
                
                if (len < freeVideo) {
                    lock (this) {
                        mc = (NativeMemoryChunk)inVideoQueue.Dequeue();
                        if (writeVideoPosition + len < videoBufferSize) {
                            Marshal.Copy(mc.pointer, circularVideoBuffer, writeVideoPosition, len);
                            writeVideoPosition += len;
                        } else {
                            int partLen = videoBufferSize - writeVideoPosition;
                            Marshal.Copy(mc.pointer, circularVideoBuffer, writeVideoPosition, partLen);
                            Marshal.Copy(mc.pointer + partLen, circularVideoBuffer, 0, len - partLen);
                            writeVideoPosition = len - partLen;
                        }
                        availableVideo += len; 
                    }
                    mc.free();
                } else {
                    // Debug.LogError($"{Name()}: CircularBuffer is full");
                }
                
            }

            if (inAudioQueue!=null && inAudioQueue._CanDequeue()) {
                FloatMemoryChunk mc = (FloatMemoryChunk)inAudioQueue._Peek();
                int len = mc.elements;
                if (len < freeAudio) {
                    mc = (FloatMemoryChunk)inAudioQueue.Dequeue();
                    if (writeAudioPosition + len < audioBufferSize) {
                        Marshal.Copy(mc.pointer, circularAudioBuffer, writeAudioPosition, len);
                        writeAudioPosition += len;
                    } else {
                        int partLen = audioBufferSize - writeAudioPosition;
                        Marshal.Copy(mc.pointer, circularAudioBuffer, writeAudioPosition, partLen);
                        Marshal.Copy(mc.pointer + partLen, circularAudioBuffer, 0, len - partLen);
                        writeAudioPosition = len - partLen;
                    }
                    mc.free();
                    lock (this) { availableAudio += len; }
                }
                
            }
        }

        public int availableAudio { get; private set; }
        public int availableVideo { get; private set; }
        public int freeAudio { get { return audioBufferSize - availableAudio; } }
        public int freeVideo { get { return videoBufferSize - availableVideo; } }

        bool firstTime = true;
        public  bool GetAudioBuffer(float[] dst, int len) {
            if ((firstTime && availableAudio >= len) || !firstTime) {
                firstTime = false;
                if (availableAudio >= len) {
                    if (writeAudioPosition < readAudioPosition) { // Se ha dado la vuelta.
                        int partLen = audioBufferSize - readAudioPosition;
                        if (partLen > len) {
                            System.Array.Copy(circularAudioBuffer, readAudioPosition, dst, 0, len);
                            readAudioPosition += len;
                        } else {
                            System.Array.Copy(circularAudioBuffer, readAudioPosition, dst, 0, partLen);
                            System.Array.Copy(circularAudioBuffer, 0, dst, partLen, len - partLen);
                            readAudioPosition = len - partLen;
                        }
                    } else {
                        System.Array.Copy(circularAudioBuffer, readAudioPosition, dst, 0, len);
                        readAudioPosition += len;
                    }
                    lock (this) { availableAudio -= len; }
                    return true;
                } else
                    Debug.Log($"{Name()}: Buffer audio sin datos.");
            }
            return false;
        }

        public System.IntPtr GetVideoPointer(int len) {
            var ret = circularVideoBufferPtr + readVideoPosition;
            readVideoPosition += len;
            if (readVideoPosition >= videoBufferSize) readVideoPosition -= videoBufferSize;
            availableVideo -= len;
            return ret;
        }
    }
}
