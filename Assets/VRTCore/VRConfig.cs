using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace VRT.Core
{
    public class VRConfig : MonoBehaviour
    {
        private static bool loaded = false;
        private bool initializing = false;
        private bool _initialized = false;

        [Tooltip("Always run this scene without VR (for LoginManager scene, primarily")]
        public bool disableVRforThisScene = false;
       
        public bool initialized {  get { return _initialized;  } }

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
            _DeInitVR();
            _Instance = null;
        }

        public static void InitVR()
        {
            Instance._InitVR();
        }

        public void _InitVR()
        {
            if (_initialized || initializing) return;
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

        public void _DeInitVR()
        {
            if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                Debug.Log("VRConfig: Deinitializing VR");
                XRGeneralSettings.Instance.Manager.StopSubsystems();
            }
        }
        private IEnumerator _LoadVR()
        {
            if (!loaded)
            {

                if (XRGeneralSettings.Instance.Manager.activeLoader != null)
                {
                    Debug.Log($"VRConfig: VR driver {XRGeneralSettings.Instance.Manager.activeLoader} already loaded, unloading...");
                    XRGeneralSettings.Instance.Manager.StopSubsystems();
                    XRGeneralSettings.Instance.Manager.DeinitializeLoader();
                }
                if (Config.Instance.VR.loader == "")
                {
                    // We prefer not to use VR.
                    loaded = true;
                    Debug.Log($"VRConfig: VR disabled by config.json setting");
                    _InitVRDevices(); 
                    yield break;
                }
                Debug.Log($"VRConfig: {XRGeneralSettings.Instance.Manager.activeLoaders.Count} available loaders");
                foreach (var ldr in XRGeneralSettings.Instance.Manager.activeLoaders)
                {
                    Debug.Log($"VRConfig: available loader: {ldr.name}");
                }

                if (Config.Instance.VR.loader != null)
                {
                    // We prefer a specific VR loader. First find the name of the loader.
                    if (Config.Instance.VR.loader == "LookingGlass")
                    {
                        Debug.LogError("VRConfig: LookingGlass not yet implemented");
                    }
                    // Now try and find the loader itself.
                    UnityEngine.XR.Management.XRLoader wantedLoader = null;
                    foreach (var ldr in XRGeneralSettings.Instance.Manager.activeLoaders)
                    {
                        if (ldr.name == Config.Instance.VR.loader)
                        {
                            wantedLoader = ldr;
                        }
                    }
                    if (wantedLoader == null)
                    {
                        Debug.LogError($"VRConfig: cannot find loader {Config.Instance.VR.loader}");
                    }
                    else
                    {
                        XRGeneralSettings.Instance.Manager.TryRemoveLoader(wantedLoader);
                        XRGeneralSettings.Instance.Manager.TryAddLoader(wantedLoader, 0);
                    }
                }
                {
                    // We automatically select the VR device to use, if any.
                    Debug.Log("VRConfig: Initializing XR Loader...");
                    yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
                }
           }
            loaded = true;
            Debug.Log($"VRConfig: loaded VR driver \"{XRSettings.loadedDeviceName}\"");
            _InitVRDevices();
        }

        private void _InitVRDevices()
        {
            if (disableVRforThisScene || Config.Instance.VR.loader == "")
            {
                if (disableVRforThisScene)
                {
                    Debug.Log("VRConfig: VR disabled for this scene");
                }
                else
                {
                    Debug.Log("VRConfig: VR disabled through config.json setting");
                }
                if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
                {
                    XRGeneralSettings.Instance.Manager.StopSubsystems();
                }
                _initialized = true;
                return;
            }
            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.Log("VRConfig: No XR plugin could be loaded. XR not enabled.");
            }
            else if (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                Debug.LogError($"VRConfig: initialization incomplete for activeLoader {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
            }
            else
            {
                Debug.Log($"VRConfig: Starting XR... {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
                XRGeneralSettings.Instance.Manager.StartSubsystems();
                // Find name of HMD subsystem. xxxjack there must be a better way...
                string loaderName = XRGeneralSettings.Instance.Manager.activeLoader.name;
               
            }

#if xxxjack_needs_fixing
            // Cater for holographic displays
            if (currentOutputDevice == "" && Config.Instance.VR.useLookingGlass)
            {
                currentOutputDevice = "lookingglass";
            }
#endif
            _initialized = true;
            // xxxjack should we check that the selected input device is actually available?
            string prOutputName = outputDeviceName();
            prOutputName = prOutputName.Replace(' ', '_');
            if (prOutputName == "") prOutputName = "none";
            BaseStats.Output("VRConfig", $"xrOutput={prOutputName}");
            // Do device-dependent initializations
            if (prOutputName == "")
            {
                initScreen();
            }
            else if (prOutputName == "Oculus")
            {
                initOculus();
            } else if (prOutputName == "lookingglass")
            {
                initLookingGlass();
            } else
            {
                Debug.LogWarning($"VRConfig: unknown device \"{prOutputName}\", no initialization performed.");
            }
        }

        public string outputDeviceName()
        {
            if (!_initialized)
            {
                Debug.LogError("VRConfig: outputDeviceName() called too early");
            }
            return XRSettings.loadedDeviceName;
        }

        public bool useHMD()
        {
            if (!_initialized)
            {
                Debug.LogError("VRConfig: outputDeviceName() called too early");
            }
            bool rv = XRGeneralSettings.Instance.Manager.isInitializationComplete && XRSettings.isDeviceActive;
            return rv;
        }

        public bool useHoloDisplay()
        {
            return Config.Instance.VR.loader == "Lookingglass";
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
