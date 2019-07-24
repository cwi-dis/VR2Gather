using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestWorkers : MonoBehaviour {
    EntityPipeline p0;
    EntityPipeline p1;
    EntityPipeline p2;
    string pc_url1;
    string audio_url1;

    // Start is called before the first frame update
    void Start() {
        var tmp = Config.Instance;
        //p0 = new GameObject("User_1").AddComponent<EntityPipeline>().Init(Config.Instance.Users[0], transform);
        p0 = new GameObject("User_1").AddComponent<EntityPipeline>().Init(Config.Instance.Users[0], transform, "Marc", "https://vrt-evanescent.viaccess-orca.com/pc-Marc/testBed.mpd", "https://vrt-evanescent.viaccess-orca.com/audio-Marc/audio.mpd");
    }

    void Update() {
        if (p1 == null && Input.GetKeyDown(KeyCode.Alpha1)) p1 = new GameObject("User_2").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
        if (p2 == null && Input.GetKeyDown(KeyCode.Alpha2)) p2 = new GameObject("User_3").AddComponent<EntityPipeline>().Init(Config.Instance.Users[2], transform);
    }
}
