using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_ParticleFade : MonoBehaviour
    {
        [SerializeField] bool _fadeWhenDetached = true;        
        ParticleSystem[] particles;

        public bool FadeWhenDetached
        {
            get { return _fadeWhenDetached; }
            private set { _fadeWhenDetached = value; }
        }

        private void Awake()
        {
            particles = GetComponentsInChildren<ParticleSystem>(true);
        }

        public void StopParticleLooping()
        {
            //if (!_fadeWhenDetached) return;

            for (int i = 0; i < particles.Length; ++i)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.loop = false;             
            }
        }

        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
