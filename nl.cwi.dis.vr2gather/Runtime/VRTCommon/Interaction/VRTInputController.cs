using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
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

        [Tooltip("Left hand GameObject")]
        [SerializeField] GameObject lhGameObject;
        [Tooltip("Right hand GameObject")]
        [SerializeField] GameObject rhGameObject;
        [Tooltip("Handsfree GameObject")]
        [SerializeField] GameObject nhGameObject;

        [Tooltip("If hands have ever been activated they remain so, even if controllers disappear")]
        public bool stickyHands = true;
        bool handsHaveBeenEnabled = false;

        [Tooltip("Currently active real physical controller type")]
        [DisableEditing][SerializeField] ControllerType m_currentRealController;
        public ControllerType currentRealController { get => m_currentRealController; }

        [Tooltip("Currently active visible controller type")]
        [DisableEditing][SerializeField] ControllerType m_currentVisibleController;
        public ControllerType currentVisibleController { get => m_currentVisibleController; }

        [Tooltip("Callbacks called when the controller changes")]
        public System.Action controllerChanged;

        [Tooltip("Introspection/Debug: print log messages")]
        [SerializeField] bool debug = false;

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
            if (debug)
            {
                UnityEngine.Debug.Log($"VRTInputController: examine {deviceList.Count} devices");
            }
            foreach (var inDev in deviceList)
            {
                if (debug)
                {
                    UnityEngine.Debug.Log($"VRTInputController: examine device \"{inDev.name}\", valid={inDev.isValid}");
                }
                if (!inDev.isValid) continue;
                
                if ((inDev.characteristics & InputDeviceCharacteristics.Controller) == 0) continue;

                foundController = true;
                if (debug)
                {
                    UnityEngine.Debug.Log($"VRTInputController: is a controller");
                }
                if (inDev.name.Contains("Oculus") || inDev.manufacturer == "Oculus")
                {
                    foundOculusController = true;
                    if (debug)
                    {
                        UnityEngine.Debug.Log($"VRTInputController: is Oculus Controller");
                    }
                } else
                if (inDev.name.Contains("HTC Vive Controller"))
                {
                    foundViveController = true;
                    if (debug)
                    {
                        UnityEngine.Debug.Log($"VRTInputController: is Vive controller");
                    }
                } else
                {
                    UnityEngine.Debug.LogWarning($"VRTInputController: treat \"{inDev.name}\" as generic controller");
                }
            }
            if (foundOculusController) return ControllerType.Oculus;
            if (foundViveController) return ControllerType.Vive;
            if (foundController) return ControllerType.OtherController;
            return ControllerType.None;
        }

        void OnDeviceChanged(InputDevice value)
        {
            UnityEngine.Debug.Log($"VRTInputController: OnDeviceChanged: {value.name}, manufacturer={value.manufacturer}, characteristics={value.characteristics}, valid={value.isValid}");
            ControllerType bestController = FindAttachedController();
            if (bestController != m_currentRealController)
            {
                OnRealControllerChanged(bestController);
            }
        }

        void OnRealControllerChanged(ControllerType newController)
        {
            if (debug)
            {
                UnityEngine.Debug.Log($"VRTInputController: controllerType={newController}, directInteraction={directInteractionIsEnabled}");
            }
            m_currentRealController = newController;
            m_currentVisibleController = m_currentRealController;
            if (directInteractionIsEnabled)
            {
                m_currentVisibleController = ControllerType.VirtualHand;
            }
            //
            // A bit convoluted logic here. Usually we don't want to disable
            // the hands if they have ever been enabled (so any logic on the hands continues
            // working) but this is not alwasy what we want. So it can be
            // controlled with a setting.
            bool handsShouldBeEnabled = newController != ControllerType.None;
            if (handsShouldBeEnabled)
            {
                handsHaveBeenEnabled = true;
            }
            if (stickyHands && handsHaveBeenEnabled)
            {
                handsShouldBeEnabled = true;
            }
            if (handsShouldBeEnabled)
            {
                UnityEngine.Debug.Log($"VRTInputController: Enabling hands");
            }
            else
            {
                UnityEngine.Debug.Log($"VRTInputController: Disabling hands, enabling handsfree");
            }
                rhGameObject.SetActive(handsShouldBeEnabled); 
            lhGameObject.SetActive(handsShouldBeEnabled); 
            nhGameObject.SetActive(!handsShouldBeEnabled); 
            // xxxjack should enable/disable correct control scheme
            //
            // And tell the various interested parties (probably left and right hand) that they may need to change their
            // visual representation.
            //
            controllerChanged.Invoke();
        }
    }

}
