using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Music2Dance1980
{
    public class SkyboxRotator : MonoBehaviour
    {
        [SerializeField] Material Skybox;
        [SerializeField] float rate = 0.05f;

        private void Start()
        {
            RenderSettings.skybox = new Material(Skybox);
            RenderSettings.skybox.SetFloat("_EffectExposure", 0.4f);
        }

        // Update is called once per frame
        void Update()
        {
            RenderSettings.skybox.SetFloat("_EffectRotation", Time.timeSinceLevelLoad * rate);
        }

    }
}