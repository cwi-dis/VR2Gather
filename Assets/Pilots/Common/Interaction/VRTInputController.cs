using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XInput;
using UnityEngine.XR;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// This component listens for changes in the attached input devices. Based on the currently
    /// available set of devices it will select the Input Manager control schemes to activate and deactivate
    /// (so we can use different bindings for different controller) and it will help with selecting the right
    /// controller visual rendition.
    /// </summary>
    public class VRTInputController : MonoBehaviour
    {
        /// <summary>
        /// Supported controller types. When adding types also add the name substring to
        /// FindAttachedController().
        /// </summary>
        public enum ControllerType
        {
            None,
            OtherController,
            Oculus,
            Vive,
            VirtualHand
        };

        [Tooltip("Currently active real physical controller type")]
        [DisableEditing][SerializeField] ControllerType m_currentRealController;
        public ControllerType currentRealController { get => m_currentRealController; }

        [Tooltip("Currently active visible controller type")]
        [DisableEditing][SerializeField] ControllerType m_currentVisibleController;
        public ControllerType currentVisibleController { get => m_currentVisibleController; }

        [Tooltip("Callbacks called when the controller changes")]
        public System.Action controllerChanged;

        [Tooltip("Introspection/debug: is direct interaction currently enabled")]
        [DisableEditing][SerializeField] private bool directInteractionIsEnabled;


        // Start is called before the first frame update
        void Awake()
        {
        }

        void Start()
        {
            InputDevices.deviceConnected += OnDeviceChanged;
            InputDevices.deviceDisconnected += OnDeviceChanged;
            InputDevices.deviceConfigChanged += OnDeviceChanged;

            directInteractionIsEnabled = PilotController.Instance.directInteractionAllowed;
            ControllerType bestController = FindAttachedController();
            OnRealControllerChanged(bestController);
        }

        private void OnDestroy()
        {
            InputDevices.deviceConnected -= OnDeviceChanged;
            InputDevices.deviceDisconnected -= OnDeviceChanged;
            InputDevices.deviceConfigChanged -= OnDeviceChanged;
        }

        void Update()
        {
            if (PilotController.Instance.directInteractionAllowed != directInteractionIsEnabled)
            {
                directInteractionIsEnabled = PilotController.Instance.directInteractionAllowed;
                OnRealControllerChanged(m_currentRealController);
            }
        }

        ControllerType FindAttachedController()
        {
            List<InputDevice> deviceList = new List<InputDevice>();
            InputDevices.GetDevices(deviceList);
            bool foundOculusController = false;
            bool foundViveController = false;
            bool foundController = false;
            foreach (var inDev in deviceList)
            {
                if (!inDev.isValid) continue;
                if ((inDev.characteristics & InputDeviceCharacteristics.Controller) == 0) continue;
                foundController = true;
                if (inDev.name.Contains("Oculus Touch Controller")) foundOculusController = true;
                if (inDev.name.Contains("HTC Vive Controller")) foundViveController = true;
            }
            if (foundOculusController) return ControllerType.Oculus;
            if (foundViveController) return ControllerType.Vive;
            if (foundController) return ControllerType.OtherController;
            return ControllerType.None;
        }

        void OnDeviceChanged(InputDevice value)
        {
            Debug.Log($"VRTInputController: OnDeviceChanged: {value.name}, manufacturer={value.manufacturer}, characteristics={value.characteristics}, valid={value.isValid}");
            ControllerType bestController = FindAttachedController();
            if (bestController != m_currentRealController)
            {
                OnRealControllerChanged(bestController);
            }
        }

        void OnRealControllerChanged(ControllerType newController)
        {
            m_currentRealController = newController;
            m_currentVisibleController = m_currentRealController;
            if (directInteractionIsEnabled)
            {
                m_currentVisibleController = ControllerType.VirtualHand;
            }
            // xxxjack should enable/disable correct control scheme
            //
            // And tell the various interested parties (probably left and right hand) that they may need to change their
            // visual representation.
            //
            controllerChanged.Invoke();
        }
    }

}
