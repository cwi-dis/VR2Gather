using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public enum ImageEffectType
    {
        NONE = -1,
        NIGHT_VISION = 0,
        SHARPEN = 1,
        SKETCH = 2,
        THERMAL = 3,
        TOTAL_NUM = 4,
    }    

    [RequireComponent(typeof(Camera))]
    public class ViveSR_Experience_PostEffects : MonoBehaviour
    {
        private Dictionary<ImageEffectType, Shader> _shaderEffects = new Dictionary<ImageEffectType, Shader>();
        public Material effectMat;

        [Header("NightVisionEffect")]
        [Range(0, 5)]
        public float nightVisionBright = 1.3f;
        [Range(0, 3)]
        public float radius = 0.4f;

        [Header("SharpenEffect")]
        [Range(9, 15)]
        public float sharpBright = 9.0f;
        [Range(0.2f, 5)]
        public float intensity = 1.8f;

        [Header("SketchEffect")]
        [Range(1, 15)]
        public float contrast = 8.0f;
        [Range(0, 2)]
        public float Whiteness = 0.1f;

        [Header("ThermalEffect")]
        [Range(1, 10)]
        public float shade = 2.5f;

        void Awake()
        {
            _shaderEffects[ImageEffectType.NONE] = Shader.Find("ViveSR/Unlit, Textured, Stencil");
            _shaderEffects[ImageEffectType.NIGHT_VISION] = Shader.Find("ViveSR_Experience/nightShader");
            _shaderEffects[ImageEffectType.SHARPEN] = Shader.Find("ViveSR_Experience/sharpShader");
            _shaderEffects[ImageEffectType.SKETCH] = Shader.Find("ViveSR_Experience/sketchShader");
            _shaderEffects[ImageEffectType.THERMAL] = Shader.Find("ViveSR_Experience/thermalShader");
        }

        private void OnEnable()
        {
            //currentEffect = ImageEffectType.NONE;
            effectMat = new Material(_shaderEffects[ImageEffectType.NONE]);
        }
        public void SetEffectShader(ImageEffectType type)
        {                                      
            if (!effectMat)
            {
                effectMat = new Material( _shaderEffects[type] );
            }
            else
            {
                effectMat.shader = _shaderEffects[type];
                effectMat.name = _shaderEffects[type].name;
            }

            UpdateMaterialProperties();
        }

        void UpdateMaterialProperties()
        {
            if (!effectMat) return;

            // for thermal
            if (effectMat.HasProperty("_Shade"))        effectMat.SetFloat("_Shade", shade);
            // for sketch
            if (effectMat.HasProperty("_LS"))           effectMat.SetFloat("_LS", contrast);
            if (effectMat.HasProperty("_Whiteness"))    effectMat.SetFloat("_Whiteness", Whiteness);
            // for sharpen
            if (effectMat.HasProperty("_SharpBright"))  effectMat.SetFloat("_SharpBright", sharpBright);
            if (effectMat.HasProperty("_SharpIntense")) effectMat.SetFloat("_SharpIntense", intensity);
            // for night vision
            if (effectMat.HasProperty("_VisionBright")) effectMat.SetFloat("_VisionBright", nightVisionBright);
            if (effectMat.HasProperty("_Radius"))       effectMat.SetFloat("_Radius", radius);
        }

#if UNITY_EDITOR
        void Update()
        {
            UpdateMaterialProperties();
        }
#endif
        
        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
               Graphics.Blit(src, dst, effectMat);
        }
    }
}