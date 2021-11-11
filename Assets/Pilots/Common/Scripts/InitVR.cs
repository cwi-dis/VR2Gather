using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using VRT.Core;

namespace VRT.Pilots.Common
{
    public class InitVR : MonoBehaviour
    {

        private const string pluginName = "OVRPlugin";
        public enum Bool
        {
            False = 0,
            True
        }
        public enum TrackingOrigin
        {
            EyeLevel = 0,
            FloorLevel = 1,
            Stage = 2,
            Count,
        }

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetTrackingOriginType(TrackingOrigin originType);

        // Start is called before the first frame update
        IEnumerator Start()
        {
            RenderSettings.fog = false;
            // Load XR Device.
            // xxxjack The logic here is slightly different from what is suggested by
            // https://docs.unity3d.com/2019.4/Documentation/ScriptReference/XR.XRSettings.LoadDeviceByName.html
            //
            XRSettings.LoadDeviceByName(VRConfig.Instance.preferredDevices());
            yield return null;
            XRSettings.enabled = true;
            yield return null;
            if (VRConfig.Instance.useHMD())
            {
                if (VRConfig.Instance.outputDeviceName() == "Oculus")
                    ovrp_SetTrackingOriginType(TrackingOrigin.FloorLevel);
            }
            else
            {
                // xxxjack I don't understand why this functionality is here... Should it be?
                Debug.Log("pushing cameras");
                Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
                for (int i = 0; i < cameras.Length; ++i)
                {
                    if (cameras[i].gameObject.layer != 10) // 10: Spectator
                        cameras[i].transform.localPosition = Vector3.up * Config.Instance.nonHMDHeight;
                }
            }

        }
    }
}