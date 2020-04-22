using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class AudioPreparer : BaseWorker {
        float[] circularBuffer;
        int bufferSize;
        int writePosition;
        int readPosition;
        int preferredBufferFill;

        QueueThreadSafe inQueue;

        public AudioPreparer(QueueThreadSafe _inQueue, int _preferredBufferFill=0) : base(WorkerType.End) {
            inQueue = _inQueue;
            if (inQueue == null) Debug.LogError($"AudioPreparer: ERROR inQueue=NULL");
            bufferSize = 320 * 6 * 100;
            if (_preferredBufferFill == 0) _preferredBufferFill = bufferSize + 1;
            preferredBufferFill = _preferredBufferFill;
            circularBuffer = new float[bufferSize];
            writePosition = 0;
            readPosition = 0;
            Debug.Log("AudioPreparer: Started.");
            Start();
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("AudioPreparer: Stopped");
        }

        protected override void Update() {
            base.Update();
            if (inQueue.Count >0) {
                // xxxjack attempting to drop audio if there is too much in the buffer already                
                int bytesInAudioBuffer = (writePosition - readPosition) % bufferSize;
                if (bytesInAudioBuffer > preferredBufferFill) {
                    Debug.Log($"AudioPreparer: audioBuffer has {bytesInAudioBuffer} already, dropping audio");
                    inQueue.Dequeue().free();
                    return;
                }

                FloatMemoryChunk mc = (FloatMemoryChunk)inQueue.Dequeue();

                int len = mc.elements;
                //Debug.Log($"BEFORE len {len} writePosition {writePosition} readPosition {readPosition}");
                if (writePosition + len < bufferSize) {
                    System.Array.Copy(mc.buffer, 0, circularBuffer, writePosition, len);
                    writePosition += len;
                } else {
                    int partLen = bufferSize - writePosition;
                    System.Array.Copy(mc.buffer, 0, circularBuffer, writePosition, partLen);
                    System.Array.Copy(mc.buffer, partLen, circularBuffer, 0, len - partLen);
                    writePosition = len - partLen;
                }
                mc.free();
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
        public bool GetAudioBuffer(float[] dst, int len) {
            if ((firstTime && available >= len) || !firstTime) {
                firstTime = false;
                if (available >= len) {
                    if (writePosition < readPosition){ // Se ha dado la vuelta.
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
