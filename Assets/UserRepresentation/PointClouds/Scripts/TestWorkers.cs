using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestWorkers : MonoBehaviour {
    EntityPipeline p0;
    EntityPipeline p1;
    EntityPipeline p2;
    EntityPipeline p3;
    EntityPipeline p4;
    EntityPipeline p5;
    EntityPipeline p6;
    EntityPipeline p7;
    EntityPipeline p8;
    EntityPipeline p9;
        
    // Start is called before the first frame update
    void Start() {
        var tmp = Config.Instance;
        p0 = new GameObject("SelfRepresentation&B2DSender").AddComponent<EntityPipeline>().Init(Config.Instance.LocalUser); 
        p1 = new GameObject("SUBReceiver&Representation-1").AddComponent<EntityPipeline>().Init(Config.Instance.RemoteUser);
    }

    void Update() {
        if (p1 == null && Input.GetKeyDown(KeyCode.Alpha1)) p1 = new GameObject("SUBReceiver&Representation-1").AddComponent<EntityPipeline>().Init(Config.Instance.RemoteUser);
        if (p2 == null && Input.GetKeyDown(KeyCode.Alpha2)) p2 = new GameObject("SUBReceiver&Representation-2").AddComponent<EntityPipeline>().Init(Config.Instance.RemoteUser);
        if (p3 == null && Input.GetKeyDown(KeyCode.Alpha3)) p3 = new GameObject("SUBReceiver&Representation-3").AddComponent<EntityPipeline>().Init(Config.Instance.RemoteUser);
        if (p4 == null && Input.GetKeyDown(KeyCode.Alpha4)) p4 = new GameObject("SUBReceiver&Representation-4").AddComponent<EntityPipeline>().Init(Config.Instance.RemoteUser);
        if (p5 == null && Input.GetKeyDown(KeyCode.Alpha5)) p5 = new GameObject("SUBReceiver&Representation-5").AddComponent<EntityPipeline>().Init(Config.Instance.RemoteUser);
        if (p6 == null && Input.GetKeyDown(KeyCode.Alpha6)) p6 = new GameObject("SUBReceiver&Representation-6").AddComponent<EntityPipeline>().Init(Config.Instance.RemoteUser);
        if (p7 == null && Input.GetKeyDown(KeyCode.Alpha7)) p7 = new GameObject("SUBReceiver&Representation-7").AddComponent<EntityPipeline>().Init(Config.Instance.RemoteUser);
        if (p8 == null && Input.GetKeyDown(KeyCode.Alpha8)) p8 = new GameObject("SUBReceiver&Representation-8").AddComponent<EntityPipeline>().Init(Config.Instance.RemoteUser);
        if (p9 == null && Input.GetKeyDown(KeyCode.Alpha9)) p9 = new GameObject("SUBReceiver&Representation-9").AddComponent<EntityPipeline>().Init(Config.Instance.RemoteUser);
    }
}
