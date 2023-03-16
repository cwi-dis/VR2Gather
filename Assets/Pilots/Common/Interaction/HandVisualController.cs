using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// MonoBehaviour that selects the correct visual representation for a VR controller and
    /// enables either ray-based or direct interaction (based on PilotController).
    /// </summary>
    public class HandVisualController : MonoBehaviour
    {
        /// <summary>
        /// Supported controller types. When adding types also add the name substring to
        /// FindAttachedController().
        /// </summary>
        public enum ControllerType
        {
            Other,
            Oculus
        };

        [Tooltip("Objects that handle XR ray-based input")]
        [SerializeField] private GameObject rayBasedInputObject;
        [Tooltip("Objects that handle input based on XR direct interaction")]
        [SerializeField] private GameObject directInteractionInputObject;
        [Tooltip("Rendition of an Oculus controller")]
        public GameObject OculusController;
        [Tooltip("Default rendition of a controller")]
        public GameObject OtherController;
        [Tooltip("Introspection/debug: is direct interaction currently enabled")]
        [DisableEditing] [SerializeField] private bool directInteractionIsEnabled;
        [Tooltip("Current controller type")]
        [DisableEditing] [SerializeField] ControllerType controllerType = ControllerType.Other;

        public void Awake()
        {
            DisableInteraction();
        }
  
        void Start()
        {
            ControllerType curController = FindAttachedController();
            FixRepresentation(curController, force:true);
            InputDevices.deviceConnected += OnDeviceChanged;
            InputDevices.deviceDisconnected += OnDeviceChanged;
            InputDevices.deviceConfigChanged += OnDeviceChanged;
            directInteractionIsEnabled = PilotController.Instance.directInteractionAllowed;
            FixDirectInteraction();
         }

        private void OnDestroy()
        {
            InputDevices.deviceConnected -= OnDeviceChanged;
            InputDevices.deviceDisconnected -= OnDeviceChanged;
            InputDevices.deviceConfigChanged -= OnDeviceChanged;

        }

        // Update is called once per frame
        void Update()
        {
            if (PilotController.Instance.directInteractionAllowed != directInteractionIsEnabled)
            {
                directInteractionIsEnabled = PilotController.Instance.directInteractionAllowed;
                FixDirectInteraction();
            }
        }

        private void DisableInteraction()
        {
            directInteractionInputObject.SetActive(false);
            rayBasedInputObject.SetActive(false);
        }
  
        private void FixDirectInteraction()
        {
            directInteractionInputObject.SetActive(directInteractionIsEnabled);
            rayBasedInputObject.SetActive(!directInteractionIsEnabled);
         }
      
        void FixRepresentation(ControllerType _controllerType, bool force=false)
        {
            if (!force && _controllerType == controllerType) return;
            controllerType = _controllerType;
            OculusController.SetActive(controllerType == ControllerType.Oculus);
            OtherController.SetActive(controllerType == ControllerType.Other);
        }

        ControllerType FindAttachedController()
        {
            List<InputDevice> deviceList = new List<InputDevice>();
            InputDevices.GetDevices(deviceList);
            foreach(var inDev in deviceList)
            {
                if (inDev.isValid && inDev.name.Contains("Oculus Touch Controller")) return ControllerType.Oculus;
            }
            return ControllerType.Other;
        }

        void OnDeviceChanged(InputDevice value)
        {
            Debug.Log($"OnDeviceChanged: {value.name}, manufacturer={value.manufacturer}, characteristics={value.characteristics}, valid={value.isValid}");
            ControllerType curController = FindAttachedController();
            FixRepresentation(curController);
        }
   
    }
}
