using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Workers
{
    public class AudioPreparer : BaseWorker
    {
        float[] circularBuffer;
        int bufferSize;
        int writePosition;
        int readPosition;
        int preferredBufferFill;

        public AudioPreparer(int _preferredBufferFill=0) : base(WorkerType.End) {
            preferredBufferFill = _preferredBufferFill;
            bufferSize = 320*6 * 100;
            if (_preferredBufferFill == 0) _preferredBufferFill = bufferSize + 1;
            preferredBufferFill = _preferredBufferFill;
            circularBuffer = new float[bufferSize];
            writePosition = 0;
            readPosition = 0;

            Start();
        }

        public override void OnStop() {
            base.OnStop();
            //            if (byteArray.Length != 0) byteArray.Dispose();
            Debug.Log("AudioPreparer Sopped");
        }

        protected override void Update() {
            base.Update();

            if (token != null) {
                // xxxjack attempting to drop audio if there is too much in the buffer already
                int bytesInAudioBuffer;
                if (readPosition >= writePosition) bytesInAudioBuffer = readPosition - writePosition;
                else  bytesInAudioBuffer = (bufferSize - writePosition) + readPosition;

                if (bytesInAudioBuffer > preferredBufferFill)
                {
                    Debug.Log($"AudioPreparer: audioBuffer has {bytesInAudioBuffer} already, dropping audio");
                    Next();
                    return;
                }
                
                int len = token.currentSize;
                // Debug.Log($"BEFORE len {len} writePosition {writePosition} readPosition {readPosition}");
                if (writePosition + len < bufferSize) {
                    System.Array.Copy(token.currentFloatArray, 0, circularBuffer, writePosition, len);
                    writePosition += len;
                }
                else {
                    int partLen = bufferSize - writePosition;
                    System.Array.Copy(token.currentFloatArray, 0, circularBuffer, writePosition, partLen);
                    System.Array.Copy(token.currentFloatArray, partLen, circularBuffer, 0, len - partLen);
                    writePosition = len - partLen;
                }
                // Debug.Log($"ADD_BUFFER writePosition {writePosition} readPosition {readPosition}");
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
