using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRT.Pilots.Common
{
    using ControllerType = VRTInputController.ControllerType;
    /// <summary>
    /// MonoBehaviour that selects the correct visual representation for a VR controller and
    /// enables either ray-based or direct interaction (based on PilotController).
    /// </summary>
    public class HandVisualController : MonoBehaviour
    {
        [Tooltip("VRTInputController (default: find in parents)")]
        public VRTInputController inputController = null;
    
        [Tooltip("Objects that handle XR ray-based input (rendered as one of the controllers)")]
        [SerializeField] private GameObject rayBasedInputObject;
        [Tooltip("Objects that handle input based on XR direct interaction (rendered as virtual hand)")]
        [SerializeField] private GameObject directInteractionInputObject;
        [Tooltip("Rendition of an Oculus controller")]
        public GameObject OculusController;
        [Tooltip("Rendition of a Vive controller")]
        public GameObject ViveController;
        [Tooltip("Rendition of another VR controller")]
        public GameObject OtherController;
        [Tooltip("Current controller type")]
        [DisableEditing] [SerializeField] ControllerType controllerType = ControllerType.None;

        [Tooltip("Enable debug logging")]
        [SerializeField] bool debug = false;

        public void Awake()
        {
            if (inputController == null)
            {
                inputController = GetComponentInParent<VRTInputController>();
            }
            inputController.controllerChanged += OnControllerChanged;
            // The inputController will call our OnControllerChanged callback during Start(), which will
            // then enable the right interaction and visual representation. So we disable everything for now.
            DisableInteraction();
        }
  
        private void OnDestroy()
        {
            inputController.controllerChanged -= OnControllerChanged;
   
        }

        private void DisableInteraction()
        {
            if (debug)
            {
                Debug.Log($"HandVisualController: disable interaction");
            }
            directInteractionInputObject.SetActive(false);
            rayBasedInputObject.SetActive(false);
        }
  
        void FixRepresentation(ControllerType _controllerType)
        {
            controllerType = _controllerType;
            if (debug)
            {
                Debug.Log($"HandVisualController: FixRepresentation type={controllerType}");
            }
            directInteractionInputObject.SetActive(controllerType == ControllerType.VirtualHand);
            rayBasedInputObject.SetActive(controllerType != ControllerType.VirtualHand); // xxxjack how about None?
            OculusController.SetActive(controllerType == ControllerType.Oculus);
            ViveController.SetActive(controllerType == ControllerType.Vive);
            OtherController.SetActive(controllerType == ControllerType.OtherController);
        }

        void OnControllerChanged()
        {
            FixRepresentation(inputController.currentVisibleController);
        }
    }
}
