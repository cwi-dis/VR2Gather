using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_NPCEyeBlink : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer FaceMesh;

        List<float> BlinkFrequencies = new List<float>();
        int OldFrequencyIndex = -1; //Don't use the same frenquency in a row
        int blinkFrequencyIndex = -1;
        private void Awake()
        {
            BlinkFrequencies.Add(0.05f);
            BlinkFrequencies.Add(5f);
            BlinkFrequencies.Add(3f);
        }

        private void OnEnable()
        {   
            StartCoroutine(EyeBlink());
        }

        IEnumerator EyeBlink()
        {
            float accelerate = 1;
            bool IsClosingEyes = FaceMesh.GetBlendShapeWeight(15) == 0; //100 = eyes closed;
            float targetWeight = IsClosingEyes ? 100 : 0;
            float eyeWeight = FaceMesh.GetBlendShapeWeight(15);

            while (enabled && (IsClosingEyes ? (eyeWeight < targetWeight - 5) : (FaceMesh.GetBlendShapeWeight(15) > targetWeight + 5)))
            {                                                          
                float closeSpd = 500f, openSpd = -700f;
                eyeWeight = FaceMesh.GetBlendShapeWeight(15) + (IsClosingEyes ? closeSpd : openSpd) * accelerate * Time.deltaTime;
                FaceMesh.SetBlendShapeWeight(15, eyeWeight);
                FaceMesh.SetBlendShapeWeight(21, eyeWeight);
                accelerate += 1;

                yield return new WaitForEndOfFrame();
            }

            FaceMesh.SetBlendShapeWeight(15, targetWeight);
            FaceMesh.SetBlendShapeWeight(21, targetWeight);

            if (!IsClosingEyes)
            {
                while (blinkFrequencyIndex == OldFrequencyIndex)
                {
                    blinkFrequencyIndex = Random.Range(0, 3);
                }
                OldFrequencyIndex = blinkFrequencyIndex;
            }

            yield return new WaitForSeconds(IsClosingEyes ? 0.1f : BlinkFrequencies[blinkFrequencyIndex]);
            StartCoroutine(EyeBlink());
        }
    }
}