using UnityEngine;
using System.Reflection;
using VRTCore;

public class PlayerManager : MonoBehaviour {
    public int      id;
    public string   orchestratorId;
    public TMPro.TextMeshProUGUI userName;
    public Camera   cam;
    public ITVMHookUp tvm;
    public GameObject avatar;
    public GameObject webcam;
    public GameObject pc;
    public GameObject audio;
    public GameObject teleporter;

    public void Awake()
    {
        Debug.Log("PlayerManager: Registering Pipeline classes for UserRepresentations");
        {
            System.Type pcPipelineType = System.Type.GetType("PointCloudPipeline");
            MethodInfo pcRegister = pcPipelineType?.GetMethod("Register", BindingFlags.Static | BindingFlags.Public);
            if (pcRegister == null) Debug.LogWarning("PlayerManager: No support for Pointclouds, PointCloudPipeline type or register-function not found");
            pcRegister?.Invoke(null, null);
        }
        {
            System.Type wcPipelineType = System.Type.GetType("WebCamPipeline");
            MethodInfo wcRegister = wcPipelineType?.GetMethod("Register", BindingFlags.Static | BindingFlags.Public);
            if (wcRegister == null) Debug.LogWarning("PlayerManager: No support for WebCam, WebCamPipeline type or register-function not found");
            wcRegister?.Invoke(null, null);
        }
        {
            System.Type tvmPipelineType = System.Type.GetType("TVMPipeline");
            MethodInfo tvmRegister = tvmPipelineType?.GetMethod("Register", BindingFlags.Static | BindingFlags.Public);
            if (tvmRegister == null) Debug.LogWarning("PlayerManager: No support for TVMs, TVMPipeline type or register-function not found");
            tvmRegister?.Invoke(null, null);
        }
    }
}
