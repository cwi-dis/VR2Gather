using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Vive.Plugin.SR.Experience
{
    public enum hintType
    {
        onController = 0,
        onHeadSet = 1
    }

    public class ViveSR_Experience_HintMessage : MonoBehaviour
    {         
        private static ViveSR_Experience_HintMessage _instance;
        public static ViveSR_Experience_HintMessage instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ViveSR_Experience_HintMessage>();
                }
                return _instance;
            }
        }
        [SerializeField] List<Text> hintTxts;

        bool isFading;

        public void Init()
        {
            hintTxts[(int)hintType.onController] = ViveSR_Experience.instance.AttachPoint.transform.Find("hint_canvas_controller/hint_canvas_controller_txt").GetComponent<Text>();
        }

        public void SetHintMessage(hintType hintType, string txt, bool autoFade, float waitTime = 3f)
        {
            hintTxts[(int)hintType].color = new Color(hintTxts[(int)hintType].color.r, hintTxts[(int)hintType].color.g, hintTxts[(int)hintType].color.b, 1);
            hintTxts[(int)hintType].text = txt;
            if(hintType == hintType.onHeadSet) isFading = false;
            if (autoFade) HintTextFadeOff(hintType, waitTime);
        }

        public void HintTextFadeOff(hintType hintType, float waitTime = 3f)
        {
            hintTxts[(int)hintType].color = new Color(hintTxts[(int)hintType].color.r, hintTxts[(int)hintType].color.g, hintTxts[(int)hintType].color.b, 1);
            StartCoroutine(FadeOff(hintType, waitTime));
        }

        IEnumerator FadeOff(hintType hintType, float waitTime = 3f)
        {
            isFading = true;
            if (isFading)
            {
                yield return new WaitForSeconds(waitTime);

                while (hintTxts[(int)hintType].color.a >= 0)
                {
                    if (!isFading) yield break;

                    hintTxts[(int)hintType].color -= new Color(0f, 0f, 0f, 2f * Time.deltaTime);
                    yield return new WaitForEndOfFrame();
                }
            }

            hintTxts[(int)hintType].text = "";
            hintTxts[(int)hintType].color = new Color(hintTxts[(int)hintType].color.r, hintTxts[(int)hintType].color.g, hintTxts[(int)hintType].color.b, 1);

            isFading = false;
        }        
    }
}