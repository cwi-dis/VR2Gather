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
    QueueThreadSafe encoderQueue = new QueueThreadSafe();
    QueueThreadSafe decoderQueue = new QueueThreadSafe();
    QueueThreadSafe writerQueue = new QueueThreadSafe();


    /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
    /// <param name="cfg"> Config file json </param>
    /// <param name="parent"> Transform parent where attach it </param>
    /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
    /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
    public EntityPipeline Init(Config._User cfg, Transform parent, string url_pcc = "", string url_audio = "") {
        //
        // Hack-ish code to determine whether we uses meshes or buffers to render (depends on graphic card).
        // We 
        Config._PCs pcConfig = Config.Instance.PCs;
        if (pcConfig == null) throw new System.Exception("EntityPipeline: missing PCs config");
        if (pcConfig.forceMesh || SystemInfo.graphicsShaderLevel < 50) { // Mesh
            preparer = new Workers.MeshPreparer(preparerQueue);
            render = gameObject.AddComponent<Workers.PointMeshRenderer>();
            ((Workers.PointMeshRenderer)render).preparer = (Workers.MeshPreparer)preparer;
        }
        else { // Buffer
            preparer = new Workers.BufferPreparer(preparerQueue);
            render = gameObject.AddComponent<Workers.PointBufferRenderer>();
            ((Workers.PointBufferRenderer)render).preparer = (Workers.BufferPreparer)preparer;
        }

        switch (cfg.sourceType) {
            case "pcself": // old "rs2"
            case "pccerth":
                if (cfg.PCSelfConfig == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig config");
                if (cfg.sourceType == "pcself")
                {
                    reader = new Workers.RS2Reader(cfg.PCSelfConfig, preparerQueue, encoderQueue);
                }
                else // sourcetype == pccerth: same as pcself but using Certh capturer
                {
                    reader = new Workers.CerthReader(cfg.PCSelfConfig, preparerQueue, encoderQueue);
                }
                // xxxjack For now, we only create an encoder and bin2dash for the first set of encoder parameters.
                // At some point we need to create multiple queues and all that.
                try {
                    codec = new Workers.PCEncoder(cfg.PCSelfConfig.Encoders[0], encoderQueue, writerQueue);
                }
                catch (System.EntryPointNotFoundException) {
                    Debug.LogError("EntityPipeline: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                    throw new System.Exception("EntityPipeline: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                }
                if (cfg.PCSelfConfig.Bin2Dash == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.Bin2Dash config");
                try
                {
                    writer = new Workers.B2DWriter(cfg.PCSelfConfig.Bin2Dash, url_pcc, writerQueue);
                }
                catch (System.EntryPointNotFoundException e) {
                    Debug.LogError($"EntityPipeline: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                    throw new System.Exception($"EntityPipeline: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                }
                Debug.Log($"Config.Instance.useAudio {Config.Instance.useAudio}");
                if (Config.Instance.useAudio) {
                    try {
                        gameObject.AddComponent<VoiceDashSender>().Init(cfg.PCSelfConfig.AudioBin2Dash, url_audio); //Audio Pipeline
                    } catch (System.EntryPointNotFoundException e) {
                        Debug.LogError("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                        throw new System.Exception("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                    }
                }
                break;
            case "pcsub":
                if (cfg.SUBConfig == null) throw new System.Exception("EntityPipeline: missing other-user SUBConfig config");
                reader = new Workers.SUBReader(cfg.SUBConfig, url_pcc, decoderQueue);
                codec = new Workers.PCDecoder(decoderQueue, preparerQueue);
                if (Config.Instance.useAudio) {
                    if (cfg.AudioSUBConfig == null) throw new System.Exception("EntityPipeline: missing other-user AudioSUBConfig config");
                    gameObject.AddComponent<VoiceDashReceiver>().Init(cfg.AudioSUBConfig, url_audio); //Audio Pipeline
                }
                break;
        }        

        //Position depending on config calibration
        transform.localPosition = pcConfig.offsetPosition;
        transform.rotation = Quaternion.Euler(pcConfig.offsetRotation);

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
