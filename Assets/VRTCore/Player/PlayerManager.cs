using UnityEngine;
using System.Reflection;

public class PlayerManager : MonoBehaviour {
    public int      id;
    public string   orchestratorId;
    public TMPro.TextMeshProUGUI userName;
    public Camera   cam;
    public object tvm; // xxxjack public DataProviders.NetworkDataProvider tvm;
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
            pcRegister?.Invoke(null, null);
        }
        {
            System.Type wcPipelineType = System.Type.GetType("WebCamPipeline");
            MethodInfo wcRegister = wcPipelineType?.GetMethod("Register", BindingFlags.Static | BindingFlags.Public);
            wcRegister?.Invoke(null, null);
        }
        {
            System.Type tvmPipelineType = System.Type.GetType("TVMPipeline");
            MethodInfo tvmRegister = tvmPipelineType?.GetMethod("Register", BindingFlags.Static | BindingFlags.Public);
            tvmRegister?.Invoke(null, null);
        }
    }
}
