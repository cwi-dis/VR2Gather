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

        public VideoPreparer() : base(WorkerType.End) {
            audioBufferSize = 24000*8;
            circularAudioBuffer = new float[audioBufferSize];
            writeAudioPosition = 0;
            readAudioPosition = 0;

            videoBufferSize = 0;
            writeVideoPosition = 0;
            readVideoPosition = 0;

            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VideoPreparer Stopped");
        }

        protected override void Update() {
            base.Update();
            if (token != null) {
                lock (token) {
                    if (token.isVideo) {
                        int len = token.currentSize;
                        if (videoBufferSize == 0) {
                            videoBufferSize = len * 15;
                            circularVideoBuffer = new byte[videoBufferSize];
                            circularVideoBufferPtr = Marshal.UnsafeAddrOfPinnedArrayElement(circularVideoBuffer, 0);
                        }

                        if (writeVideoPosition + len < videoBufferSize) {
                            Marshal.Copy(token.currentBuffer, circularVideoBuffer, writeVideoPosition, len);
                            writeVideoPosition += len;
                        } else {
                            int partLen = videoBufferSize - writeVideoPosition;
                            Marshal.Copy(token.currentBuffer, circularVideoBuffer, writeVideoPosition, partLen);
                            Marshal.Copy(token.currentBuffer + partLen, circularVideoBuffer, 0, len - partLen);
                            writeVideoPosition = len - partLen;
                        }
                    } else {
                        int len = token.currentSize;
                        if (writeAudioPosition + len < audioBufferSize) {
                            Marshal.Copy(token.currentBuffer, circularAudioBuffer, writeAudioPosition, len);
                            writeAudioPosition += len;
                        } else {
                            int partLen = audioBufferSize - writeAudioPosition;
                            Marshal.Copy(token.currentBuffer, circularAudioBuffer, writeAudioPosition, partLen);
                            Marshal.Copy(token.currentBuffer + partLen, circularAudioBuffer, 0, len - partLen);
                            writeAudioPosition = len - partLen;
                        }

                    }
                }
                Next();
            }
        }

        public int availableAudio {
            get {
                if (writeAudioPosition < readAudioPosition)
                    return (audioBufferSize - readAudioPosition) + writeAudioPosition; // Looped
                return writeAudioPosition - readAudioPosition;
            }
        }

        public int availableVideo {
            get {
                if (writeVideoPosition < readVideoPosition)
                    return (videoBufferSize - readVideoPosition) + writeVideoPosition; // Looped
                return writeVideoPosition - readVideoPosition;
            }
        }

        bool firstTime = true;
        float lastTime = 0;
        public  bool GetAudioBuffer(float[] dst, int len) {
            if ((firstTime && availableAudio >= len) || !firstTime) {
                firstTime = false;
                if (availableAudio >= len) {
                    if (writeAudioPosition < readAudioPosition) { // Se ha dado la vuelta.
                        int partLen = audioBufferSize - readAudioPosition;
                        if (partLen > len) {
                            System.Array.Copy(circularAudioBuffer, readAudioPosition, dst, 0, len);
                            readAudioPosition += len;
                        }
                        else {
                            System.Array.Copy(circularAudioBuffer, readAudioPosition, dst, 0, partLen);
                            System.Array.Copy(circularAudioBuffer, 0, dst, partLen, len - partLen);
                            readAudioPosition = len - partLen;
                        }
                    }
                    else {
                        System.Array.Copy(circularAudioBuffer, readAudioPosition, dst, 0, len);
                        readAudioPosition += len;
                    }
                    return true;
                }
            }
            return false;
        }

        public System.IntPtr GetVideoPointer(int len) {
            var ret = circularVideoBufferPtr + readVideoPosition;
            readVideoPosition += len;
            if (readVideoPosition >= videoBufferSize) readVideoPosition -= videoBufferSize;
            return ret;
        }
    }
}
