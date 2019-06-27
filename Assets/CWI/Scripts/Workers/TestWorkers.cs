using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestWorkers : MonoBehaviour {
    EntityPipeline p0;
    EntityPipeline p1;
    EntityPipeline p2;

    // Start is called before the first frame update
    void Start() {
        var tmp = Config.Instance;
        p0 = new GameObject("PC_0").AddComponent<EntityPipeline>().Init(Config.Instance.PCs[0], transform);
    }

    void Update() {
        if (p1 == null && Input.GetKeyDown(KeyCode.Alpha1)) p1 = new GameObject("PC_1").AddComponent<EntityPipeline>().Init(Config.Instance.PCs[1], transform);
        if (p2 == null && Input.GetKeyDown(KeyCode.Alpha2)) p2 = new GameObject("PC_2").AddComponent<EntityPipeline>().Init(Config.Instance.PCs[2], transform);
    }
}
