using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class InitVR : MonoBehaviour {
    // Start is called before the first frame update
    IEnumerator Start() {
        RenderSettings.fog = false;
        // Load XR Device
        XRSettings.LoadDeviceByName(new string[] { "Oculus", "OPenVR" });
        yield return null;
        XRSettings.enabled = true;
    }
}
