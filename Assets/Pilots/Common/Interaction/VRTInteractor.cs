using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Behaviour for an object that can interact with other objects as if it is a finger.
    /// Needs a trigger collider. On collision, it will check if the gemaObject of the other collider
    /// has a IXRActivateInteractable, and if it has it will call OnActivate() on it.
    /// Example: the drum stick colliders.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VRTInteractor : MonoBehaviour
    {
        

        private void OnTriggerEnter(Collider other)
        {
            var otherGO = other.gameObject;
            IXRActivateInteractor source = null;
            IXRActivateInteractable target = otherGO.GetComponent<IXRActivateInteractable>(); // (IXRActivateInteractable)args.interactableObject;
            if (target == null)
            {
                return;
            }
            Debug.Log($"VRTInteractor: OnTriggerEnter from {source}, calling {target}.OnActivated() ");
            ActivateEventArgs activateArgs = new ActivateEventArgs
            {
                interactorObject = source,
                interactableObject = target
            };
            target.OnActivated(activateArgs);
        }

    }

}
