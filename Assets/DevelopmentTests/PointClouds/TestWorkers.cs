using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Orchestrator.Wrapping;
using VRTCore;

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
        p0 = (PointCloudPipeline)new GameObject("SelfRepresentation&B2DSender").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.LocalUser); 
        p1 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-1").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
    }

    void Update() {
        if (p1 == null && Input.GetKeyDown(KeyCode.Alpha1)) p1 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-1").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
        if (p2 == null && Input.GetKeyDown(KeyCode.Alpha2)) p2 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-2").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
        if (p3 == null && Input.GetKeyDown(KeyCode.Alpha3)) p3 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-3").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
        if (p4 == null && Input.GetKeyDown(KeyCode.Alpha4)) p4 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-4").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
        if (p5 == null && Input.GetKeyDown(KeyCode.Alpha5)) p5 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-5").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
        if (p6 == null && Input.GetKeyDown(KeyCode.Alpha6)) p6 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-6").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
        if (p7 == null && Input.GetKeyDown(KeyCode.Alpha7)) p7 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-7").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
        if (p8 == null && Input.GetKeyDown(KeyCode.Alpha8)) p8 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-8").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
        if (p9 == null && Input.GetKeyDown(KeyCode.Alpha9)) p9 = (PointCloudPipeline)new GameObject("SUBReceiver&Representation-9").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
    }
}
