using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRT.Core;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Ensure the correct GameObjects within P_Self_Player are enabled/disabled,
    /// depending on whether we are in XR (controllers, hands) or not (handsfree)
    /// </summary>
    public class FixInteractorGameObjects : MonoBehaviour
    {
        [Tooltip("GameObjects for interaction in XR")]
        public List<GameObject> interactionObjectsXR;
        [Tooltip("GameObjects for interaction not in XR")]
        public List<GameObject> interactionObjectsNonXR;

        [Tooltip("Enable debug messages")] 
        public bool debug = false;
        
        
        // Start is called before the first frame update
        void Start()
        {
            FixInteractors();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void FixInteractors()
        {
            bool xrIsActive = VRTConfig.ISXRActive();
            foreach (var go in interactionObjectsXR)
            {
                if (debug)
                {
                    var endis = xrIsActive ? "enable" : "disable";
                    Debug.Log($"FixInteractors: {endis} {go.name}");
                }
                go.SetActive(xrIsActive);
            }
            foreach (var go in interactionObjectsNonXR)
            {
                if (debug)
                {
                    var endis = !xrIsActive ? "enable" : "disable";
                    Debug.Log($"FixInteractors: {endis} {go.name}");
                }
                go.SetActive(!xrIsActive);
            }
        }
    }
}
