using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vive.Plugin.SR.Experience
{
    public enum ControllerInputIndex
    {
        none = -1,
        trigger = -2,
        grip = -3,
        left = 0,
        right = 1,
        up = 2,
        down = 3,
        mid = 4,
        maxNum
    }

    public class ViveSR_Experience_Tutorial_TouchpadImage : MonoBehaviour
    {
        public bool isAnimationOn;

        bool[] isDisabled = new bool[] { true, true, true, true, true };

        Material mat;

        Color[] touchpadImageColors = new[] { Color.white, Color.white, Color.white, Color.white, Color.white };

        void setMat()
        {
            Image touchpadImage = GetComponent<Image>();
            mat = new Material(touchpadImage.material);
            touchpadImage.material = mat;
        }

        public bool IsDisabled(TouchpadDirection index)
        {
            return isDisabled[((int)index)-1];
        }
        public void SetEnable(TouchpadDirection index, bool enable)
        {
            ResetSprite();
            isDisabled[((int)index) - 1] = !enable;
        }
        public void SetColor(TouchpadDirection index, Color color)
        {
            touchpadImageColors[((int)index) - 1] = color;
            if (mat == null) setMat();
            mat.SetColorArray("_colorArray", touchpadImageColors);
        }
         
        public Color GetColor(TouchpadDirection index)
        {
            return touchpadImageColors[((int)index)-1];     
        }

        public void StartAnimate(TouchpadDirection inputIndex)
        {
            mat.SetInt("_TouchpadDirection", (int)inputIndex);
            mat.SetFloat("_Animation", 0);
            if(!isAnimationOn) StartCoroutine(Animate(inputIndex));
        }

        public void StopAnimate()
        {
            isAnimationOn = false;
        }

        public void ResetSprite()
        {
            if (mat == null) setMat();
        }

        public IEnumerator Animate(TouchpadDirection index)
        {
            isAnimationOn = true;
            mat.SetFloat("_Animation", 0);
            while (isAnimationOn)
            {
                float stripValue = mat.GetFloat("_Animation");
                mat.SetFloat("_Animation", stripValue >= 1 ? 0 : (stripValue + 5f * Time.deltaTime));

                yield return new WaitForSeconds(0.1f);
            }
            mat.SetFloat("_Animation", 0);
            ResetSprite();
        }
    }
}
