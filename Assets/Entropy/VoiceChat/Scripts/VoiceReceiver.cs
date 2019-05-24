using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceReceiver {    
    float[] circularBuffer;
    int     bufferSize;
    int     writePosition;
    int     readPosition;


    public VoiceReceiver() {
        bufferSize = 65536;
        circularBuffer = new float[bufferSize];
        writePosition = 0;
        readPosition = 0;
    }

    bool firstTime = true;


    public int available { get {
            if (writePosition < readPosition)
                return (bufferSize - readPosition) + writePosition; // Looped
            return writePosition - readPosition;
        }
    }

    public bool GetBuffer(float[] dst, int len) {
        if ((firstTime && available > 512) || !firstTime)
        {
            firstTime = false;
            if (available > len )
            {
                if (writePosition < readPosition)
                {
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


    float[] floatBuffer;
    public void ReceiveBuffer(byte[] data) {
        if (floatBuffer == null) floatBuffer = new float[data.Length / 4];
        System.Buffer.BlockCopy(data, 1+2+8, floatBuffer, 0, data.Length-(1 + 2 + 8));

        int len = floatBuffer.Length;
        if (writePosition + len < bufferSize) {
            System.Array.Copy(floatBuffer, 0, circularBuffer, writePosition, len);
            writePosition += len;
        }
        else
        {
            int partLen = bufferSize - writePosition;
            System.Array.Copy(floatBuffer, 0, circularBuffer, writePosition, partLen);
            System.Array.Copy(floatBuffer, partLen, circularBuffer, 0, len - partLen);
            writePosition = len - partLen;
        }

    }

}
