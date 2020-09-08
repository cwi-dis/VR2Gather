#define  TEST_LOCAL_PC
//#define TEST_PC
//#define TEST_VOICECHAT

using OrchestratorWrapping;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMemorySystem : MonoBehaviour
{

    public bool         forceMesh = false;
    public bool         localPCs = false;
    public bool         useCompression = true;
    public bool         useVoice = false;

    public bool         useRemoteStream = false;
    public string       remoteURL = "";
    public string       remoteStream="";

    Workers.BaseWorker  reader;
    Workers.BaseWorker  encoder;
    public int          decoders = 1;
    Workers.BaseWorker[] decoder;
    Workers.BaseWorker  dashWriter;
    Workers.BaseWorker  dashReader;

    Workers.BaseWorker  preparer;
    QueueThreadSafe     preparerQueue = new QueueThreadSafe();
    QueueThreadSafe     encoderQueue = new QueueThreadSafe();
    QueueThreadSafe     writerQueue = new QueueThreadSafe();
    QueueThreadSafe     decoderQueue = new QueueThreadSafe(2, true);
    MonoBehaviour       render;

    // rtmp://127.0.0.1:1935/live/signals
    // Start is called before the first frame update
    void Start() {
        /*
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
                Workers.B2DWriter.DashStreamDescription[] b2dStreams = new Workers.B2DWriter.DashStreamDescription[1];
                b2dStreams[0].tileNumber = 0;
                b2dStreams[0].quality = 0;
                b2dStreams[0].inQueue = writerQueue;
                remoteURL = $"https://vrt-evanescent1.viaccess-orca.com/{uuid}/pcc/";
                remoteStream = "pointclouds";
                dashWriter = new Workers.B2DWriter(remoteURL, remoteStream, "cwi1", 2000, 10000, b2dStreams);
            }
            Workers.PCSubReader.TileDescriptor[] tiles = new Workers.PCSubReader.TileDescriptor[1]
            {
                    new Workers.PCSubReader.TileDescriptor() {
                        outQueue = decoderQueue,
                        tileNumber = 0
                    }
            };
            dashReader = new Workers.PCSubReader(remoteURL, remoteStream, 1, tiles);
            decoder = new Workers.PCDecoder[decoders];
            for (int i = 0; i < decoders; ++i)
                decoder[i] = new Workers.PCDecoder(decoderQueue, preparerQueue);
        }
        */

        // using Audio over dash
        if (useVoice) {
            useVoice = false;
            string uuid = System.Guid.NewGuid().ToString();
            gameObject.AddComponent<VoiceSender>().Init(new OrchestratorWrapping.User() { sfuData = new SfuData() { url_audio = $"https://vrt-evanescent1.viaccess-orca.com/{uuid}/audio/" } }, "audio", 2000, 10000, true); //Audio Pipeline
            gameObject.AddComponent<VoiceReceiver>().Init(new OrchestratorWrapping.User() { sfuData = new SfuData() { url_audio = $"https://vrt-evanescent1.viaccess-orca.com/{uuid}/audio/" } }, "audio", 0, 1, true); //Audio Pipeline
        }

    }

    private void Update() {
/*
        if(useVoice && Input.GetKeyDown(KeyCode.Space)){//&& OrchestratorController.Instance.UserIsLogged) {
            useVoice = false;
            string uuid = System.Guid.NewGuid().ToString();
            gameObject.AddComponent<VoiceSender>().Init(new OrchestratorWrapping.User() { sfuData = new SfuData() { url_audio= $"https://vrt-evanescent1.viaccess-orca.com/{uuid}/audio/" } }, "audio", 2000, 10000, true); //Audio Pipeline
            gameObject.AddComponent<VoiceReceiver>().Init(new OrchestratorWrapping.User() { sfuData = new SfuData() { url_audio = $"https://vrt-evanescent1.viaccess-orca.com/{uuid}/audio/" } }, "audio", 0, 1, true); //Audio Pipeline

        }
*/
    }

    void OnDestroy() {
        reader?.StopAndWait();
        encoder?.StopAndWait();
        dashWriter?.StopAndWait();
        dashReader?.StopAndWait();
        if (decoder != null) {
            for (int i = 0; i < decoders; ++i)
                decoder[i]?.StopAndWait();
        }

        preparer?.StopAndWait();
        Debug.Log($"NewMemorySystem: Queues references counting: preparerQueue {preparerQueue._Count} encoderQueue {encoderQueue._Count} writerQueue {writerQueue._Count} decoderQueue {decoderQueue._Count}");
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
