using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudsMainController : MonoBehaviour {
    // Start is called before the first frame update
    void Start() {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            var pc = Config.Instance.PCs[0];
            PointCloudBaseRenderer pct = null;
            if (pc.forceMesh || SystemInfo.graphicsShaderLevel < 50)
                pct = new GameObject("PC").AddComponent<PointCloudMeshRenderer>();
            else
                pct = new GameObject("PC").AddComponent<PointCloudBufferRenderer>();
            pct.transform.parent = transform;
            pct.Init(pc);

        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            var pc = Config.Instance.PCs[1];
            PointCloudBaseRenderer pct = null;
            if (pc.forceMesh || SystemInfo.graphicsShaderLevel < 50)
                pct = new GameObject("PC").AddComponent<PointCloudMeshRenderer>();
            else
                pct = new GameObject("PC").AddComponent<PointCloudBufferRenderer>();
            pct.transform.parent = transform;
            pct.Init(pc);

        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            var pc = Config.Instance.PCs[2];
            PointCloudBaseRenderer pct = null;
            if (pc.forceMesh || SystemInfo.graphicsShaderLevel < 50)
                pct = new GameObject("PC").AddComponent<PointCloudMeshRenderer>();
            else
                pct = new GameObject("PC").AddComponent<PointCloudBufferRenderer>();
            pct.transform.parent = transform;
            pct.Init(pc);

        }

    }

}
