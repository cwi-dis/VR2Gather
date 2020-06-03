using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;

public class InitVR : MonoBehaviour {
    public float nonVRCameraHeight = 1.7f;



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
        } else
            Camera.main.transform.position = Vector3.up * nonVRCameraHeight;

    }
}
