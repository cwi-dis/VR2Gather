using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Ensure all TeleportAreas in the scene have the correct teleport provider and interaction
    /// manager.
    /// 
    /// Seems to be needed because we create the whole interaction stuff after the teleport areas
    /// Awake() has been called.
    /// </summary>
    public class FixTeleportAreas : MonoBehaviour
    {
        public XRInteractionManager interactionManager;
        public TeleportationProvider teleportationProvider;

        // Start is called before the first frame update
        void Start()
        {
            if (interactionManager == null) interactionManager = GetComponent<XRInteractionManager>();
            if (teleportationProvider == null) teleportationProvider = GetComponent<TeleportationProvider>();
            if (interactionManager != null && teleportationProvider != null)
            {
                Debug.Log($"FixTeleportationAreas: installing");
                var allAreas = FindObjectsOfType<BaseTeleportationInteractable>();
                foreach(var ta in allAreas)
                {
                    ta.interactionManager = interactionManager;
                    ta.teleportationProvider = teleportationProvider;
                    Debug.Log($"FixTeleportationAreas: installed into {ta.name}");
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
