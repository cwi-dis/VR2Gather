using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.UserRepresentation.PointCloud;
using VRT.UserRepresentation.WebCam;
using VRT.Core;
using VRT.Statistics;
using Cwipc;

public class VRTInitializer : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("Initializer: Registering pipelines");
        PointCloudPipeline.Register();
        WebCamPipeline.Register();
        _ = Config.Instance;
    }

    private void OnApplicationQuit()
    {
#if VRT_WITH_STATS
        BaseStats.Output("PilotController", $"quitting=1");
#endif
        // xxxjack the ShowTotalRefCount call may come too early, because the VoiceDashSender and VoiceDashReceiver seem to work asynchronously...
        BaseMemoryChunkReferences.ShowTotalRefCount();
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
