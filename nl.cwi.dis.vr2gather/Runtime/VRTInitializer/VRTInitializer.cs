using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;
using VRT.UserRepresentation.PointCloud;
using VRT.UserRepresentation.WebCam;
using VRT.Transport.SocketIO;
using VRT.Transport.TCP;
using VRT.Transport.TCPReflector;
#if !VRT_WITHOUT_DASH
using VRT.Transport.Dash;
#endif
#if !VRT_WITHOUT_WEBRTC
using VRT.Transport.WebRTC;
#endif
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using Cwipc;
using VRT.Core;

public class VRTInitializer : MonoBehaviour
{

    
    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("VRTInitializer: Registering transport protocols");
        TransportProtocolSocketIO.Register();
        TransportProtocolTCPDirect.Register();
        TransportProtocolTCPReflector.Register();
#if !VRT_WITHOUT_DASH
        TransportProtocolDash.Register();
#endif
#if !VRT_WITHOUT_WEBRTC
        TransportProtocolWebRTC.Register();
#endif
        Debug.Log("VRTInitializer: Registering pipelines");
        PointCloudPipelineSelf.Register();
        PointCloudPipelineOther.Register();
        WebCamPipeline.Register();
    }

    private void OnApplicationQuit()
    {
#if VRT_WITH_STATS
        Statistics.Output("VRTInitializer", $"quitting=1");
#endif
#if xxxjack_removed
        // xxxjack the ShowTotalRefCount call may come too early, because the VoiceDashSender and VoiceDashReceiver seem to work asynchronously...
        BaseMemoryChunkReferences.ShowTotalRefCount();
#endif
    }

    private void Start()
    {
        Debug.Log("VRTInitializer: Start");
        if (VRTConfig.Instance.disableVR)
        {
            if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
            {
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            }
            Debug.Log("VRTInitializer: VR disabled");
            return;
        }
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.LogWarning("VRTInitializer: de-initialize XR on Mac to work around bug");
        }
        else
        {
            Debug.Log("VRTInitializer: XR was not enabled");
        }
#endif
    }

    // Update is called once per frame
    void Update()
    {
    }
}
