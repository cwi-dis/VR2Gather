using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace VRT.Core
{
    public class VRConfigJack : MonoBehaviour
    {
        private string currentOutputDevice = null;
        private string currentInputDevice = null;
        private bool initializing = false;
        private bool initialized = false;
       
        static VRConfigJack _Instance;
        public static VRConfigJack Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new VRConfigJack();
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
#if OLD_UNITY_VR
            Debug.Log($"VRConfig: {XRSettings.supportedDevices.Length} XR devices supported:");
            foreach(var d in XRSettings.supportedDevices)
            {
                Debug.Log($"VRConfig: supported device: {d}");
            }
#endif
            StartCoroutine(_LoadVR());
            initializing = true;
        }

        private IEnumerator _LoadVR()
        {
#if OLD_UNITY_VR
            string[] devices = preferredDevices();
            XRSettings.LoadDeviceByName(devices);
            yield return null;
            currentOutputDevice = XRSettings.loadedDeviceName;
            if (currentOutputDevice != "")
            {
                XRSettings.enabled = true;
            }
            yield return null;
            if (!XRSettings.isDeviceActive && XRSettings.enabled) {
                Debug.LogWarning($"VRConfig: could not load {currentOutputDevice}");
                currentOutputDevice = "";
                XRSettings.enabled = false;
            }
#else
            // xxxjack if preferred device is "" or holodisplay we should do things differently
            // xxxjack if there is a preferred device we should manually instantiate the correct loader
            Debug.Log("VRConfig: Initializing XR Loader...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
            //
            // Check which plugin was loaded, remember that, and use it to determine input plugin.
            //
            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.Log("VRConfig: No XR plugin could be loaded. Disabling XR.");
                currentOutputDevice = "";
            }
            else
            {
                Debug.Log($"VRConfig: Starting XR... {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
                XRGeneralSettings.Instance.Manager.StartSubsystems();
                // Find name of HMD subsystem. xxxjack there must be a better way...
                if (XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name == "OculusLoader")
                {
                    currentOutputDevice = "Oculus";
                }
                else
                if (XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name == "xxxjackViveLoader")
                {
                    currentOutputDevice = "OpenVR";
                }
                else
                {
                    Debug.LogError($"VRConfig: unknown XR Loader {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}. No input support.");
                    currentOutputDevice = "";
                }
            }
#endif
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
            // Cater for holographic displays
            if (currentOutputDevice == "" && Config.Instance.VR.useLookingGlass)
            {
                currentOutputDevice = "HoloDisplay";
            }
            // xxxjack should we check that the selected input device is actually available?
            string prInputName = currentInputDevice.Replace(' ', '_');
            string prOutputName = currentOutputDevice.Replace(' ', '_');
            if (prOutputName == "") prOutputName = "screen";
            BaseStats.Output("VRConfig", $"xrInput={prInputName}, xrOutput={prOutputName}");
            //
            // Do device-dependent initializations
            //
            if (currentOutputDevice == "")
            {
                initScreen();
            }
            else if (currentOutputDevice == "Oculus")
            {
                initOculus();
            }
            else if (currentOutputDevice == "HoloDisplay")
            {
                initLookingGlass();
            }
            initialized = true;
        }

        public string[] preferredDevices()
        {
            if (Config.Instance.VR.useLookingGlass)
            {
                return new string[] { "" };
            }
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
            if (!initialized)
            {
                Debug.LogWarning("VRConfig: outputDeviceName() called too early");
            }
            // xxxjack is this always correct? Or is this old-style?
            bool rv = XRSettings.enabled && XRSettings.isDeviceActive;
            return rv;
        }

        public bool useHoloDisplay()
        {
            return Config.Instance.VR.useLookingGlass;
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
            return currentInputDevice == "Oculus";
        }

        public float cameraDefaultHeight()
        {
            if (useHMD()) return 0;
            return 1.7f;    // Default camera height for non-HMD users

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

        private void initLookingGlass()
        {
            // We don't initialize the Looking Glass Portrait here, it would
            // require a code reference to the HoloPlay plugin which we don't want here.
        }

        public Camera getMainCamera()
        {
            return Camera.main;
        }

        public GameObject getMainCameraGameObject()
        {
            return Camera.main.gameObject;
        }

        public Transform getMainCameraTransform()
        {
            return Camera.main.transform;
        }
    }
}