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

        public AudioPreparer() : base(WorkerType.End) {
            bufferSize = 320 * 100;
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

        protected override void Update()
        {
            base.Update();

            if (token != null) {
                int len = token.currentSize;
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
//                Debug.Log($"ADD_BUFFER writePosition {writePosition} readPosition {readPosition}");
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
            if ((firstTime && available >= len*10) || !firstTime)
            {
                firstTime = false;
                if (available >= len)
                {
                    if (writePosition < readPosition) // Se ha dado la vuelta.
                    {
                        int partLen = bufferSize - readPosition;
                        if (partLen > len)
                        {
                            System.Array.Copy(circularBuffer, readPosition, dst, 0, len);
                            readPosition += len;
                        }
                        else
                        {
                            System.Array.Copy(circularBuffer, readPosition, dst, 0, partLen);
                            System.Array.Copy(circularBuffer, 0, dst, partLen, len - partLen);
                            readPosition = len - partLen;
                        }
                    }
                    else
                    {
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
