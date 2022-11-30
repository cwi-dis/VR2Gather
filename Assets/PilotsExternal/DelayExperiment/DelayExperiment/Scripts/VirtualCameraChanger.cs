using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vive.Plugin.SR;
public class VirtualCameraChanger : MonoBehaviour
{
    // This Script is prepared to wait until the ViveSRModule has been initialized, then, It will change some default parameters of the ViveSR implementation.

    public Camera[] VirtualCameras;
    public Transform ViveSRModule;
    public RenderTexture CameraRenderTexture;
    ViveSR viveSRScript;
    public Transform CameraTransform;
    void Start()
    {
        viveSRScript = ViveSRModule.GetComponent<ViveSR>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ViveSR.UpdateUnityPassThrough)
        {
            VirtualCameras = CameraTransform.parent.GetComponentsInChildren<Camera>();
            foreach (Camera VCamera in VirtualCameras)
            {
                VCamera.targetTexture = CameraRenderTexture;
                VCamera.cullingMask = 1 << 20;
                VCamera.clearFlags = CameraClearFlags.SolidColor;
            }
            


            this.enabled = false;
        }
    }
}
