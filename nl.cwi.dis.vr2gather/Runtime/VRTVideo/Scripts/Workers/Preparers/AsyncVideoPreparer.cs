using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.Video
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;

    public class AsyncVideoPreparer : AsyncPreparer, IVideoPreparer
    {
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


        QueueThreadSafe inAudioQueue;

        public int videFrameSize;

        public AsyncVideoPreparer(QueueThreadSafe _inVideoQueue, QueueThreadSafe _inAudioQueue) : base(_inVideoQueue)
        {
            NoUpdateCallsNeeded();
            inAudioQueue = _inAudioQueue;

            audioBufferSize = 24000 * 8;
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

        protected override void AsyncUpdate()
        {
        }

        public override void Synchronize()
        {
            // Synchronize playout for the current frame with other preparers (if needed)
        }

        public override bool LatchFrame()
        {
            bool didReadData = false;
            if (InQueue != null && InQueue._CanDequeue())
            {
                didReadData = true;
                NativeMemoryChunk mc = (NativeMemoryChunk)InQueue._Peek();
                int len = mc.length;
                videFrameSize = len;
                if (videoBufferSize == 0)
                {
                    videoBufferSize = len * 15;
                    circularVideoBuffer = new byte[videoBufferSize];
                    circularVideoBufferPtr = Marshal.UnsafeAddrOfPinnedArrayElement(circularVideoBuffer, 0);
                }

                if (len < freeVideo)
                {
                    lock (this)
                    {
                        mc = (NativeMemoryChunk)InQueue.Dequeue();
                        if (writeVideoPosition + len < videoBufferSize)
                        {
                            Marshal.Copy(mc.pointer, circularVideoBuffer, writeVideoPosition, len);
                            writeVideoPosition += len;
                        }
                        else
                        {
                            int partLen = videoBufferSize - writeVideoPosition;
                            Marshal.Copy(mc.pointer, circularVideoBuffer, writeVideoPosition, partLen);
                            Marshal.Copy(mc.pointer + partLen, circularVideoBuffer, 0, len - partLen);
                            writeVideoPosition = len - partLen;
                        }
                        availableVideo += len;
                    }
                    mc.free();
                }
                else
                {
                    // Debug.LogError($"{Name()}: CircularBuffer is full");
                }

            }

            if (inAudioQueue != null && inAudioQueue._CanDequeue())
            {
                didReadData = true;
                FloatMemoryChunk mc = (FloatMemoryChunk)inAudioQueue._Peek();
                int len = mc.elements;
                if (len < freeAudio)
                {
                    mc = (FloatMemoryChunk)inAudioQueue.Dequeue();
                    if (writeAudioPosition + len < audioBufferSize)
                    {
                        Marshal.Copy(mc.pointer, circularAudioBuffer, writeAudioPosition, len);
                        writeAudioPosition += len;
                    }
                    else
                    {
                        int partLen = audioBufferSize - writeAudioPosition;
                        Marshal.Copy(mc.pointer, circularAudioBuffer, writeAudioPosition, partLen);
                        Marshal.Copy(mc.pointer + partLen, circularAudioBuffer, 0, len - partLen);
                        writeAudioPosition = len - partLen;
                    }
                    mc.free();
                    lock (this) { availableAudio += len; }
                }

            }
            return didReadData;
        }

        private int availableAudio { get; set; }
        public int availableVideo { get; private set; }
        private int freeAudio { get { return audioBufferSize - availableAudio; } }
        private int freeVideo { get { return videoBufferSize - availableVideo; } }

        bool firstTime = true;
        public int GetAudioBuffer(float[] dst, int len)
        {
            if (firstTime && availableAudio >= len || !firstTime)
            {
                firstTime = false;
                if (availableAudio >= len)
                {
                    if (writeAudioPosition < readAudioPosition)
                    { // Se ha dado la vuelta.
                        int partLen = audioBufferSize - readAudioPosition;
                        if (partLen > len)
                        {
                            System.Array.Copy(circularAudioBuffer, readAudioPosition, dst, 0, len);
                            readAudioPosition += len;
                        }
                        else
                        {
                            System.Array.Copy(circularAudioBuffer, readAudioPosition, dst, 0, partLen);
                            System.Array.Copy(circularAudioBuffer, 0, dst, partLen, len - partLen);
                            readAudioPosition = len - partLen;
                        }
                    }
                    else
                    {
                        System.Array.Copy(circularAudioBuffer, readAudioPosition, dst, 0, len);
                        readAudioPosition += len;
                    }
                    lock (this) { availableAudio -= len; }
                    return 0;
                }
                else
                    Debug.Log($"{Name()}: GetAudioBuffer: want {len} bytes but only {availableAudio} available");
            }
            return len;
        }

        public System.IntPtr GetVideoPointer(int len)
        {
            var ret = circularVideoBufferPtr + readVideoPosition;
            readVideoPosition += len;
            if (readVideoPosition >= videoBufferSize) readVideoPosition -= videoBufferSize;
            availableVideo -= len;
            return ret;
        }
    }
}
