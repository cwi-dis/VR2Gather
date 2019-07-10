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

        public AudioPreparer() : base(WorkerType.End)
        {
            bufferSize = 65536;
            circularBuffer = new float[bufferSize];
            writePosition = 0;
            readPosition = 0;

            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
//            if (byteArray.Length != 0) byteArray.Dispose();
        }

        protected override void Update()
        {
            base.Update();

            if (token != null) {
                Debug.Log("Datos!");
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
        public bool GetBuffer(float[] dst, int len)
        {
            if ((firstTime && available > 320) || !firstTime)
            {
                firstTime = false;
                if (available > len)
                {
                    Debug.Log("Write");
                    if (writePosition < readPosition)
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
