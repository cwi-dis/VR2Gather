using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Ensure all Interactables in the scene have the correct interaction
    /// manager. Teleportation interactables also get the correct teleport provider.
    /// 
    /// Seems to be needed because we create the whole interaction stuff after the teleport areas
    /// Awake() has been called.
    /// </summary>
    public class FixInteractables : MonoBehaviour
    {
        public XRInteractionManager interactionManager;
        public TeleportationProvider teleportationProvider;

        // Start is called before the first frame update
        void Start()
        {
            if (interactionManager == null) interactionManager = GetComponent<XRInteractionManager>();
            if (teleportationProvider == null) teleportationProvider = GetComponent<TeleportationProvider>();
            if (teleportationProvider != null)
            {
                Debug.Log($"FixInteractables: installing teleportation");
                var allTeleportations = FindObjectsOfType<BaseTeleportationInteractable>();
                foreach (var ta in allTeleportations)
                {
                    ta.teleportationProvider = teleportationProvider;
                    Debug.Log($"FixInteractables: installed teleportationProvider into {ta.name}");
                }
            }
            if (interactionManager != null)
            {
                Debug.Log($"FixInteractables: installing interactables");
                var allInteractables = FindObjectsOfType<XRBaseInteractable>();
                foreach(var go in allInteractables)
                {
                    go.interactionManager = interactionManager;
                    Debug.Log($"FixInteractables: installed interactionManager into {go.name}");
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
