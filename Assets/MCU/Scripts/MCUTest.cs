using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MCUTest : MonoBehaviour {

    EntityPipeline fusedPC;
    bool connected = false;
    bool initialized = false;
    public byte id = 0;
    public string url = "";
    float[] pos = { 0.0f, 0.0f, 0.0f };
    float rotation = 0.0f;
    byte[] fov = { 1, 1 };

    // Start is called before the first frame update
    void Start()
    {
        connected = mcu._API.Connect("127.0.0.1", 8080);
    }

    // Update is called once per frame
    void Update() {
        pos[0] = gameObject.transform.position.x;
        pos[1] = gameObject.transform.position.y;
        pos[2] = gameObject.transform.position.z;
        rotation = gameObject.transform.rotation.eulerAngles.y;

        if (!connected) connected = mcu._API.Connect("127.0.0.1", 8080);
        else {
            if (!initialized) initialized = mcu._API.SendInit(id, url, pos, rotation);
            else {
                if (fusedPC == null) fusedPC = new GameObject("FusedPC").AddComponent<EntityPipeline>().Init(Config.Instance.Users[1], transform);
                if (Input.GetKeyDown(KeyCode.Alpha1)) mcu._API.SendPosition(id, pos);
                if (Input.GetKeyDown(KeyCode.Alpha2)) mcu._API.SendRotation(id, rotation);
                if (Input.GetKeyDown(KeyCode.Alpha3)) mcu._API.SendFOV(id, fov);
                if (Input.GetKeyDown(KeyCode.Alpha4)) mcu._API.SendDisconnect(id);
            }
        }
    }
}
