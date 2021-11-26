using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;

namespace VRT.Core
{
    public class VRConfig : MonoBehaviour
    {
        private string currentOutputDevice = null;
        private string currentInputDevice = null;
        private bool initializing = false;
        private bool initialized = false;
       
        static VRConfig _Instance;
        public static VRConfig Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new VRConfig();
                }
                return _Instance;
            }
        }

        private void Awake()
        {
            if (_Instance != null && _Instance != this)
            {
                Debug.LogWarning("VRConfig: duplicate instance, destroying this (new) instance");
                Destroy(gameObject);
                return;
            }
            _Instance = this;
            DontDestroyOnLoad(this.gameObject);
            _InitVR();
        }

        public static void InitVR()
        {
            Instance._InitVR();
        }

        public void _InitVR()
        {
            if (initialized || initializing) return;
            Debug.Log($"VRConfig: {XRSettings.supportedDevices.Length} XR devices supported:");
            foreach(var d in XRSettings.supportedDevices)
            {
                Debug.Log($"VRConfig: device={d}");
            }
            StartCoroutine(_LoadVR());
            initializing = true;
        }

        private IEnumerator _LoadVR()
        {
            string[] preferredDevices = VRConfig.Instance.preferredDevices();
            Debug.Log($"xxxjack loadDeviceByName {preferredDevices}");
            XRSettings.LoadDeviceByName(preferredDevices);
            yield return null;
            currentOutputDevice = XRSettings.loadedDeviceName;
            Debug.Log($"xxxjack loadedDevice={currentOutputDevice}");
            XRSettings.enabled = true;
            yield return null;
            Debug.Log($"xxxjack isDeviceActive {XRSettings.isDeviceActive}");
            if (!XRSettings.isDeviceActive && XRSettings.enabled) {
                Debug.LogWarning($"VRConfig: could not load {currentOutputDevice}");
                currentOutputDevice = "";
                XRSettings.enabled = false;
            }
            if (Config.Instance.VR.preferredController != "")
            {
                currentInputDevice = Config.Instance.VR.preferredController;
            }
            else
            {
                // if no controller override was specified
                // we use the controller corresponding to the output
                // device
                currentInputDevice = currentOutputDevice;
                if (currentInputDevice == "") currentInputDevice = "emulation";
            }
            string prInputName = currentInputDevice.Replace(' ', '_');
            string prOutputName = currentOutputDevice.Replace(' ', '_');
            if (prOutputName == "") prOutputName = "none";
            BaseStats.Output("VRConfig", $"xrInput={prInputName}, xrOutput={prOutputName}");
            // Do device-dependent initializations
            if (currentOutputDevice == "")
            {
                initScreen();
            }
            else if (currentOutputDevice == "Oculus")
            {
                initOculus();
            }
            initialized = true;
        }

        public string[] preferredDevices()
        {
            return Config.Instance.VR.preferredDevices;
        }

        public string outputDeviceName()
        {
            if (!initialized)
            {
                Debug.LogWarning("VRConfig: outputDeviceName() called too early");
            }
            return currentOutputDevice;
        }

        public bool useHMD()
        {
            bool rv = XRSettings.enabled && XRSettings.isDeviceActive;
            return rv;
#if xxxjack_removed
            // xxxjack for some reason this doesn't always work, it returns false when using the Vive...
            var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
            int nEnabled = 0;
            Debug.Log($"VRConfig: {xrDisplaySubsystems.Count} subsystems");
            foreach (var xrDisplay in xrDisplaySubsystems)
            {
                if (xrDisplay.running)
                {
                    nEnabled += 1;
                }
            }
            return nEnabled >= 1;
#endif
        }

        public bool useControllerEmulation()
        {
            if (!initialized)
            {
                Debug.LogWarning("VRConfig: useControllerEmulation() called too early");
            }
            return currentInputDevice == "emulation";
        }

        public bool useControllerGamepad()
        {
            if (!initialized)
            {
                Debug.LogWarning("VRConfig: useControllerGamepad() called too early");
            }
            // xxxjack should we check that we are _actually_ using a gamepad?
            return currentInputDevice == "gamepad";
        }

        public bool useControllerOpenVR()
        {
            if (!initialized)
            {
                Debug.LogWarning("VRConfig: useControllerOpenVR() called too early");
            }
            return currentInputDevice == "OpenVR";
        }

        public bool useControllerOculus()
        {
            if (!initialized)
            {
                Debug.LogWarning("VRConfig: useControllerOculus() called too early");
            }
            // xxxjack should we check that we are _actually_ using a gamepad?
            return currentInputDevice == "Oculus";
        }

        public float cameraDefaultHeight()
        {
            if (useHMD()) return 0;
            return 1.7f;    // Default camera height for non-HMD users.
        }

        public void initScreen()
        {
            Resolution[] resolutions = Screen.resolutions;
            bool fullRes = false;
            foreach (var res in resolutions)
            {
                if (res.width == 1920 && res.height == 1080) fullRes = true;
            }
            if (fullRes) Screen.SetResolution(1920, 1080, false, 30);
            else Screen.SetResolution(1280, 720, false, 30);
            Debug.Log("Resolution: " + Screen.width + "x" + Screen.height);
        }

        private const string oculusPluginName = "OVRPlugin";
        public enum OculusBool
        {
            False = 0,
            True
        }
        public enum OculusTrackingOrigin
        {
            EyeLevel = 0,
            FloorLevel = 1,
            Stage = 2,
            Count,
        }
        [DllImport(oculusPluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern OculusBool ovrp_SetTrackingOriginType(OculusTrackingOrigin originType);

        private void initOculus()
        {
            ovrp_SetTrackingOriginType(OculusTrackingOrigin.FloorLevel);
        }
    }
}