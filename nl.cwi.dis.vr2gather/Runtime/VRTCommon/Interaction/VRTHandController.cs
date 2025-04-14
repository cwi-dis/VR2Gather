using System.Collections;
using System.Collections.Generic;
using VRT.Core;
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
            IVRTGrabbable grabbable = grabbedObject?.GetComponent<IVRTGrabbable>();
            if (grabbable == null)
            {
                Debug.LogError($"VRTHandcontroller({name}): grabbed {grabbedObject} which has no Grabbable");
                return;
            }
            if (debug) Debug.Log($"VRTHandcontroller({name}): grabbed {grabbable}");
            IVRTGrabbable heldGrabbable = handNetworkController?.HeldGrabbable;
            if (heldGrabbable == grabbable) {
                Debug.LogWarning($"VRTHandcontroller({name}): already holding {grabbable}");
                return;
            }
            if (heldGrabbable != null)
            {
                Debug.LogWarning($"VRTHandcontroller({name}): dropping already-held object {heldGrabbable}");
                GameObject heldGO = heldGrabbable.gameObject;
                heldGO.transform.SetParent(null, true);
                heldGrabbable.OnSelectExit();
            }

            handNetworkController.HeldGrabbable = grabbable;
            GameObject grabbableGO = grabbable.gameObject;
            grabbableGO.transform.SetParent(gameObject.transform, true);
            if (debug)
            {
                Debug.Log($"VRTHandcontroller({name}): calling grabbable.OnSelectEnter()");
            }
            grabbable.OnSelectEnter(args);
        }

        public void OnSelectExit()
        {
            // xxxjack we could check that the object released is actually held...
            // xxxjack may also be needed if we can hold multiple objects....
            IVRTGrabbable heldGrabbable = handNetworkController?.HeldGrabbable;
            handNetworkController.HeldGrabbable = null;
            if (debug) Debug.Log($"VRTHandcontroller({name}): released {heldGrabbable}");
            if (heldGrabbable != null)
            {
                heldGrabbable.gameObject.transform.SetParent(null, true);
                if (debug)
                {
                    Debug.Log($"VRTHandcontroller({name}): calling grabbable.OnSelectExit()");
                }
                heldGrabbable.OnSelectExit();
            }            
        }  // Start is called before the first frame update
       
    }
}

