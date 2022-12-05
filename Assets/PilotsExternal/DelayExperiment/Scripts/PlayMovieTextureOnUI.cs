using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayMovieTextureOnUI : MonoBehaviour
{
        public RawImage rawimage;
    WebCamTexture webcamTexture;
        void Start()
        {

        WebCamDevice[] cam_devices = WebCamTexture.devices;
        // for debugging purposes, prints available devices to the console
        for (int i = 0; i < cam_devices.Length; i++)
        {
            print("Webcam available: " + cam_devices[i].name);
        }
        webcamTexture = new WebCamTexture(cam_devices[0].name);
            rawimage.texture = webcamTexture;
            rawimage.material.mainTexture = webcamTexture;
            webcamTexture.Play();
        }
    

    private void OnApplicationQuit()
    {
        webcamTexture.Stop();
    }
}
