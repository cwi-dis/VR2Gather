using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedX
{
    static NSpeex.SpeexDecoder m_wide_dec = new NSpeex.SpeexDecoder(NSpeex.BandMode.Wide);
    static NSpeex.SpeexEncoder m_wide_enc = new NSpeex.SpeexEncoder(NSpeex.BandMode.Wide);
    static short[] decoded = new short[640];
    static float[] result = new float[640];

    public static void DecodeFromSpeex(byte[] encoded, int offset, int dataLength, float[] result, int resultOffset) {

        m_wide_dec.Decode(encoded, offset, dataLength, decoded, 0, false);
        int t = 0;
        while (t < decoded.Length) {
            short sample = decoded[t];
            float floatSample = sample / (float)short.MaxValue;
            floatSample *= 2f;
            floatSample -= 1f;

            result[t+ resultOffset] = floatSample;
            t++;
        }

    }

    static short[] samplesShort = new short[640];
    static byte[] encoded = new byte[62];
    public static byte[] EncodeToSpeex(float[] data, out int dataLength) {
        int i = 0;
        while (i < data.Length) {
            float sample = data[i];
            sample += 1f; // now it's in the range 0 .. 2
            sample *= 0.5f; // now it's in the range 0 .. 1
            short sampleShort = (short)Mathf.FloorToInt(sample * short.MaxValue);
            samplesShort[i] = sampleShort;
            ++i;
        }

        dataLength = m_wide_enc.Encode(samplesShort, 0, 1, encoded, 0, encoded.Length);
        // where 'input' is an array of shorts, each short is one 16-bit sample
        return encoded;
    }




}
