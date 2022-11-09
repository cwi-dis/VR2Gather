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
using PointCloudRenderer = VRT.UserRepresentation.PointCloud.PointCloudRenderer;

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
        PCSubReader.TileDescriptor[] tiles = new PCSubReader.TileDescriptor[1] {
            new PCSubReader.TileDescriptor() {
                outQueue = decoderQueue,
                tileNumber = 0
            }
        };

        AsyncB2DWriter.DashStreamDescription[] streams = new AsyncB2DWriter.DashStreamDescription[1] {
            new AsyncB2DWriter.DashStreamDescription(){
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
                gameObject.AddComponent<VoiceSender>().Init(user, "audio", 2000, 10000, Config.ProtocolType.SocketIO); //Audio Pipeline
                gameObject.AddComponent<VoiceReceiver>().Init(user, "audio", 0, Config.ProtocolType.SocketIO); //Audio Pipeline
                pointcloudsReader = new SocketIOReader(user, remoteStream, "cwi1", tiles);
                pointcloudsWriter = new AsyncSocketIOWriter(user, remoteStream, "cwi1", streams);

            };
        }

        Config config = Config.Instance;
        preparer = new PointCloudPreparer(preparerQueue);
        render = gameObject.AddComponent<PointCloudRenderer>();
        ((PointCloudRenderer)render).SetPreparer((PointCloudPreparer)preparer);

        if (usePointClouds) {
			if (localPCs) {
                var pcQueue = preparerQueue;
                if (useCompression) pcQueue = encoderQueue;
                if (prerecordedPointclouds != "")
                {
                    reader = new PrerecordedLiveReader(prerecordedPointclouds, 0, targetFPS, pcQueue);
                }
                else
                {
                    reader = new PCReader(targetFPS, numPoints, pcQueue);
                }
                if (useCompression) {
					PCEncoder.EncoderStreamDescription[] encStreams = new PCEncoder.EncoderStreamDescription[1];
					encStreams[0].octreeBits = octree_bits;
					encStreams[0].tileNumber = tilenum;
					encStreams[0].outQueue = writerQueue;
					encoder = new PCEncoder[encoders];
                    for (int i = 0; i < encoders; ++i)
                        encoder[i] = new PCEncoder(encoderQueue, encStreams);
                    decoder = new PCDecoder[decoders];
					for (int i = 0; i < decoders; ++i)
						decoder[i] = new PCDecoder(writerQueue, preparerQueue);
				}
			} else {
				if (!useRemoteStream) {
                    if (prerecordedPointclouds != "")
                    {
                        reader = new PrerecordedLiveReader(prerecordedPointclouds, 0, targetFPS, encoderQueue);
                    }
                    else
                    {
                        reader = new PCReader(targetFPS, numPoints, encoderQueue);
                    }
                    PCEncoder.EncoderStreamDescription[] encStreams = new PCEncoder.EncoderStreamDescription[1];
					encStreams[0].octreeBits = octree_bits;
					encStreams[0].tileNumber = tilenum;
					encStreams[0].outQueue = writerQueue;

                    encoder = new PCEncoder[encoders];
                    for (int i = 0; i < encoders; ++i)
                        encoder[i] = new PCEncoder(encoderQueue, encStreams);

                    string uuid = System.Guid.NewGuid().ToString();
					URL = $"{remoteURL}/{uuid}/pcc/";
					pointcloudsWriter = new AsyncB2DWriter(URL, remoteStream, "cwi1", 2000, 10000, streams);
				} 
				else
					URL = remoteURL;

				if (!useSocketIO)
					pointcloudsReader = new PCSubReader(URL, remoteStream, "cwi1", tiles);

				decoder = new PCDecoder[decoders];
				for (int i = 0; i < decoders; ++i)
					decoder[i] = new PCDecoder(decoderQueue, preparerQueue);
			}
        }
        

        // using Audio over dash
        if (useDashVoice && !useSocketIO) {
            string uuid = System.Guid.NewGuid().ToString();
            gameObject.AddComponent<VoiceSender>().Init(new User() { 
                sfuData = new SfuData() { 
                    url_audio = $"{remoteURL}/{uuid}/audio/" 
                } 
            }, "audio", 2000, 10000, Config.ProtocolType.Dash); //Audio Pipeline
            gameObject.AddComponent<VoiceReceiver>().Init(new User() { 
                sfuData = new SfuData() { 
                    url_audio = $"{remoteURL}/{uuid}/audio/" 
                } 
            }, "audio", 0, Config.ProtocolType.Dash); //Audio Pipeline
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
