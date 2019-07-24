using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudsMainController : MonoBehaviour {
    // Start is called before the first frame update
    public string subURL;
    void Start() {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            var pc = Config.Instance.Users[0];
            PointCloudBaseRenderer pct = null;
            if (pc.Render.forceMesh || SystemInfo.graphicsShaderLevel < 50)
                pct = new GameObject("PC").AddComponent<PointCloudMeshRenderer>();
            else
                pct = new GameObject("PC").AddComponent<PointCloudBufferRenderer>();
            pct.transform.parent = transform;
            pct.Init(pc);

        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            var pc = Config.Instance.Users[1];
            PointCloudBaseRenderer pct = null;
            if (pc.Render.forceMesh || SystemInfo.graphicsShaderLevel < 50)
                pct = new GameObject("PC").AddComponent<PointCloudMeshRenderer>();
            else
                pct = new GameObject("PC").AddComponent<PointCloudBufferRenderer>();
            pct.transform.parent = transform;
            pct.Init(pc);

        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            var pc = Config.Instance.Users[2];
            PointCloudBaseRenderer pct = null;
            if (pc.Render.forceMesh || SystemInfo.graphicsShaderLevel < 50)
                pct = new GameObject("PC").AddComponent<PointCloudMeshRenderer>();
            else
                pct = new GameObject("PC").AddComponent<PointCloudBufferRenderer>();
            pct.transform.parent = transform;
            pct.Init(pc);

        }

    }

}
