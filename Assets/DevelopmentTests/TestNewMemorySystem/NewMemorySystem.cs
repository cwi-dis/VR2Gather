#define  TEST_LOCAL_PC
//#define TEST_PC
//#define TEST_VOICECHAT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMemorySystem : MonoBehaviour
{

    public bool         forceMesh = true;
    public bool         localPCs = false;
    public bool         useCompression = true;
    public bool         useVoice = false;
    Workers.BaseWorker  reader;
    Workers.BaseWorker  encoder;
    Workers.BaseWorker  decoder;
    Workers.BaseWorker  dashWriter;
    Workers.BaseWorker  dashReader;

    Workers.BaseWorker  binWriter;
    Workers.BaseWorker  binReader;


    Workers.BaseWorker  preparer;
    QueueThreadSafe     preparerQueue = new QueueThreadSafe();
    QueueThreadSafe     encoderQueue = new QueueThreadSafe();
    QueueThreadSafe     writerQueue = new QueueThreadSafe();
    QueueThreadSafe     decoderQueue = new QueueThreadSafe();
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
                encoder = new Workers.PCEncoder(10, encoderQueue, writerQueue);
                decoder = new Workers.PCDecoder(writerQueue, preparerQueue);
            }
        } else {
            reader = new Workers.RS2Reader("../cameraconfig.xml", 0.01f, encoderQueue);
            encoder = new Workers.PCEncoder(10, encoderQueue, writerQueue);
            string uuid = System.Guid.NewGuid().ToString();
            dashWriter = new Workers.B2DWriter("https://vrt-evanescent.viaccess-orca.com/" + uuid + "/", "pointclouds", 2000, 10000, writerQueue);
            dashReader = new Workers.PCSubReader("https://vrt-evanescent.viaccess-orca.com/" + uuid + "/", "pointclouds", 0, 1, decoderQueue, true);
            decoder = new Workers.PCDecoder(decoderQueue, preparerQueue);
        }

        if (useVoice) {
            string uuid = System.Guid.NewGuid().ToString();
            gameObject.AddComponent<VoiceDashSender>().Init("https://vrt-evanescent.viaccess-orca.com/" + uuid + "/", "audio", 2000, 10000); //Audio Pipeline
            gameObject.AddComponent<VoiceDashReceiver>().Init("https://vrt-evanescent.viaccess-orca.com/" + uuid + "/", "audio", 0, 1); //Audio Pipeline

        }
    }

    void OnDestroy() {
        dashWriter?.StopAndWait();
        dashReader?.StopAndWait();
        binWriter?.StopAndWait();
        binReader?.StopAndWait();

        reader?.StopAndWait();
        encoder?.StopAndWait();
        decoder?.StopAndWait();
        preparer?.StopAndWait();
        Debug.Log($"NewMemorySystem: Queues references counting: preparerQueue {preparerQueue.Count} encoderQueue {encoderQueue.Count} writerQueue {writerQueue.Count} decoderQueue {decoderQueue.Count}");
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
