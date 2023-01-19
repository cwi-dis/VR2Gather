#define  TEST_LOCAL_PC
//#define TEST_PC
//#define TEST_VOICECHAT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.Voice;
using VRT.UserRepresentation.PointCloud;
using VRT.Core;
using Cwipc;
using PointCloudRenderer = Cwipc.PointCloudRenderer;
using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;
using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;
using EncoderStreamDescription = Cwipc.StreamSupport.EncoderStreamDescription;

public class TestPipeline : MonoBehaviour
{

    [Tooltip("Don't send pointclouds via Dash, just show locally")]
    public bool localPCs = false;
    [Tooltip("Filename for prerecorded pointclouds (synthetic used when empty)")]
    public string prerecordedPointclouds = "";
    [Tooltip("Compress and decompress even when showing locally")]
    public bool useCompression = true;
    [Tooltip("Do audio over dash")]
    public bool useDashVoice = false;
    [Tooltip("Enable to show pointclouds (otherwise only audio or video)")]
    public bool usePointClouds = false;
    [Tooltip("Enable for SocketIO, disable for Dash")]
    public bool useSocketIO = true;
    [Tooltip("Queues drop when full if enabled, otherwise they wait until there is room")]
    public bool dropQueuesWhenFull = true;
    [Tooltip("Pointcloud reader frames per second")]
    public float targetFPS = 20f;
    [Tooltip("Number of points if using synthetic reader")]
    public int numPoints = 1000;
    [Tooltip("Encoder parameter: octree_bits")]
    public int octree_bits = 10;
    [Tooltip("Encoder parameter: tile number to encode/decode, 0 for all")]
    public int tilenum;

    public NetController orchestrator;
    [Tooltip("Set if remoteURL is full url of stream created by another instance")]
    public bool useRemoteStream = false;
    [Tooltip("Partial URL of Dash stream (or full URL if useRemoteStream is set)")]
    public string remoteURL = "https://vrt-evanescent1.viaccess-orca.com";
    string URL = "";
    public string remoteStream = "";

    AsyncWorker reader;
    AsyncWorker[] encoder;
    [Tooltip("Use multiple decoders in parallel (untested)")]
    public int decoders = 1;
    [Tooltip("Use multiple encoders in parallel (untested)")]
    public int encoders = 1;
    AsyncWorker[] decoder;
    AsyncWorker pointcloudsWriter;
    AsyncWorker pointcloudsReader;

    [Tooltip("Debugging: preparer created")]
    public AsyncWorker preparer;
    QueueThreadSafe preparerQueue;
    QueueThreadSafe encoderQueue;
    QueueThreadSafe writerQueue;
    QueueThreadSafe decoderQueue;
    [Tooltip("Debugging: renderer created")]
    public MonoBehaviour render;

