using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.UserRepresentation.PointCloud;
using VRT.UserRepresentation.WebCam;

public class VRTInitializer : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("Initializer: Registering pipelines");
        PointCloudPipeline.Register();
        WebCamPipeline.Register();
    }

    private void Start()
    {
        Debug.Log("Initializer: Start");
    }
    // Update is called once per frame
    void Update()
    {
    }
}
