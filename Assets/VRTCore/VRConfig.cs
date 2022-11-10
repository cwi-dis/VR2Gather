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
        private static bool loaderInitialized = false;
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

        private void OnApplicationQuit()
        {
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            loaderInitialized = false;
        }

        private void Awake()
        {
            if (_Instance == null)
            {
                _Instance = this;
            }
            else
            {
                Debug.LogWarning("VRConfig: Awake called a second time, ignored");
            }
        }

        private void Start()
        {
         }

        private void Update()
        {
            if(!initialized && !initializing)
            {
                initializing = true;
                StartCoroutine(_LoadAndStartVR());
            }
        }

        private void OnDestroy()
        {
            _StopVR();
            _Instance = null;
        }

        public void _StopVR()
        {
            if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
            {
                Debug.Log("VRConfig: Deinitializing VR");
                XRGeneralSettings.Instance.Manager.StopSubsystems();
            }
            _initialized = false;
        }

        private IEnumerator _LoadAndStartVR()
        {
            if (!loaderInitialized)
            {
                // First time we get here (during this application run or editor play).
                // We load the loader wanted.
                if (XRGeneralSettings.Instance.Manager != null && XRGeneralSettings.Instance.Manager.activeLoader != null)
                {
                    Debug.Log($"VRConfig: VR driver {XRGeneralSettings.Instance.Manager.activeLoader} already loaded, stopping and unloading...");
                    XRGeneralSettings.Instance.Manager.StopSubsystems();
                    XRGeneralSettings.Instance.Manager.DeinitializeLoader();
                }
                if (Config.Instance.VR.loader == "")
                {
                    // We prefer not to use VR during this whole run.
                    loaderInitialized = true;
                    Debug.Log($"VRConfig: VR disabled by config.json setting");
                    yield break;
                }
                else
                {
#if xxxjack_debug_vr_loaders
                    Debug.Log($"VRConfig: {XRGeneralSettings.Instance.Manager.activeLoaders.Count} available loaders");
                    foreach (var ldr in XRGeneralSettings.Instance.Manager.activeLoaders)
                    {
                        Debug.Log($"VRConfig: available loader: {ldr.name}");
                    }
#endif
                    if (Config.Instance.VR.loader != null)
                    {
                        // We prefer a specific VR loader. Re-order loaders so the correct one is first.
                        // First find the name of the loader.
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
                    // The loaders are now in the correct order (if we have overrided through a config.json
                    // setting). We simply load the first one that works.
                    Debug.Log("VRConfig: Initializing XR Loader...");
                    yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
                }
            }
            loaderInitialized = true;
            Debug.Log($"VRConfig: loaded VR driver \"{XRSettings.loadedDeviceName}\"");
            if (disableVRforThisScene)
            {
                // We don't want VR in this scene. Stop it (if it has been loaded).
                Debug.Log("VRConfig: VR disabled for this scene");
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                _initialized = true;
                yield break;
            }
            if (Config.Instance.VR.loader != "")
            {
                // We want VR, if available. Ensure we have a loader, and it has been initialized.
                if (XRGeneralSettings.Instance.Manager.activeLoader == null)
                {
                    Debug.Log("VRConfig: No XR plugin could be loaded. XR not enabled.");
                }
                else if (!XRGeneralSettings.Instance.Manager.isInitializationComplete)
                {
                    Debug.LogError($"VRConfig: initialization incomplete for activeLoader {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
                }
            }
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                Debug.Log($"VRConfig: Starting XR... {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
                XRGeneralSettings.Instance.Manager.StartSubsystems();
            }
#if xxxjack_needs_fixing
            // Cater for holographic displays
            if (currentOutputDevice == "" && Config.Instance.VR.useLookingGlass)
            {
                currentOutputDevice = "lookingglass";
            }
#endif
            _initialized = true;
            
            string prOutputName = outputDeviceName();
            prOutputName = prOutputName.Replace(' ', '_');
            if (prOutputName == "") prOutputName = "none";
#if VRT_WITH_STATS
            BaseStats.Output("VRConfig", $"xrOutput={prOutputName}");
#endif
            
            // Do device-dependent initializations
            if (prOutputName == "none")
            {
                initScreen();
            }
            else if (prOutputName == "Oculus")
            {
                initOculus();
            }
            else if (prOutputName == "lookingglass")
            {
                initLookingGlass();
            }
            else if (prOutputName == "OpenXR_Display")
            {
                // Nothing to do.
            }
            else
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