    // rtmp://127.0.0.1:1935/live/signals
    // Start is called before the first frame update
    void Start() {
        preparerQueue = new QueueThreadSafe("PreparerQueue", 10, dropQueuesWhenFull);
        encoderQueue = new QueueThreadSafe("EncoderQueue", 10, dropQueuesWhenFull);
        writerQueue = new QueueThreadSafe("WriterQueue", 10, dropQueuesWhenFull);
        decoderQueue = new QueueThreadSafe("DecoderQueue", 10, dropQueuesWhenFull);
        IncomingTileDescription[] tiles = new IncomingTileDescription[1] {
            new IncomingTileDescription() {
                outQueue = decoderQueue,
                tileNumber = 0
            }
        };

        OutgoingStreamDescription[] streams = new OutgoingStreamDescription[1] {
            new OutgoingStreamDescription(){
                tileNumber = 0,
                qualityIndex = 0,
                inQueue = writerQueue
            }
        };

        if (useSocketIO && orchestrator) {
            orchestrator.gameObject.SetActive(useSocketIO);
            orchestrator.OnConnectReady = () => {
                Debug.Log(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> OnConnectReady");
                string uuid = System.Guid.NewGuid().ToString();
                User user = OrchestratorController.Instance.SelfUser;
                gameObject.AddComponent<VoiceSender>().Init(user, "audio", 2000, 10000, VRTConfig.ProtocolType.SocketIO); //Audio Pipeline
                gameObject.AddComponent<VoiceReceiver>().Init(user, "audio", 0, VRTConfig.ProtocolType.SocketIO); //Audio Pipeline
                pointcloudsReader = new AsyncSocketIOReader(user, remoteStream, "cwi1", tiles);
                pointcloudsWriter = new AsyncSocketIOWriter(user, remoteStream, "cwi1", streams);

            };
        }

        VRTConfig config = VRTConfig.Instance;
        preparer = new AsyncPointCloudPreparer(preparerQueue);
        render = gameObject.AddComponent<PointCloudRenderer>();
        ((PointCloudRenderer)render).SetPreparer((AsyncPointCloudPreparer)preparer);

        if (usePointClouds) {
			if (localPCs) {
                var pcQueue = preparerQueue;
                if (useCompression) pcQueue = encoderQueue;
                if (prerecordedPointclouds != "")
                {
                    reader = new AsyncPrerecordedReader(prerecordedPointclouds, 0, targetFPS, pcQueue);
                }
                else
                {
                    reader = new AsyncSyntheticReader(targetFPS, numPoints, pcQueue);
                }
                if (useCompression) {
					EncoderStreamDescription[] encStreams = new EncoderStreamDescription[1];
					encStreams[0].octreeBits = octree_bits;
					encStreams[0].tileFilter = tilenum;
					encStreams[0].outQueue = writerQueue;
					encoder = new AsyncPCEncoder[encoders];
                    for (int i = 0; i < encoders; ++i)
                        encoder[i] = new AsyncPCEncoder(encoderQueue, encStreams);
                    decoder = new AsyncPCDecoder[decoders];
					for (int i = 0; i < decoders; ++i)
						decoder[i] = new AsyncPCDecoder(writerQueue, preparerQueue);
				}
			} else {
				if (!useRemoteStream) {
                    if (prerecordedPointclouds != "")
                    {
                        reader = new AsyncPrerecordedReader(prerecordedPointclouds, 0, targetFPS, encoderQueue);
                    }
                    else
                    {
                        reader = new AsyncSyntheticReader(targetFPS, numPoints, encoderQueue);
                    }
                    EncoderStreamDescription[] encStreams = new EncoderStreamDescription[1];
					encStreams[0].octreeBits = octree_bits;
					encStreams[0].tileFilter = tilenum;
					encStreams[0].outQueue = writerQueue;

                    encoder = new AsyncPCEncoder[encoders];
                    for (int i = 0; i < encoders; ++i)
                        encoder[i] = new AsyncPCEncoder(encoderQueue, encStreams);

                    string uuid = System.Guid.NewGuid().ToString();
					URL = $"{remoteURL}/{uuid}/pcc/";
					pointcloudsWriter = new AsyncB2DWriter(URL, remoteStream, "cwi1", 2000, 10000, streams);
				} 
				else
					URL = remoteURL;

				if (!useSocketIO)
					pointcloudsReader = new AsyncSubPCReader(URL, remoteStream, "cwi1", tiles);

				decoder = new AsyncPCDecoder[decoders];
				for (int i = 0; i < decoders; ++i)
					decoder[i] = new AsyncPCDecoder(decoderQueue, preparerQueue);
			}
        }
        

        // using Audio over dash
        if (useDashVoice && !useSocketIO) {
            string uuid = System.Guid.NewGuid().ToString();
            gameObject.AddComponent<VoiceSender>().Init(new User() { 
                sfuData = new SfuData() { 
                    url_audio = $"{remoteURL}/{uuid}/audio/" 
                } 
            }, "audio", 2000, 10000, VRTConfig.ProtocolType.Dash); //Audio Pipeline
            gameObject.AddComponent<VoiceReceiver>().Init(new User() { 
                sfuData = new SfuData() { 
                    url_audio = $"{remoteURL}/{uuid}/audio/" 
                } 
            }, "audio", 0, VRTConfig.ProtocolType.Dash); //Audio Pipeline
        }

    }

    void OnDestroy() {
        reader?.StopAndWait();
        pointcloudsWriter?.StopAndWait();
        pointcloudsReader?.StopAndWait();
        if (encoder != null) {
            for (int i = 0; i < encoders; ++i)
                encoder[i]?.StopAndWait();
        }
        if (decoder != null) {
            for (int i = 0; i < decoders; ++i)
                decoder[i]?.StopAndWait();
        }

        preparer?.StopAndWait();
        Debug.Log($"NewMemorySystem: Queues references counting: preparerQueue {preparerQueue._Count} encoderQueue {encoderQueue._Count} writerQueue {writerQueue._Count} decoderQueue {decoderQueue._Count}");
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
