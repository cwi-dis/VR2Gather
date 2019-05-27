using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedX
{
    static NSpeex.SpeexDecoder m_wide_dec = new NSpeex.SpeexDecoder(NSpeex.BandMode.Wide);

    public static float[] DecodeFromSpeex(byte[] encoded, int dataLength, int frequency) {

        short[] decoded = new short[encoded.Length];
        m_wide_dec.Decode(encoded, 0, dataLength, decoded, 0, false);
        float[] result = new float[encoded.Length];
        int t = 0;
        while (t < decoded.Length)
        {
            short sample = decoded[t];
            float floatSample = sample / (float)short.MaxValue;
            floatSample *= 2f;
            floatSample -= 1f;

            result[t] = floatSample;
            t++;
        }

        return result;
    }


    public static byte[] EncodeToSpeex(float[] samplesFloat, out int dataLength) {
        short[] samplesShort = new short[samplesFloat.Length];
        int i = 0;
        while (i < samplesFloat.Length)
        {
            float sample = samplesFloat[i];
            sample += 1f; // now it's in the range 0 .. 2
            sample *= 0.5f; // now it's in the range 0 .. 1
            short sampleShort = (short)Mathf.FloorToInt(sample * short.MaxValue);
            samplesShort[i] = sampleShort;
            ++i;

        }

        /*
        short[] inputPartChunk = new short[sizeChunkNorris]; // chunk of multiple of 640
        int y = 0;
        while (y < sizeChunkNorris) {
            inputPartChunk[y] = samplesShort[y];
            y++;
        }
        */

        byte[] encoded = new byte[4096];
        NSpeex.SpeexEncoder m_wide_enc = new NSpeex.SpeexEncoder(NSpeex.BandMode.Wide);
        dataLength = m_wide_enc.Encode(samplesShort, 0, samplesShort.Length, encoded, 0, encoded.Length);
        // where 'input' is an array of shorts, each short is one 16-bit sample

        Debug.Log(">>> dataLength " + dataLength);
        return encoded;
    }




}
