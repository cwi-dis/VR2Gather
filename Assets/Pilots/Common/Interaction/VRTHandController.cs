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
            handNetworkController.HeldGrabbable = grabbable;
        }

        public void OnSelectExit(SelectExitEventArgs args)
        {
            // xxxjack we could check that the object released is actually held...
            // xxxjack may also be needed if we can hold multiple objects....
            if (debug) Debug.Log($"{name}: released {handNetworkController.HeldGrabbable}");
            handNetworkController.HeldGrabbable = null;
        }  // Start is called before the first frame update
       
    }
}

