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
            if (Config.Instance.VR.useLookingGlass)
            {
                return new string[] { "" };
            }
            return Config.Instance.VR.preferredDevices;
        }

        public string outputDeviceName()
        {
             // xxxjack should be check XRSettings.isDeviceActive?
            return XRSettings.loadedDeviceName;
        }

        public bool useHMD()
        {
            // xxxjack for some reason this doesn't always work, it returns false when using the Vive...
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

        public bool useHoloDisplay()
        {
            return Config.Instance.VR.useLookingGlass;
        }

        public bool useControllerEmulation()
        {
            // If emulation has specifically been asked for we return true
            if (Config.Instance.VR.preferredController == "emulation") return true;
            // If something else has specifically been asked for we return false
            if (Config.Instance.VR.preferredController != "") return false;
            // If the default has been asked for we make a best guess.
            if (useHMD()) return false;
            if (Config.Instance.VR.disableKeyboardMouse) return false;
            return true;
        }

        public bool useControllerGamepad()
        {
            // xxxjack should we check that we are _actually_ using a gamepad?
            return Config.Instance.VR.preferredController == "gamepad";
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