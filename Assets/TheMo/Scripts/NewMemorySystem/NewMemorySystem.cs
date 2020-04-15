using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMemorySystem : MonoBehaviour
{
    Workers.BaseWorker reader;
    Workers.BaseWorker encoder;
    Workers.BaseWorker decoder;
//    Workers.BaseWorker writer;
    Workers.BaseWorker preparer;
    QueueThreadSafe preparerQueue = new QueueThreadSafe();
    QueueThreadSafe encoderQueue = new QueueThreadSafe();
    QueueThreadSafe decoderQueue = new QueueThreadSafe();
    MonoBehaviour render;
    // Start is called before the first frame update
    void Start() {
        Config._User cfg = Config.Instance.Users[0];

        preparer = new Workers.BufferPreparer(preparerQueue);
        render = gameObject.AddComponent<Workers.PointBufferRenderer>();
        ((Workers.PointBufferRenderer)render).preparer = (Workers.BufferPreparer)preparer;

        reader = new Workers.RS2Reader(cfg.PCSelfConfig, encoderQueue);
        encoder = new Workers.PCEncoder(cfg.PCSelfConfig.Encoder, encoderQueue, decoderQueue);
        decoder = new Workers.PCDecoder(decoderQueue, preparerQueue);

//        reader.AddNext(preparer).AddNext(reader); // <- local render tine.
//        reader.AddNext(encoder).AddNext(decoder).AddNext(preparer).AddNext(reader); // <- local render tine.

        reader.token = new Workers.Token();
    }

    void OnDestroy() {
        reader?.StopAndWait();
        encoder?.StopAndWait();
        decoder?.StopAndWait();
        preparer?.StopAndWait();
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
