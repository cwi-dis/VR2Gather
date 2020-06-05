#define  TEST_LOCAL_PC
//#define TEST_PC
//#define TEST_VOICECHAT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMemorySystem : MonoBehaviour
{

    public bool         forceMesh = false;
    public bool         localPCs = false;
    public bool         useCompression = true;
    public bool         useVoice = false;
    Workers.BaseWorker  reader;
    Workers.BaseWorker  encoder;
    Workers.BaseWorker  decoder;
    Workers.BaseWorker  dashWriter;
    Workers.BaseWorker  dashReader;

    //Workers.BaseWorker  binWriter;
    //Workers.BaseWorker  binReader;

    Workers.BaseWorker  preparer;
    QueueThreadSafe     preparerQueue = new QueueThreadSafe();
    QueueThreadSafe     encoderQueue = new QueueThreadSafe();
    QueueThreadSafe     writerQueue = new QueueThreadSafe();
    QueueThreadSafe     decoderQueue = new QueueThreadSafe(2, true);
    MonoBehaviour       render;

    // rtmp://127.0.0.1:1935/live/signals
    // Start is called before the first frame update
    void Start() {
        Config config = Config.Instance;
        if (forceMesh) {
            preparer = new Workers.MeshPreparer(preparerQueue);
            render = gameObject.AddComponent<Workers.PointMeshRenderer>();
            ((Workers.PointMeshRenderer)render).preparer = (Workers.MeshPreparer)preparer;
        } else {
            preparer = new Workers.BufferPreparer(preparerQueue);
            render = gameObject.AddComponent<Workers.PointBufferRenderer>();
            ((Workers.PointBufferRenderer)render).preparer = (Workers.BufferPreparer)preparer;
        }

        if (localPCs) {
            if(!useCompression)
                reader = new Workers.RS2Reader("../cameraconfig.xml", 0.01f, preparerQueue);
            else {
                reader = new Workers.RS2Reader("../cameraconfig.xml", 0.01f, encoderQueue);
                Workers.PCEncoder.EncoderStreamDescription[] encStreams = new Workers.PCEncoder.EncoderStreamDescription[1];
                encStreams[0].octreeBits = 10;
                encStreams[0].tileNumber = 0;
                encStreams[0].outQueue = writerQueue;
                encoder = new Workers.PCEncoder(encoderQueue, encStreams);
                decoder = new Workers.PCDecoder(writerQueue, preparerQueue);
            }
        } else {
            reader = new Workers.RS2Reader("../cameraconfig.xml", 0.01f, encoderQueue);
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
            dashWriter = new Workers.B2DWriter("https://vrt-evanescent.viaccess-orca.com/" + uuid + "/pcc/", "pointclouds", "cwi1", 2000, 10000, b2dStreams);
            dashReader = new Workers.PCSubReader("https://vrt-evanescent.viaccess-orca.com/" + uuid + "/pcc/", "pointclouds", 0, 1, decoderQueue);
            decoder = new Workers.PCDecoder(decoderQueue, preparerQueue);
        }

        if (useVoice) {
            string uuid = System.Guid.NewGuid().ToString();
            gameObject.AddComponent<VoiceDashSender>().Init("https://vrt-evanescent.viaccess-orca.com/" + uuid + "/", "audio", 2000, 10000); //Audio Pipeline
            gameObject.AddComponent<VoiceDashReceiver>().Init("https://vrt-evanescent.viaccess-orca.com/" + uuid + "/", "audio", 0, 1); //Audio Pipeline

        }
    }

    void OnDestroy() {

        reader?.StopAndWait();
        encoder?.StopAndWait();
        dashWriter?.StopAndWait();
        dashReader?.StopAndWait();
        //binWriter?.StopAndWait();
        //binReader?.StopAndWait();
        decoder?.StopAndWait();
        preparer?.StopAndWait();
        Debug.Log($"NewMemorySystem: Queues references counting: preparerQueue {preparerQueue._Count} encoderQueue {encoderQueue._Count} writerQueue {writerQueue._Count} decoderQueue {decoderQueue._Count}");
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
