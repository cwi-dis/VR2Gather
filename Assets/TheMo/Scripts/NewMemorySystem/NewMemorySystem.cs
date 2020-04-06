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
    MonoBehaviour render;
    // Start is called before the first frame update
    void Start() {
        Config._User cfg = Config.Instance.Users[0];

        preparer = new Workers.BufferPreparer();
        render = gameObject.AddComponent<Workers.PointBufferRenderer>();
        ((Workers.PointBufferRenderer)render).preparer = (Workers.BufferPreparer)preparer;

        reader = new Workers.RS2Reader(cfg.PCSelfConfig);
        encoder = new Workers.PCEncoder(cfg.PCSelfConfig.Encoder);
        decoder = new Workers.PCDecoder();

//        reader.AddNext(preparer).AddNext(reader); // <- local render tine.
        reader.AddNext(encoder).AddNext(decoder).AddNext(preparer).AddNext(reader); // <- local render tine.

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
