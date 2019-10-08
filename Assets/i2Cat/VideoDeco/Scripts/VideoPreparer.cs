using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace Workers {
    public class VideoPreparer : BaseWorker {
        float[] circularBuffer;
        int bufferSize;
        int writePosition;
        int readPosition;

        public VideoPreparer() : base(WorkerType.End) {
            bufferSize = 320 * 6 * 100;
            circularBuffer = new float[bufferSize];
            writePosition = 0;
            readPosition = 0;

            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("VideoPreparer Stopped");
        }

        protected override void Update() {
            base.Update();
            if (token != null) { 
                if (!token.isVideo) {
                    int len = token.currentSize;
                    if (writePosition + len < bufferSize) {
                        Marshal.Copy(token.currentBuffer, circularBuffer, writePosition, len);
                        //                    System.Array.Copy(token.currentFloatArray, 0, circularBuffer, writePosition, len);
                        writePosition += len;
                    } else {
                        int partLen = bufferSize - writePosition;
                        Marshal.Copy(token.currentBuffer, circularBuffer, writePosition, partLen);
                        Marshal.Copy(token.currentBuffer + partLen * 4, circularBuffer, writePosition, len - partLen);
                        //                    System.Array.Copy(token.currentFloatArray, 0, circularBuffer, writePosition, partLen);
                        //                    System.Array.Copy(token.currentFloatArray, partLen, circularBuffer, 0, len - partLen);
                        writePosition = len - partLen;
                    }
                //                Debug.Log($"ADD_BUFFER writePosition {writePosition} readPosition {readPosition}");                
                }
                Next();
            }
        }

        public int available {
            get {
                if (writePosition < readPosition)
                    return (bufferSize - readPosition) + writePosition; // Looped
                return writePosition - readPosition;
            }
        }

        bool firstTime = true;
        float lastTime = 0;
        public override bool GetBuffer(float[] dst, int len) {
            if ((firstTime && available >= len) || !firstTime) {
                firstTime = false;
                if (available >= len) {
                    if (writePosition < readPosition) { // Se ha dado la vuelta.
                        int partLen = bufferSize - readPosition;
                        if (partLen > len) {
                            System.Array.Copy(circularBuffer, readPosition, dst, 0, len);
                            readPosition += len;
                        }
                        else {
                            System.Array.Copy(circularBuffer, readPosition, dst, 0, partLen);
                            System.Array.Copy(circularBuffer, 0, dst, partLen, len - partLen);
                            readPosition = len - partLen;
                        }
                    }
                    else {
                        System.Array.Copy(circularBuffer, readPosition, dst, 0, len);
                        readPosition += len;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
