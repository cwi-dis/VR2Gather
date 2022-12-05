using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{      
    public class ViveSR_Experience_PortalAnimation : MonoBehaviour
    {
        [SerializeField] GameObject _PortalLogo;
        public GameObject PortalLogo { get { return _PortalLogo; } }
        List<ParticleSystem> ParticleSystems;
        GameObject scaleCenter;       

        void Awake()
        {        
            ParticleSystems = new List<ParticleSystem>(GetComponentsInChildren<ParticleSystem>());
            scaleCenter = transform.GetChild(0).transform.gameObject;
            scaleCenter.transform.localScale = Vector3.zero;
        }

        private void OnEnable()
        {
            StartCoroutine(Enlarge(true));
        }

        IEnumerator Enlarge(bool isOn)
        {                                                                          
            ViveSR_Experience.instance.SoundManager.PlayAtAttachPoint(AudioClipIndex.Portal);

            while (isOn ? scaleCenter.transform.localScale.x <= 1.2 : scaleCenter.transform.localScale.x >= 0)
            {
                 scaleCenter.transform.localScale += Vector3.one * Time.deltaTime * (isOn ? 3f : -3f);

                yield return new WaitForEndOfFrame();
            }
            scaleCenter.transform.localScale = isOn ? Vector3.one * 1.2f : Vector3.zero;
        }

        public void SetParticleSystems(bool isOn)
        {
            foreach (ParticleSystem ps in ParticleSystems)
            {
                ParticleSystem.EmissionModule emission = ps.emission;
                emission.enabled = isOn;
            }
        }
              
        public void SetPortalScale(bool isOn)
        {
           StartCoroutine(Enlarge(isOn));
        }
    }
}