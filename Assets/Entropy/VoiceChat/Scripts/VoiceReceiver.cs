using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceReceiver {    
    int playerID;
    int frequency = 44100;
    float[] chunk = new float[4096]; // Max chunk.

    // Circular buffer with audio received.
    float[] buffer;
    int     bufferSize;
    int     writePosition;
    int     readPosition;


    public VoiceReceiver(int playerID) {
        this.playerID = playerID;
        bufferSize = frequency * 2; // 88200
        buffer = new float[bufferSize];
        writePosition = 0;
        readPosition = 0;
    }

    public bool GetBuffer(float[] dst, int len) {
        int available;
        if(writePosition< readPosition)
            available = (bufferSize - readPosition) + writePosition; // Looped
        else
            available = writePosition + readPosition; // Looped
        if (available > len)
        {
            if (writePosition < readPosition)
            {
                int firstPartLen = bufferSize - readPosition;
                if (firstPartLen > len) // first part is enougth?
                {
                    System.Array.Copy(buffer, readPosition, dst, 0, len);
                    readPosition += len;
                }
                else
                {
                    System.Array.Copy(buffer, readPosition, dst, 0, firstPartLen);
                    System.Array.Copy(buffer, 0, dst, firstPartLen, len- firstPartLen);
                    readPosition = len - firstPartLen;
                }
            }
            else
            {
                System.Array.Copy(buffer, readPosition, dst, 0, len);
                readPosition += len;
            }
            return true;
        }
        else
            return false;
    }

    public void ReceiveBuffer(float[] buffer) {
        int len = buffer.Length;
        if (writePosition + len >= bufferSize)
        {
            int firstPartLen = (writePosition + len) - bufferSize;
            System.Array.Copy(buffer, 0, this.buffer, writePosition, firstPartLen);
            System.Array.Copy(buffer, firstPartLen, this.buffer, 0, len - firstPartLen);
            writePosition = len - firstPartLen;
        }
        else
        {
            System.Array.Copy(buffer, 0, this.buffer, writePosition, len);
            writePosition += len;
        }


    }

}
