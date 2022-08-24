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
        private static bool loaded = false;
        private bool initializing = false;
        private bool initialized = false;

        [Tooltip("Always run this scene without VR (for LoginManager scene, primarily")]
        public bool disableVRforThisScene = false;
       
        public bool isInitialized()
        {
            return initialized;
        }

        static VRConfig _Instance;
        public static VRConfig Instance
        {
            get
            {
                if (_Instance == null)
                {
                    Debug.LogWarning("VRConfig: attempting to get Instance before it was created in the scene.");
                }
                return _Instance;
            }
        }

        private void Awake()
        {
            if (_Instance == this)
            {
                Debug.LogWarning("VRConfig: Awake called a second time");
                return;
            }
            if (_Instance != null)
            {
                Debug.Log("VRConfig: new instance, deleting old one");
                _Instance = null;
                return;
            }
            _Instance = this;
            
            _InitVR();
        }

        private void OnDestroy()
        {
            XRSettings.enabled = false;
            _Instance = null;
        }

        public static void InitVR()
        {
            Instance._InitVR();
        }

        public void _InitVR()
        {
            if (initialized || initializing) return;
            if (!loaded)
            {
                StartCoroutine(_LoadVR());
                initializing = true;
            }
            else
            {
                _InitVRDevices();
            }
        }

        private IEnumerator _LoadVR()
        {
            if (!loaded)
            {
                Debug.Log($"VRConfig: {XRSettings.supportedDevices.Length} XR devices supported:");
                foreach (var d in XRSettings.supportedDevices)
                {
                    Debug.Log($"VRConfig: device={d}");
                }
                string[] devices = preferredDevices();
                Debug.Log($"VRConfig: preferred load order: {string.Join(", ", devices)}");
                XRSettings.LoadDeviceByName(devices);
                yield return null;
            }
            loaded = true;
            Debug.Log($"VRConfig: loaded VR driver {XRSettings.loadedDeviceName}");
            _InitVRDevices();
        }

        private void _InitVRDevices()
        {
            if (disableVRforThisScene)
            {
                Debug.Log("VRConfig: VR disabled for this scene");
                currentInputDevice = "emulation";
                currentOutputDevice = "";
                XRSettings.enabled = false;
                initialized = true;
                return;
            }
            currentOutputDevice = XRSettings.loadedDeviceName;
            if (currentOutputDevice != "")
            {
                XRSettings.enabled = true;
                Debug.Log($"VRConfig: VR enabled for this scene, device {currentOutputDevice}");
            }
            else
            {
                Debug.Log($"VRConfig: VR could be enabled for this scene, but no VR driver loaded");
            }

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
            // Cater for holographic displays
            if (currentOutputDevice == "" && Config.Instance.VR.useLookingGlass)
            {
                currentOutputDevice = "lookingglass";
            }
            // xxxjack should we check that the selected input device is actually available?
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
            } else if (currentOutputDevice == "lookingglass")
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
                Debug.LogError("VRConfig: outputDeviceName() called too early");
            }
            return currentOutputDevice;
        }

        public bool useHMD()
        {
            if (!initialized)
            {
                Debug.LogError("VRConfig: outputDeviceName() called too early");
            }
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
                Debug.LogError("VRConfig: useControllerEmulation() called too early");
            }
            return currentInputDevice == "emulation";
        }

        public bool useControllerGamepad()
        {
            if (!initialized)
            {
                Debug.LogError("VRConfig: useControllerGamepad() called too early");
            }
            return currentInputDevice == "gamepad";
        }

        public bool useControllerOpenVR()
        {
            if (!initialized)
            {
                Debug.LogError("VRConfig: useControllerOpenVR() called too early");
            }
            return currentInputDevice == "OpenVR";
        }

        public bool useControllerOculus()
        {
            if (!initialized)
            {
                Debug.LogError("VRConfig: useControllerOculus() called too early");
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