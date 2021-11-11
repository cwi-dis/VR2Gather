using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRT.Core
{
    public class VRConfig
    {

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

        public string[] preferredDevices()
        {
            return new string[] { "Oculus", "OPenVR" };
        }

        public string outputDeviceName()
        {
            // xxxjack should be check XRSettings.isDeviceActive?
            return XRSettings.loadedDeviceName;
        }

        public bool useHMD()
        {
            var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances<XRDisplaySubsystem>(xrDisplaySubsystems);
            foreach (var xrDisplay in xrDisplaySubsystems)
            {
                if (xrDisplay.running)
                {
                    return true;
                }
            }
            return false;
        }

        public bool useControllerEmulation()
        {
            if (useHMD()) return false;
            if (Config.Instance.VR.disableKeyboardMouse) return false;
            return true;
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
    }
}