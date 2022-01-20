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
        const float factor = 0.1f;
        const float sampleFrequency = 48000f;
        const float toneFrequency = 440f;

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
            Debug.Log($"xxxjack checkToneBuffer: {name}[{maxIndex}] = {maxValue}");
            if (maxValue > factor)
            {
                Debug.LogWarning($"xxxjack checkToneBuffer: too large: {name}[{maxIndex}] = {maxValue}");
            }
        }
    }
}
#endif