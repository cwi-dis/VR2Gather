using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Music2Dance1980
{
    public class DiscoVolumetricLight : MonoBehaviour
    { 
        [SerializeField] protected Renderer renderer_inner, renderer_outter;
        [SerializeField] protected Transform transForAnimation;
        [SerializeField] protected Projector projector;

        private void Start()
        {
            DiscoVolumetricLightManager discoVolumetricLightManager = FindObjectOfType<DiscoVolumetricLightManager>();
            projector.material = new Material(projector.material);
            discoVolumetricLightManager.discoVolumetricLights.Add(this);

            renderer_inner.material.mainTexture = renderer_outter.material.mainTexture = discoVolumetricLightManager.noiseTex;
            Color _color = discoVolumetricLightManager.GetColorRandom();
            renderer_inner.material.color = renderer_outter.material.color = _color;
            projector.material.color = _color * 0.7f;
            projector.material.mainTexture = discoVolumetricLightManager.noiseTex;
        }                                          
    }
}