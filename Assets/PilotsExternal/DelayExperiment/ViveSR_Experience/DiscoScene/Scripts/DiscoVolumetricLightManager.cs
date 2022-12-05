using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Music2Dance1980
{
    public class DiscoVolumetricLightManager : MonoBehaviour
    {
        [SerializeField] int pixWidth = 32, pixHeight = 32;
        float xOffset = 0, yOffset = 0;
        public Texture2D noiseTex;
        Color[] pix;

        [SerializeField] float waveSpeed = 1f, waveHeight = 2f;
        [SerializeField] float scale_x = 1, scale_y = 1;

        public List<DiscoVolumetricLight> discoVolumetricLights;

        [SerializeField] List<Color> Colors;

        public Color GetColorRandom()
        {
            return Colors[UnityEngine.Random.Range(0, Colors.Count)];
        }

        public Color GetColor(int index)
        {
            return Colors[index];
        }

        void Awake()
        {
            noiseTex = new Texture2D(pixWidth, pixHeight);
            pix = new Color[noiseTex.width * noiseTex.height];
        }

        void CalcNoise()
        {
            float width_inv = 1.0f / noiseTex.width;
            float height_inv = 1.0f/ noiseTex.height;
            for (int y = 0;y < noiseTex.height; y++)
            {
                for (int x = 0; x < noiseTex.width; x++)
                {
                    float xCoord = xOffset + (float)x * width_inv * scale_x + Time.timeSinceLevelLoad * waveSpeed;
                    float yCoord = yOffset + (float)y * height_inv * scale_y + Time.timeSinceLevelLoad * waveHeight;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);
                    pix[y * noiseTex.width + x] = new Color(sample, sample, sample);
                }
            }

            noiseTex.SetPixels(pix);
            noiseTex.Apply();
        }

        void Update()
        {
            if (discoVolumetricLights.Count > 0) CalcNoise();
        }
    }
}