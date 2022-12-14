using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;
using System.Threading;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

public class SampleTwoUserTilingSessionController : SampleTwoUserSessionController
{
    [Tooltip("Orchestrator (default: from this gameObject)")]
    [SerializeField] protected SampleOrchestration orchestrator;
    [Tooltip("For compressed session: levels of octree depth to compress to")]
    [SerializeField] protected int[] octreeDepths = new int[] { 10 };
    [Header("Introspection")]
    [SerializeField] private StreamSupport.PointCloudNetworkTileDescription ourTileDescription;
    [SerializeField] private StreamSupport.PointCloudNetworkTileDescription theirTileDescription;

  
    private void Update()
    {
        if (otherInitialized) return;
        if (streamDescriptionReceived)
        {
            otherInitialized = true;
            InitializeOther();
        }
    }

    public void SessionStartCallback(StreamSupport.PointCloudNetworkTileDescription ntd)
    {
        theirTileDescription = ntd;
        streamDescriptionReceived = true;
    }

    /// <summary>
    /// Initialize the base class (host names), then create the server to and client to
    /// communicate control information with the other side.
    /// </summary>
    protected override void Initialize()
    {
        streamDescriptionReceived = false;
        base.Initialize();
        string senderUrl = $"tcp://{firstHost}:4300";
        string receiverUrl = $"tcp://{secondHost}:4300";
        if (orchestrator == null)
        {
            orchestrator = GetComponent<SampleOrchestration>();
        }
        orchestrator.Initialize(senderUrl, receiverUrl);
        orchestrator.RegisterCallback<StreamSupport.PointCloudNetworkTileDescription>("SessionStart", SessionStartCallback);
        selfPipeline.gameObject.SetActive(true);
    }

    /// <summary>
    /// Initialize our pointcloud pipeline.
    /// </summary>
    protected override void InitializeSelf()
    {
        PointCloudSelfPipelineTiled pipeline = selfPipeline.GetComponent<PointCloudSelfPipelineTiled>();
        AbstractPointCloudSink transmitter = pipeline?.transmitter;
        if (transmitter == null) Debug.LogError($"SampleTowUserSessionController: transmitter is null for {selfPipeline}");
        transmitter.sinkType = AbstractPointCloudSink.SinkType.TCP;
        transmitter.outputUrl = $"tcp://{firstHost}:4303";
        transmitter.compressedOutputStreams = useCompression;
        Debug.Log($"SampleTwoUserSessionController: initialized self: transmitter on {firstHost}");
        //
        // Send message to other side, describg our tiling and compression streams
        //
        PointCloudTileDescription[] tilesToTransmit = pipeline.getTiles();
        if (tilesToTransmit == null)
        {
            Debug.LogWarning($"SampleTwoUserSessionController: selfPipeline returned no PointCloudTileDescription");
            // If there is no tile information we assume a single tile.
            tilesToTransmit = new PointCloudTileDescription[1]
            {
                new PointCloudTileDescription()
                {
                    cameraMask=0,
                    cameraName="untiled",
                    normal=Vector3.zero
                }
            };
        }
     
        else if(tilesToTransmit.Length > 1)
        {
            // We assume tile 0 is the untiled representation and remove it.
            tilesToTransmit = tilesToTransmit[1..];
        }
        ourTileDescription = StreamSupport.CreateNetworkTileDescription(tilesToTransmit, octreeDepths);
        orchestrator.Send<StreamSupport.PointCloudNetworkTileDescription>("SessionStart", ourTileDescription);
      }

    /// <summary>
    /// Initialize the other pointcloud pipeline and enable it.
    /// </summary>
    protected override void InitializeOther()
    {

        PointCloudPipelineTiled receiver = otherPipeline.GetComponent<PointCloudPipelineTiled>();
        if (receiver == null) Debug.LogError($"SampleTowUserSessionController: receiver is null for {otherPipeline}");
        receiver.sourceType = PointCloudPipelineTiled.SourceType.TCP;
        receiver.inputUrl = $"tcp://{secondHost}:4303";
        receiver.compressedInputStream = useCompression;
        StreamSupport.IncomingTileDescription[] incomingTileDescription = StreamSupport.CreateIncomingTileDescription(theirTileDescription);
        receiver.SetTileDescription(incomingTileDescription);
        Debug.Log($"SampleTwoUserSessionController: initialized other: receiver for {secondHost}");
        otherPipeline.gameObject.SetActive(true);
    }

}
