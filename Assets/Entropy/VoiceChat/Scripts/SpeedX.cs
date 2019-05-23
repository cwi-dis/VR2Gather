using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedX
{

    static void ToShortArray(float[] input, short[] output)
    {
        if (output.Length < input.Length)
        {
            throw new System.ArgumentException("in: " + input.Length + ", out: " + output.Length);
        }

        for (int i = 0; i < input.Length; ++i)
        {
            output[i] = (short)Mathf.Clamp((int)(input[i] * 32767.0f), short.MinValue, short.MaxValue);
        }
    }

    static void ToFloatArray(short[] input, float[] output, int length)
    {
        if (output.Length < length || input.Length < length)
        {
            throw new System.ArgumentException();
        }

        for (int i = 0; i < length; ++i)
        {
            output[i] = input[i] / (float)short.MaxValue;
        }
    }

    static NSpeex.SpeexEncoder speexEnc = new NSpeex.SpeexEncoder(NSpeex.BandMode.Narrow);
    static float[]  decoded = new float[4096];
    static short[]  shortBuffer = new short[4096];
    static byte[]   encoded = new byte[4096+50];

    public static byte[] Compress(float[] input, out int length) {
        ToShortArray(input, shortBuffer);
        length = speexEnc.Encode(shortBuffer, 0, input.Length, encoded, 0, 4096);
        encoded[0] = (byte)(length & 0xFF);
        encoded[1] = (byte)((length>>8) & 0xFF);
        return encoded;
    }

    static NSpeex.SpeexDecoder speexDec = new NSpeex.SpeexDecoder(NSpeex.BandMode.Narrow);
    public static float[] Decompress(byte[] data) {
        int dataLength =(int) (data[1]<<8) | (int)data[0];
        Debug.Log("dataLength " + dataLength);
        speexDec.Decode(data, 2, dataLength, shortBuffer, 0, false);
        ToFloatArray(shortBuffer, decoded, shortBuffer.Length);
        return decoded;
    }

}
