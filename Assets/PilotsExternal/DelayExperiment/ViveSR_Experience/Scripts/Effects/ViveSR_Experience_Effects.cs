using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Effects : MonoBehaviour
    {
        [Header("EffectBalls")]
        public int CurrentEffectNumber = -1;

        [SerializeField] Renderer EffectballRenderer;
        [SerializeField] List<Texture> EffectImages;

        [Header("ImageEffect")]
        [SerializeField] public ViveSR_Experience_PostEffects postEffectScript = null;

        public void GenerateEffectBall()
        {
            SetEffectBallTransform();

            //Switch effect balls.            
            CurrentEffectNumber = (CurrentEffectNumber + 1) % (int)ImageEffectType.TOTAL_NUM;
            gameObject.SetActive(true);
            EffectballRenderer.material.mainTexture = EffectImages[CurrentEffectNumber];
        }

        public void SetEffectBallTransform()
        {
            switch (ViveSR_Experience.instance.CurrentDevice)
            {
                case DeviceType.VIVE_PRO:
                    transform.localPosition = new Vector3(-0.005f, -0.01f, 0.05f);
                    transform.localEulerAngles = new Vector3(0f, -90f, 50f);
                    break;
                case DeviceType.VIVE_COSMOS:
                    transform.localPosition = new Vector3(-0.005f, -0.04f, 0.02f);
                    transform.localEulerAngles = new Vector3(0f, -90f, 20f);
                    break;
                default:
                    goto case DeviceType.VIVE_PRO;
            }
        }

        public void HideEffectBall()
        {
            gameObject.SetActive(false);
        }

        public void ChangeShader(int index)
        {
            if (!postEffectScript) return;
            if (index == -1)
            {
                postEffectScript.gameObject.SetActive(false);
            }
            else
            {
                postEffectScript.gameObject.SetActive(true);
                postEffectScript.SetEffectShader((ImageEffectType)index);
            }
        }

        public void ToggleEffects(bool isOn)
        {
            postEffectScript.gameObject.SetActive(isOn);
        }

    }
}