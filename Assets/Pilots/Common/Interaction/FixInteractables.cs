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
        public bool debug = false;

        // Start is called before the first frame update
        void Start()
        {
            if (interactionManager == null)
            {
                interactionManager = FindObjectOfType<XRInteractionManager>();
                if (interactionManager == null)
                {
                    Debug.Log("FixInteractables: Creating interaction manager");
                    var interactionManagerGO = new GameObject("XR Interaction Manager", typeof(XRInteractionManager));
                    interactionManager = interactionManagerGO.GetComponent<XRInteractionManager>();
                }
            }
            if (teleportationProvider == null) teleportationProvider = GetComponent<TeleportationProvider>();
            if (teleportationProvider != null)
            {
                if (debug) Debug.Log($"FixInteractables: installing teleportation");
                var allTeleportations = FindObjectsOfType<BaseTeleportationInteractable>(true);
                foreach (var ta in allTeleportations)
                {
                    ta.teleportationProvider = teleportationProvider;
                    if (debug) Debug.Log($"FixInteractables: installed teleportationProvider into {ta.name}");
                }
            }
            if (interactionManager != null)
            {
                if (debug) Debug.Log($"FixInteractables: installing interactables");
                var allInteractables = FindObjectsOfType<XRBaseInteractable>(true);
                foreach(var go in allInteractables)
                {
                    go.interactionManager = interactionManager;
                    if (debug) Debug.Log($"FixInteractables: installed interactionManager into {go.name}");
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
