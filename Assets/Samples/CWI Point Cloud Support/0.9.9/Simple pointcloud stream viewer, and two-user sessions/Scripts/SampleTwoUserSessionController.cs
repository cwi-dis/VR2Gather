using System.Collections;
using System.Net;
using UnityEngine;
using Cwipc;

/// <summary>
/// Very simple session controller for two-user pointcloud session.
/// If singleMachineSession is true we set both users to localhost (so you are essentially
/// seeing yourself twice: once directly as self-view and once via a network connection to localhost).
/// Otherwise the script checks which of the two given hostnames refers to this machine, and starts a
/// transmission server for sending out our point clouds on that IP address and a receiver for the
/// other IP address.
/// </summary>
public class SampleTwoUserSessionController : MonoBehaviour
{
    [Tooltip("Single machine session: use localhost for sender and receiver")]
    [SerializeField] protected bool singleMachineSession = true;
    [Header("For 2-machine sessions give both hostnames. Each instance will find its own.")]
    [Tooltip("Host name or IP address")]
    [SerializeField] protected string firstHost;
    [Tooltip("Host name or IP address")]
    [SerializeField] protected string secondHost;
    [Tooltip("Self: capturer, self-view, compressor, transmitter GameObject")]
    [SerializeField] protected GameObject selfPipeline;
    [Tooltip("Other:, receiver, decompressor, view GameObject")]
    [SerializeField] protected GameObject otherPipeline;
    [Tooltip("Whether to use compression in this session")]
    [SerializeField] protected bool useCompression = true;
    [Tooltip("Introspection: have we received the information for the second host?")]
    [SerializeField] protected bool streamDescriptionReceived = true;
    [Tooltip("Introspection: have we initialized the receiver for the other host?")]
    [SerializeField] protected bool otherInitialized = false;

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        InitializeSelf();
    }

    private void Update()
    {
        if (otherInitialized) return;
        if (streamDescriptionReceived)
        {
            InitializeOther();
            otherInitialized = true;
        }
    }

    /// <summary>
    /// Early initialization: fix up host names, and disable the other pointcloud pipeline.
    /// </summary>
    protected virtual void Initialize()
    {
        if (singleMachineSession)
        {
            firstHost = "localhost";
            secondHost = "localhost";
        }
        // See if we need to swap the hostnames (if we are second)
        IPHostEntry ourHostEntry = Dns.GetHostEntry(Dns.GetHostName());
        IPHostEntry secondHostEntry = Dns.GetHostEntry(secondHost);
        bool secondIsOurs = false;
        foreach(var ip1 in ourHostEntry.AddressList)
        {
            foreach(var ip2 in secondHostEntry.AddressList)
            {
                if (ip1.Equals(ip2))
                {
                    secondIsOurs = true;
                }
            }
        }
        if (secondIsOurs)
        {
            string swap = secondHost;
            secondHost = firstHost;
            firstHost = swap;
        }
        // Ensure other pipeline is not active yet (we need to set its tile information before its
        // Start() is called).
        if (selfPipeline.activeInHierarchy)
        {
            Debug.LogWarning("SampleTwoUserSessionController: selfPipeline is already active, this will lead to problems.");
        }
        // Ensure other pipeline is not active yet (we need to set its tile information before its
        // Start() is called).
        if (otherPipeline.activeInHierarchy)
        {
            Debug.LogWarning("SampleTwoUserSessionController: otherPipeline is already active, this will lead to problems.");
        }
    }

    /// <summary>
    /// Initialize our pointcloud pipeline.
    /// </summary>
    protected virtual void InitializeSelf()
    {
        PointCloudSelfPipelineSimple pipeline = selfPipeline.GetComponent<PointCloudSelfPipelineSimple>();
        AbstractPointCloudSink transmitter = pipeline?.transmitter;
        if (transmitter == null) Debug.LogError($"SampleTowUserSessionController: transmitter is null for {selfPipeline}");
        transmitter.sinkType = AbstractPointCloudSink.SinkType.TCP;
        transmitter.outputUrl = $"tcp://{firstHost}:4303";
        transmitter.compressedOutputStreams = useCompression;
        Debug.Log($"SampleTwoUserSessionController: initialized self: transmitter on {firstHost}");
        selfPipeline.gameObject.SetActive(true);

    }

    /// <summary>
    /// Initialize the other pointcloud pipeline and enable it.
    /// </summary>
    protected virtual void InitializeOther()
    {

        PointCloudPipelineSimple receiver = otherPipeline.GetComponent<PointCloudPipelineSimple>();
        if (receiver == null) Debug.LogError($"SampleTowUserSessionController: receiver is null for {otherPipeline}");
        receiver.sourceType = PointCloudPipelineSimple.SourceType.TCP;
        receiver.inputUrl = $"tcp://{secondHost}:4303";
        receiver.compressedInputStream = useCompression;
        Debug.Log($"SampleTwoUserSessionController: initialized other: receiver for {secondHost}");
        otherPipeline.gameObject.SetActive(true);
    }
}
