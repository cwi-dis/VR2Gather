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
    /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
    /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
    public EntityPipeline Init(Config._User cfg, string url_pcc = "", string url_audio = "") {
        //
        // Hack-ish code to determine whether we uses meshes or buffers to render (depends on graphic card).
        // We 
        Config._PCs PCs = Config.Instance.PCs;
        if (PCs == null) throw new System.Exception("EntityPipeline: missing PCs config");
        if (PCs.forceMesh || SystemInfo.graphicsShaderLevel < 50) { // Mesh
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
                var PCSelfConfig = cfg.PCSelfConfig;
                if (PCSelfConfig == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig config");
                if (cfg.sourceType == "pcself")
                {
                    var RS2ReaderConfig = PCSelfConfig.RS2ReaderConfig;
                    if (RS2ReaderConfig == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.RS2ReaderConfig config");

                    reader = new Workers.RS2Reader(RS2ReaderConfig.configFilename, PCSelfConfig.voxelSize, preparerQueue, encoderQueue);
                }
                else // sourcetype == pccerth: same as pcself but using Certh capturer
                {
                    var CerthReaderConfig = PCSelfConfig.CerthReaderConfig;
                    if (CerthReaderConfig == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.CerthReaderConfig config");
                    reader = new Workers.CerthReader(CerthReaderConfig.ConnectionURI, CerthReaderConfig.PCLExchangeName, CerthReaderConfig.MetaExchangeName, PCSelfConfig.voxelSize, preparerQueue, encoderQueue);
                }
                // xxxjack For now, we only create an encoder and bin2dash for the first set of encoder parameters.
                // At some point we need to create multiple queues and all that.
                var Encoders = PCSelfConfig.Encoders;
                if (Encoders.Length != 1) throw new System.Exception("EntityPipeline: self-user PCSelfConfig.Encoders must have exactly 1 entry");
                try {
                    codec = new Workers.PCEncoder(Encoders[0].octreeBits, encoderQueue, writerQueue);
                }
                catch (System.EntryPointNotFoundException) {
                    Debug.LogError("EntityPipeline: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                    throw new System.Exception("EntityPipeline: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                }
                var Bin2Dash = cfg.PCSelfConfig.Bin2Dash;
                if (Bin2Dash == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.Bin2Dash config");
                try
                {
                    writer = new Workers.B2DWriter(url_pcc, "pointcloud", Bin2Dash.segmentSize, Bin2Dash.segmentLife, writerQueue);
                }
                catch (System.EntryPointNotFoundException e) {
                    Debug.LogError($"EntityPipeline: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                    throw new System.Exception($"EntityPipeline: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                }
                Debug.Log($"Config.Instance.useAudio {Config.Instance.useAudio}");
                if (Config.Instance.useAudio) {
                    var AudioBin2Dash = cfg.PCSelfConfig.AudioBin2Dash;
                    if (AudioBin2Dash == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.AudioBin2Dash config");
                    try
                    {
                        gameObject.AddComponent<VoiceDashSender>().Init(url_audio, "audio", AudioBin2Dash.segmentSize, AudioBin2Dash.segmentLife); //Audio Pipeline
                    } catch (System.EntryPointNotFoundException e) {
                        Debug.LogError("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                        throw new System.Exception("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                    }
                }
                break;
            case "pcsub":
                var SUBConfig = cfg.SUBConfig;
                if (SUBConfig == null) throw new System.Exception("EntityPipeline: missing other-user SUBConfig config");
                reader = new Workers.PCSubReader(url_pcc,"pointcloud", SUBConfig.streamNumber, SUBConfig.initialDelay, decoderQueue);
                codec = new Workers.PCDecoder(decoderQueue, preparerQueue);
                if (Config.Instance.useAudio) {
                    var AudioSUBConfig = cfg.AudioSUBConfig;
                    if (AudioSUBConfig == null) throw new System.Exception("EntityPipeline: missing other-user AudioSUBConfig config");
                    gameObject.AddComponent<VoiceDashReceiver>().Init(url_audio, "audio", AudioSUBConfig.streamNumber, AudioSUBConfig.initialDelay); //Audio Pipeline
                }
                break;
        }        

        //Position depending on config calibration
        transform.localPosition = PCs.offsetPosition;
        transform.rotation = Quaternion.Euler(PCs.offsetRotation);

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
