#define NO_VOICE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPipeline : MonoBehaviour {
    Workers.BaseWorker  reader;
    Workers.BaseWorker  codec;
    Workers.BaseWorker  writer;
    Workers.BaseWorker  preparer;
    MonoBehaviour       render;

    QueueThreadSafe preparerQueue = new QueueThreadSafe();
    QueueThreadSafe codecQueue = new QueueThreadSafe();
    QueueThreadSafe writerQueue = new QueueThreadSafe();


    /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
    /// <param name="cfg"> Config file json </param>
    /// <param name="parent"> Transform parent where attach it </param>
    /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
    /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
    public EntityPipeline Init(Config._User cfg, Transform parent, string url_pcc = "", string url_audio = "") {
        var temp = Config.Instance;
        Config._PCs configTransform = temp.PCs;
        cfg.Render.forceMesh = true;
        if (cfg.Render.forceMesh || SystemInfo.graphicsShaderLevel < 50) { // Mesh
            preparer = new Workers.MeshPreparer(preparerQueue);
            render = gameObject.AddComponent<Workers.PointMeshRenderer>();
            ((Workers.PointMeshRenderer)render).preparer = (Workers.MeshPreparer)preparer;
        }
        else { // Buffer
            preparer = new Workers.BufferPreparer(preparerQueue);
            render = gameObject.AddComponent<Workers.PointBufferRenderer>();
            ((Workers.PointBufferRenderer)render).preparer = (Workers.BufferPreparer)preparer;
        }

        int forks = 1;
        switch (cfg.sourceType) {
            case "pcself": // old "rs2"
                reader = new Workers.RS2Reader(cfg.PCSelfConfig, preparerQueue, codecQueue);
                //reader.AddNext(preparer).AddNext(reader); // <- local render tine.
                try {
                    codec = new Workers.PCEncoder(cfg.PCSelfConfig.Encoder, codecQueue, writerQueue);
                }
                catch (System.EntryPointNotFoundException) {
                    Debug.LogError("EntityPipeline: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                }
                try {
                    writer = new Workers.B2DWriter(cfg.PCSelfConfig.Bin2Dash, url_pcc, writerQueue);
                }
                catch (System.EntryPointNotFoundException e) {
                    Debug.LogError($"EntityPipeline: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                }
                if (codec != null && writer != null) {
                    reader.AddNext(codec).AddNext(writer).AddNext(reader); // <- encoder and bin2dash tine.
                    forks = 2;
                }
                /*
                if (temp.useAudio) {
                    try {
                        gameObject.AddComponent<VoiceDashSender>().Init(cfg.PCSelfConfig.AudioBin2Dash, url_audio); //Audio Pipeline
                    }
                    catch (System.EntryPointNotFoundException e) {
                        Debug.LogError("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                    }
                }
                */
                break;
            case "pcsub":
                reader = new Workers.SUBReader(cfg.SUBConfig, url_pcc, codecQueue); // TODO: Fix new Queue mode.
                codec = new Workers.PCDecoder(codecQueue, preparerQueue);
                if (temp.useAudio) {
                    gameObject.AddComponent<VoiceDashReceiver>().Init(cfg.AudioSUBConfig, url_audio); //Audio Pipeline
                }
                break;
            case "net":
                reader = new Workers.NetReader(cfg.NetConfig);
                codec = new Workers.PCDecoder(codecQueue,null);// TODO: Fix new Queue mode.
                reader.AddNext(codec).AddNext(preparer).AddNext(reader);
                break;
        }        

        //transform.parent = parent;
        //if (url_pcc == string.Empty || url_audio == string.Empty) {
        //    transform.position = new Vector3(cfg.Render.position.x, cfg.Render.position.y, cfg.Render.position.z);
        //    transform.rotation = Quaternion.Euler(cfg.Render.rotation);
        //}

        //Position depending on config calibration
        transform.localPosition = configTransform.offsetPosition;
        transform.rotation = Quaternion.Euler(configTransform.offsetRotation);

        transform.localScale = cfg.Render.scale;
        return this;
    }

    // Update is called once per frame
    void OnDestroy() {
        reader?.StopAndWait();
        codec?.StopAndWait();
        writer?.StopAndWait();
        preparer?.StopAndWait();
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
