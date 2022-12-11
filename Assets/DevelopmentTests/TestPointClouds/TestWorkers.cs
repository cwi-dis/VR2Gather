using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.PointCloud;

public class TestWorkers : MonoBehaviour {
    PointCloudPipelineBase p0;
    PointCloudPipelineBase p1;
    PointCloudPipelineBase p2;
    PointCloudPipelineBase p3;
    PointCloudPipelineBase p4;
    PointCloudPipelineBase p5;
    PointCloudPipelineBase p6;
    PointCloudPipelineBase p7;
    PointCloudPipelineBase p8;
    PointCloudPipelineBase p9;
        
    // Start is called before the first frame update
    void Start() {
        var tmp = Config.Instance;
        p0 = (PointCloudPipelineBase)new GameObject("SelfRepresentation&B2DSender").AddComponent<PointCloudPipelineBase>().Init(true, new User(), Config.Instance.LocalUser); 
        p1 = (PointCloudPipelineBase)new GameObject("SUBReceiver&Representation-1").AddComponent<PointCloudPipelineBase>().Init(false, new User(), Config.Instance.RemoteUser);
    }

    void Update() {
        if (p1 == null && Keyboard.current.digit1Key.wasPressedThisFrame) p1 = (PointCloudPipelineBase)new GameObject("SUBReceiver&Representation-1").AddComponent<PointCloudPipelineBase>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p2 == null && Keyboard.current.digit2Key.wasPressedThisFrame) p2 = (PointCloudPipelineBase)new GameObject("SUBReceiver&Representation-2").AddComponent<PointCloudPipelineBase>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p3 == null && Keyboard.current.digit3Key.wasPressedThisFrame) p3 = (PointCloudPipelineBase)new GameObject("SUBReceiver&Representation-3").AddComponent<PointCloudPipelineBase>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p4 == null && Keyboard.current.digit4Key.wasPressedThisFrame) p4 = (PointCloudPipelineBase)new GameObject("SUBReceiver&Representation-4").AddComponent<PointCloudPipelineBase>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p5 == null && Keyboard.current.digit5Key.wasPressedThisFrame) p5 = (PointCloudPipelineBase)new GameObject("SUBReceiver&Representation-5").AddComponent<PointCloudPipelineBase>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p6 == null && Keyboard.current.digit6Key.wasPressedThisFrame) p6 = (PointCloudPipelineBase)new GameObject("SUBReceiver&Representation-6").AddComponent<PointCloudPipelineBase>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p7 == null && Keyboard.current.digit7Key.wasPressedThisFrame) p7 = (PointCloudPipelineBase)new GameObject("SUBReceiver&Representation-7").AddComponent<PointCloudPipelineBase>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p8 == null && Keyboard.current.digit8Key.wasPressedThisFrame) p8 = (PointCloudPipelineBase)new GameObject("SUBReceiver&Representation-8").AddComponent<PointCloudPipelineBase>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p9 == null && Keyboard.current.digit9Key.wasPressedThisFrame) p9 = (PointCloudPipelineBase)new GameObject("SUBReceiver&Representation-9").AddComponent<PointCloudPipelineBase>().Init(false, new User(), Config.Instance.RemoteUser);
    }
}
