using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_NPCAnimationRef : MonoBehaviour
    {                     
        [SerializeField] ViveSR_Experience_NPCAnimationController _NPCAnimController;
        public ViveSR_Experience_NPCAnimationController NPCAnimController
        {
            get
            {
                return _NPCAnimController;
            }
        }
        public NavMeshAgent NavMeshAgent;

        public ViveSR_Experience_Chair OccupyingChair = null;

        List<SkinnedMeshRenderer> _Renderers = new List<SkinnedMeshRenderer>();
        public List<SkinnedMeshRenderer> Renderers
        {
            get
            {
                return _Renderers;
            }
        }

        private void Awake()
        { 
            foreach (SkinnedMeshRenderer rnd in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(rnd)_Renderers.Add(rnd);
            }
        }
    }
}