using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

public class AdjustCameraByDevice : MonoBehaviour {
    private float alturaOculus = 1.5f;

    // Use this for initialization
    void Update () {
        alturaOculus = 1.7f;
        //alturaOculus = StringParser.GetParsedStringToFloat(SceneController.Instance.config.camera_height, 1.5f);
        if (!XRDevice.isPresent || XRSettings.loadedDeviceName.Contains("Oculus"))
            transform.position = new Vector3(transform.position.x, alturaOculus, transform.position.z);
    }
}
