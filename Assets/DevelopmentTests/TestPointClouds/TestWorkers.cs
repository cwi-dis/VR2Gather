using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.PointCloud;

public class TestWorkers : MonoBehaviour {
    PointCloudPipeline p0;
    PointCloudPipeline p1;
    PointCloudPipeline p2;
    PointCloudPipeline p3;
    PointCloudPipeline p4;
    PointCloudPipeline p5;
    PointCloudPipeline p6;
    PointCloudPipeline p7;
    PointCloudPipeline p8;
    PointCloudPipeline p9;
        
    // Start is called before the first frame update
    void Start() {
        var tmp = Config.Instance;
        p0 = (PointCloudPipeline)new GameObject("SelfRepresentation&B2DSender").AddComponent<PointCloudPipeline>().Init(true, new User(), Config.Instance.LocalUser); 
        p1 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-1").AddComponent<PointCloudPipeline>().Init(false, new User(), Config.Instance.RemoteUser);
    }

    void Update() {
        if (p1 == null && Keyboard.current.digit1Key.wasPressedThisFrame) p1 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-1").AddComponent<PointCloudPipeline>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p2 == null && Keyboard.current.digit2Key.wasPressedThisFrame) p2 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-2").AddComponent<PointCloudPipeline>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p3 == null && Keyboard.current.digit3Key.wasPressedThisFrame) p3 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-3").AddComponent<PointCloudPipeline>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p4 == null && Keyboard.current.digit4Key.wasPressedThisFrame) p4 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-4").AddComponent<PointCloudPipeline>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p5 == null && Keyboard.current.digit5Key.wasPressedThisFrame) p5 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-5").AddComponent<PointCloudPipeline>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p6 == null && Keyboard.current.digit6Key.wasPressedThisFrame) p6 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-6").AddComponent<PointCloudPipeline>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p7 == null && Keyboard.current.digit7Key.wasPressedThisFrame) p7 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-7").AddComponent<PointCloudPipeline>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p8 == null && Keyboard.current.digit8Key.wasPressedThisFrame) p8 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-8").AddComponent<PointCloudPipeline>().Init(false, new User(), Config.Instance.RemoteUser);
        if (p9 == null && Keyboard.current.digit9Key.wasPressedThisFrame) p9 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-9").AddComponent<PointCloudPipeline>().Init(false, new User(), Config.Instance.RemoteUser);
    }
}
