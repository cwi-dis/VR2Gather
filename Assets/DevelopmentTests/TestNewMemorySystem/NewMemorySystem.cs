#define  TEST_LOCAL_PC
//#define TEST_PC
//#define TEST_VOICECHAT

using VRTDash;
using SocketIO;
using Orchestrator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voice;
using VRTCore;

public class NewMemorySystem : MonoBehaviour
{


    public bool forceMesh = false;
    public bool localPCs = false;
    public bool useCompression = true;
    public bool useDashVoice = false;
    public bool useSocketIO = true;

    public NetController orchestrator;

    public bool useRemoteStream = false;
    public string remoteURL = "https://vrt-evanescent1.viaccess-orca.com";
    string URL = "";
    public string remoteStream = "";

    BaseWorker reader;
    BaseWorker encoder;
    public int decoders = 1;
    BaseWorker[] decoder;
    BaseWorker pointcloudsWriter;
    BaseWorker pointcloudsReader;

    BaseWorker preparer;
    QueueThreadSafe preparerQueue = new QueueThreadSafe("NewMemorySystemPreparer");
    QueueThreadSafe encoderQueue = new QueueThreadSafe("NewMemorySystemEncoder");
    QueueThreadSafe writerQueue = new QueueThreadSafe("NewMemorySystemWriter");
    QueueThreadSafe decoderQueue = new QueueThreadSafe("NewMemorySystemDecoder", 2, true);
    MonoBehaviour render;

    // rtmp://127.0.0.1:1935/live/signals
    // Start is called before the first frame update
    void Start() {
        PCSubReader.TileDescriptor[] tiles = new PCSubReader.TileDescriptor[1] {
            new PCSubReader.TileDescriptor() {
                outQueue = decoderQueue,
                tileNumber = 0
            }
        };

        B2DWriter.DashStreamDescription[] streams = new B2DWriter.DashStreamDescription[1] {
            new B2DWriter.DashStreamDescription(){
                tileNumber = 0,
                quality = 0,
                inQueue = writerQueue
            }
        };

        if (useSocketIO && orchestrator) {
            orchestrator.gameObject.SetActive(useSocketIO);
            orchestrator.OnConnectReady = () => {
                Debug.Log(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> OnConnectReady");
                string uuid = System.Guid.NewGuid().ToString();
                User user = OrchestratorController.Instance.SelfUser;
                gameObject.AddComponent<VoiceSender>().Init(user, "audio", 2000, 10000, false); //Audio Pipeline
                gameObject.AddComponent<VoiceReceiver>().Init(user, "audio", 0, 1, false); //Audio Pipeline

                pointcloudsReader = new SocketIOReader(user, remoteStream, tiles);
                pointcloudsWriter = new SocketIOWriter(user, remoteStream, streams);

            };
        }

        Config config = Config.Instance;
        if (forceMesh) {
            preparer = new Workers.MeshPreparer(preparerQueue);
            render = gameObject.AddComponent<Workers.PointMeshRenderer>();
            ((Workers.PointMeshRenderer)render).SetPreparer((Workers.MeshPreparer)preparer);
        } else {
            preparer = new Workers.BufferPreparer(preparerQueue);
            render = gameObject.AddComponent<Workers.PointBufferRenderer>();
            ((Workers.PointBufferRenderer)render).SetPreparer((Workers.BufferPreparer)preparer);
        }
        
        if (localPCs) {
            if (!useCompression)
                reader = new Workers.RS2Reader(20f, 1000, preparerQueue);
            else {
                reader = new Workers.RS2Reader(20f, 1000, encoderQueue);
                Workers.PCEncoder.EncoderStreamDescription[] encStreams = new Workers.PCEncoder.EncoderStreamDescription[1];
                encStreams[0].octreeBits = 10;
                encStreams[0].tileNumber = 0;
                encStreams[0].outQueue = writerQueue;
                encoder = new Workers.PCEncoder(encoderQueue, encStreams);
                decoder = new Workers.PCDecoder[decoders];
                for (int i = 0; i < decoders; ++i)
                    decoder[i] = new Workers.PCDecoder(writerQueue, preparerQueue);
            }
        } else {
            if (!useRemoteStream) {
                reader = new Workers.RS2Reader(20f, 1000, encoderQueue);
                Workers.PCEncoder.EncoderStreamDescription[] encStreams = new Workers.PCEncoder.EncoderStreamDescription[1];
                encStreams[0].octreeBits = 10;
                encStreams[0].tileNumber = 0;
                encStreams[0].outQueue = writerQueue;
                encoder = new Workers.PCEncoder(encoderQueue, encStreams);
                string uuid = System.Guid.NewGuid().ToString();
                URL = $"{remoteURL}/{uuid}/pcc/";
                pointcloudsWriter = new VRTDash.B2DWriter(URL, remoteStream, "cwi1", 2000, 10000, streams);
            } 
            else
                URL = remoteURL;

            if (!useSocketIO)
                pointcloudsReader = new PCSubReader(URL, remoteStream, 1, tiles);

            decoder = new Workers.PCDecoder[decoders];
            for (int i = 0; i < decoders; ++i)
                decoder[i] = new Workers.PCDecoder(decoderQueue, preparerQueue);
        }
        

        // using Audio over dash
        if (useDashVoice && !useSocketIO) {
            string uuid = System.Guid.NewGuid().ToString();
            gameObject.AddComponent<VoiceSender>().Init(new Orchestrator.User() { 
                sfuData = new SfuData() { 
                    url_audio = $"{remoteURL}/{uuid}/audio/" 
                } 
            }, "audio", 2000, 10000, true); //Audio Pipeline
            gameObject.AddComponent<VoiceReceiver>().Init(new Orchestrator.User() { 
                sfuData = new SfuData() { 
                    url_audio = $"{remoteURL}/{uuid}/audio/" 
                } 
            }, "audio", 0, 1, true); //Audio Pipeline
        }

    }

    void OnDestroy() {
        reader?.StopAndWait();
        encoder?.StopAndWait();
        pointcloudsWriter?.StopAndWait();
        pointcloudsReader?.StopAndWait();
        if (decoder != null) {
            for (int i = 0; i < decoders; ++i)
                decoder[i]?.StopAndWait();
        }

        preparer?.StopAndWait();
        Debug.Log($"NewMemorySystem: Queues references counting: preparerQueue {preparerQueue._Count} encoderQueue {encoderQueue._Count} writerQueue {writerQueue._Count} decoderQueue {decoderQueue._Count}");
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
