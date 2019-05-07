using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudsMainController : MonoBehaviour
{
    public Shader pointShader = null;
    public Shader pointShader40 = null;

    // Start is called before the first frame update
    void Start()
    {
        foreach( var pc in Config.Instance.PCs) {
            PointCloudRenderer pct = new GameObject("PC").AddComponent<PointCloudRenderer>();
            pct.transform.parent = transform;
            pct.Init(pc, pc.forceMesh|| SystemInfo.graphicsShaderLevel < 50? pointShader40:pointShader );
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
