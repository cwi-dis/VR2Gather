using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.PointCloud;
using VRTCore;

namespace VRT.MCU
{
    public class MCUTest : MonoBehaviour
    {

        PointCloudPipeline fusedPC;
        bool connected = false;
        bool initialized = false;
        public byte id = 0;
        public string url = "";
        float[] pos = { 0.0f, 0.0f, 0.0f };
        float rotation = 0.0f;
        public int[] fov = { 1, 1 };
        public int[] lod = { 0, 3 };
        [SerializeField] CheckVisibility p = null;

        // Start is called before the first frame update
        void Start()
        {
            connected = mcu._API.Connect("127.0.0.1", 8080);
        }

        // Update is called once per frame
        void Update()
        {
            pos[0] = p.gameObject.transform.position.x;
            pos[1] = p.gameObject.transform.position.y;
            pos[2] = p.gameObject.transform.position.z;
            rotation = p.gameObject.transform.rotation.eulerAngles.y;

            fov[0] = p.fov;
            lod[0] = p.lod;

            if (!connected) connected = mcu._API.Connect("127.0.0.1", 8080);
            else
            {
                if (!initialized) initialized = mcu._API.SendInit(id, url, pos, rotation, fov, lod);
                else
                {

                    if (fusedPC == null) fusedPC = (PointCloudPipeline)new GameObject("FusedPC").AddComponent<PointCloudPipeline>().Init(new User(), Config.Instance.RemoteUser);
                    if (Input.GetKeyDown(KeyCode.Alpha1)) mcu._API.SendPosition(id, pos);
                    //if (Input.GetKeyDown(KeyCode.Alpha2)) mcu._API.SendRotation(id, rotation);
                    if (Input.GetKeyDown(KeyCode.Alpha3)) mcu._API.SendFOV(id, fov);
                    if (Input.GetKeyDown(KeyCode.Alpha4)) mcu._API.SendLOD(id, lod);
                    //if (Input.GetKeyDown(KeyCode.Alpha5)) mcu._API.SendDisconnect(id);
                }
            }
        }
    }
}