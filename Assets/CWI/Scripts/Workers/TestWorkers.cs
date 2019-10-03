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

    // Start is called before the first frame update
    void Start() {
        var tmp = Config.Instance;
        p0 = new GameObject("User_1").AddComponent<EntityPipeline>().Init(Config.Instance.Users[0], transform); 
        p1 = new GameObject("User_2").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
        //p2 = new GameObject("User_3").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
    }

    void Update() {
        if (p1 == null && Input.GetKeyDown(KeyCode.Alpha1)) p1 = new GameObject("User_2").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
        if (p2 == null && Input.GetKeyDown(KeyCode.Alpha2)) p2 = new GameObject("User_3").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
        if (p3 == null && Input.GetKeyDown(KeyCode.Alpha3)) p3 = new GameObject("User_3").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
        if (p4 == null && Input.GetKeyDown(KeyCode.Alpha4)) p4 = new GameObject("User_3").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
        if (p5 == null && Input.GetKeyDown(KeyCode.Alpha5)) p5 = new GameObject("User_3").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
        if (p6 == null && Input.GetKeyDown(KeyCode.Alpha6)) p6 = new GameObject("User_3").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
        if (p7 == null && Input.GetKeyDown(KeyCode.Alpha7)) p7 = new GameObject("User_3").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
    }
}
