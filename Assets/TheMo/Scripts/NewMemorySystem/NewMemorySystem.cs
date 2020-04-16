using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMemorySystem : MonoBehaviour
{
    Workers.BaseWorker reader;
    Workers.BaseWorker encoder;
    Workers.BaseWorker decoder;
    Workers.BaseWorker dashWriter;
    Workers.BaseWorker dashReader;

    Workers.BaseWorker preparer;
    QueueThreadSafe preparerQueue = new QueueThreadSafe();
    QueueThreadSafe encoderQueue = new QueueThreadSafe();
    QueueThreadSafe writerQueue = new QueueThreadSafe();
    QueueThreadSafe decoderQueue = new QueueThreadSafe();
    MonoBehaviour render;
    // Start is called before the first frame update
    void Start() {
        Config._User cfg = Config.Instance.Users[0];

        preparer = new Workers.BufferPreparer(preparerQueue);
        render = gameObject.AddComponent<Workers.PointBufferRenderer>();
        ((Workers.PointBufferRenderer)render).preparer = (Workers.BufferPreparer)preparer;

        reader = new Workers.RS2Reader(cfg.PCSelfConfig, encoderQueue);

        encoder = new Workers.PCEncoder(cfg.PCSelfConfig.Encoder, encoderQueue, writerQueue);

        string uuid = System.Guid.NewGuid().ToString();
        dashWriter = new Workers.B2DWriter(cfg.PCSelfConfig.Bin2Dash, "https://vrt-evanescent.viaccess-orca.com/"+uuid+"/", writerQueue);

        dashReader = new Workers.SUBReader(cfg.SUBConfig, "https://vrt-evanescent.viaccess-orca.com/"+ uuid + "/testBed.mpd", decoderQueue);

        decoder = new Workers.PCDecoder(decoderQueue, preparerQueue);
    }

    void OnDestroy() {
        dashWriter?.StopAndWait();
        dashReader?.StopAndWait();
        reader?.StopAndWait();
        encoder?.StopAndWait();
        decoder?.StopAndWait();
        preparer?.StopAndWait();
        Debug.Log($"NewMemorySystem: Queues references counting: preparerQueue {preparerQueue.Count} encoderQueue {encoderQueue.Count} writerQueue {writerQueue.Count} decoderQueue {decoderQueue.Count}");
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
