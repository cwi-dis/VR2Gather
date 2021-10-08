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

public class TestPipeline : MonoBehaviour
{


    public bool localPCs = false;
    public string prerecordedPointclouds = "";
    public bool useCompression = true;
    public bool useDashVoice = false;
    public bool usePointClouds = false;
    public bool useSocketIO = true;
    public bool dropQueuesWhenFull = true;
    public float targetFPS = 20f;
    public int numPoints = 1000;

    public NetController orchestrator;

    public bool useRemoteStream = false;
    public string remoteURL = "https://vrt-evanescent1.viaccess-orca.com";
    string URL = "";
    public string remoteStream = "";

    BaseWorker reader;
    BaseWorker[] encoder;
    public int decoders = 1;
    public int encoders = 1;
    BaseWorker[] decoder;
    BaseWorker pointcloudsWriter;
    BaseWorker pointcloudsReader;

    public BaseWorker preparer;
    QueueThreadSafe preparerQueue;
    QueueThreadSafe encoderQueue;
    QueueThreadSafe writerQueue;
    QueueThreadSafe decoderQueue;
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

        B2DWriter.DashStreamDescription[] streams = new B2DWriter.DashStreamDescription[1] {
            new B2DWriter.DashStreamDescription(){
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
                gameObject.AddComponent<VoiceReceiver>().Init(user, "audio", 0, 1, Config.ProtocolType.SocketIO); //Audio Pipeline
                pointcloudsReader = new SocketIOReader(user, remoteStream, tiles);
                pointcloudsWriter = new SocketIOWriter(user, remoteStream, streams);

            };
        }

        Config config = Config.Instance;
        preparer = new BufferPreparer(preparerQueue);
        render = gameObject.AddComponent<PointBufferRenderer>();
        ((PointBufferRenderer)render).SetPreparer((BufferPreparer)preparer);

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
					encStreams[0].octreeBits = 10;
					encStreams[0].tileNumber = 0;
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
					reader = new PCReader(targetFPS, numPoints, encoderQueue);
					PCEncoder.EncoderStreamDescription[] encStreams = new PCEncoder.EncoderStreamDescription[1];
					encStreams[0].octreeBits = 10;
					encStreams[0].tileNumber = 0;
					encStreams[0].outQueue = writerQueue;

                    encoder = new PCEncoder[encoders];
                    for (int i = 0; i < encoders; ++i)
                        encoder[i] = new PCEncoder(encoderQueue, encStreams);

                    string uuid = System.Guid.NewGuid().ToString();
					URL = $"{remoteURL}/{uuid}/pcc/";
					pointcloudsWriter = new B2DWriter(URL, remoteStream, "cwi1", 2000, 10000, streams);
				} 
				else
					URL = remoteURL;

				if (!useSocketIO)
					pointcloudsReader = new PCSubReader(URL, remoteStream, 1, tiles);

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
            }, "audio", 0, 1, Config.ProtocolType.Dash); //Audio Pipeline
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
