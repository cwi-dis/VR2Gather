using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;

public class InitVR : MonoBehaviour {

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
    IEnumerator Start() {
        RenderSettings.fog = false;
        // Load XR Device
        XRSettings.LoadDeviceByName(new string[] { "Oculus", "OPenVR" });
        yield return null;
        XRSettings.enabled = true;
        yield return null;
        if (XRDevice.isPresent) {
            if (XRSettings.loadedDeviceName == "Oculus")
                ovrp_SetTrackingOriginType(TrackingOrigin.FloorLevel);
        } 
        else  {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            for (int i = 0; i < cameras.Length; ++i)
                cameras[i].transform.localPosition = Vector3.up * Config.Instance.nonHMDHeight;
        }

    }
}
