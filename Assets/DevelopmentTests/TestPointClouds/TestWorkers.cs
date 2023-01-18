using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.PointCloud;

public class TestWorkers : MonoBehaviour {
    PointCloudPipelineSelf p0;
    PointCloudPipelineOther p1;
    PointCloudPipelineOther p2;
    PointCloudPipelineOther p3;
    PointCloudPipelineOther p4;
    PointCloudPipelineOther p5;
    PointCloudPipelineOther p6;
    PointCloudPipelineOther p7;
    PointCloudPipelineOther p8;
    PointCloudPipelineOther p9;
        
    // Start is called before the first frame update
    void Start() {
        var tmp = VRTConfig.Instance;
        p0 = (PointCloudPipelineSelf)new GameObject("SelfRepresentation&B2DSender").AddComponent<PointCloudPipelineSelf>().Init(true, new User(), VRTConfig.Instance.LocalUser); 
        p1 = (PointCloudPipelineOther)new GameObject("SUBReceiver&Representation-1").AddComponent<PointCloudPipelineOther>().Init(false, new User(), VRTConfig.Instance.RemoteUser);
    }

    void Update() {
        if (p1 == null && Keyboard.current.digit1Key.wasPressedThisFrame) p1 = (PointCloudPipelineOther)new GameObject("SUBReceiver&Representation-1").AddComponent<PointCloudPipelineOther>().Init(false, new User(), VRTConfig.Instance.RemoteUser);
        if (p2 == null && Keyboard.current.digit2Key.wasPressedThisFrame) p2 = (PointCloudPipelineOther)new GameObject("SUBReceiver&Representation-2").AddComponent<PointCloudPipelineOther>().Init(false, new User(), VRTConfig.Instance.RemoteUser);
        if (p3 == null && Keyboard.current.digit3Key.wasPressedThisFrame) p3 = (PointCloudPipelineOther)new GameObject("SUBReceiver&Representation-3").AddComponent<PointCloudPipelineOther>().Init(false, new User(), VRTConfig.Instance.RemoteUser);
        if (p4 == null && Keyboard.current.digit4Key.wasPressedThisFrame) p4 = (PointCloudPipelineOther)new GameObject("SUBReceiver&Representation-4").AddComponent<PointCloudPipelineOther>().Init(false, new User(), VRTConfig.Instance.RemoteUser);
        if (p5 == null && Keyboard.current.digit5Key.wasPressedThisFrame) p5 = (PointCloudPipelineOther)new GameObject("SUBReceiver&Representation-5").AddComponent<PointCloudPipelineOther>().Init(false, new User(), VRTConfig.Instance.RemoteUser);
        if (p6 == null && Keyboard.current.digit6Key.wasPressedThisFrame) p6 = (PointCloudPipelineOther)new GameObject("SUBReceiver&Representation-6").AddComponent<PointCloudPipelineOther>().Init(false, new User(), VRTConfig.Instance.RemoteUser);
        if (p7 == null && Keyboard.current.digit7Key.wasPressedThisFrame) p7 = (PointCloudPipelineOther)new GameObject("SUBReceiver&Representation-7").AddComponent<PointCloudPipelineOther>().Init(false, new User(), VRTConfig.Instance.RemoteUser);
        if (p8 == null && Keyboard.current.digit8Key.wasPressedThisFrame) p8 = (PointCloudPipelineOther)new GameObject("SUBReceiver&Representation-8").AddComponent<PointCloudPipelineOther>().Init(false, new User(), VRTConfig.Instance.RemoteUser);
        if (p9 == null && Keyboard.current.digit9Key.wasPressedThisFrame) p9 = (PointCloudPipelineOther)new GameObject("SUBReceiver&Representation-9").AddComponent<PointCloudPipelineOther>().Init(false, new User(), VRTConfig.Instance.RemoteUser);
    }
}
