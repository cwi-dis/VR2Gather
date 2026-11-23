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
        [Tooltip("Components that are only useful in XR, disabled without XR (for example XRI's XRInputModalityManager, which warns about a missing hand tracking subsystem otherwise)")]
        public List<Behaviour> componentsXR;

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
            // Only disabled, never enabled: in XR they keep whatever state the prefab gives them.
            // Runs in Start(), before the components' first Update() (#246).
            if (!xrIsActive && componentsXR != null)
            {
                foreach (var c in componentsXR)
                {
                    if (c == null) continue;
                    if (debug) Debug.Log($"FixInteractors: disable {c.GetType().Name} on {c.gameObject.name}");
                    c.enabled = false;
                }
            }
        }
    }
}
