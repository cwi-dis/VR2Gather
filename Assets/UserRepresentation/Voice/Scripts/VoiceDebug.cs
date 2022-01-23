using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VRT.Core;

#if VRT_AUDIO_DEBUG
namespace VRT.UserRepresentation.Voice
{
 
    public class ToneGenerator
    {
        float position = 0;
        const float factor = 0.5f;
        const float sampleFrequency = 48000f;
        const float toneFrequency = 440f;
        const bool printAll = true;

        public ToneGenerator() { }

        public void addTone(float[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] += Mathf.Sin(position) * factor;
                position += 2 * Mathf.PI / (sampleFrequency / toneFrequency);
            }
        }

        public static void checkToneBuffer(string name, float[] buffer)
        {
            float maxValue = Math.Abs(buffer[0]);
            int maxIndex = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (Math.Abs(buffer[i]) > maxValue)
                {
                    maxValue = Math.Abs(buffer[i]);
                    maxIndex = i;
                }
            }
            float maxDeltaValue = Math.Abs(buffer[1] - buffer[0]);
            int maxDeltaIndex = 1;
            for (int i = 1; i < buffer.Length; i++)
            {
                if (Math.Abs(buffer[i] - buffer[i-1]) > maxDeltaValue)
                {
                    maxDeltaValue = Math.Abs(buffer[i] - buffer[i-1]);
                    maxDeltaIndex = i;
                }
            }
            if (printAll)
            {
                Debug.Log($"xxxjack checkToneBuffer: {name}[{maxIndex} of {buffer.Length}] = {maxValue}");
                Debug.Log($"xxxjack checkToneBuffer: {name}[{maxDeltaIndex} of {buffer.Length}] delta {maxDeltaValue}");
            }
            if (maxValue > factor)
            {
                Debug.LogWarning($"xxxjack checkToneBuffer: too large: {name}[{maxIndex} of {buffer.Length}] = {maxValue}");
            }
            if (maxDeltaValue > factor / 8)
            {
                Debug.LogWarning($"xxxjack checkToneBuffer: too large: {name}[{maxDeltaIndex} of {buffer.Length}] delta {maxDeltaValue}");
            }
        }

        public static void checkToneBuffer(string name, IntPtr bufferPtr, int length)
        {
            int nFloats = length / sizeof(float);
            float[] buffer = new float[nFloats];
            System.Runtime.InteropServices.Marshal.Copy(bufferPtr, buffer, 0, nFloats);
            checkToneBuffer(name, buffer);
        }
    }
}
#endif