using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Component that handles grabbing and releasing of Grabbables (together with HandNetworkController)
    /// </summary>
    public class VRTHandController : MonoBehaviour
    {
        [Tooltip("Network controller (default: get from this GameObject)")]
        public HandNetworkControllerSelf handNetworkController;
        [Tooltip("Print logging messages on important changes")]
        [SerializeField] bool debug = false;

        private void Awake()
        {
            if (handNetworkController == null)
            {
                handNetworkController = GetComponent<HandNetworkControllerSelf>();
            }
            if (handNetworkController == null)
            {
                Debug.LogError("VRTHandController: needs HandNetworkControllerSelf");
            }
        }

        public void OnSelectEnter(SelectEnterEventArgs args)
        {
            GameObject grabbedObject = args.interactableObject.transform.gameObject;
            VRTGrabbableController grabbable = grabbedObject?.GetComponent<VRTGrabbableController>();
            if (grabbable == null)
            {
                Debug.LogError($"{name}: grabbed {grabbedObject} which has no Grabbable");
            }
            if (debug) Debug.Log($"{name}: grabbed {grabbable}");
            var heldGrabbable = handNetworkController?.HeldGrabbable;
            if (heldGrabbable == grabbedObject) {
                Debug.LogWarning($"{name}: already holding {grabbable}");
                return;
            }
            if (heldGrabbable != null)
            {
                Debug.LogWarning($"{name}: dropping already-held object {heldGrabbable}");
                GameObject heldGO = heldGrabbable.gameObject;
                heldGO.transform.SetParent(null, true);
            }

            handNetworkController.HeldGrabbable = grabbable;
            GameObject grabbableGO = grabbable.gameObject;
            grabbableGO.transform.SetParent(gameObject.transform, true);
        }

        public void OnSelectExit(SelectExitEventArgs args)
        {
            // xxxjack we could check that the object released is actually held...
            // xxxjack may also be needed if we can hold multiple objects....
            var heldGrabbable = handNetworkController?.HeldGrabbable;
            if (debug) Debug.Log($"{name}: released {heldGrabbable}");
            if (heldGrabbable != null)
            {
                heldGrabbable.gameObject.transform.SetParent(null, true);
            }
            handNetworkController.HeldGrabbable = null;
        }  // Start is called before the first frame update
       
    }
}

