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

    QueueThreadSafe preparerQueue = new QueueThreadSafe(2, false);
    QueueThreadSafe encoderQueue; 
    Workers.PCEncoder.EncoderStreamDescription[] encoderStreamDescriptions; // octreeBits, tileNumber, queue encoder->writer
    Workers.B2DWriter.DashStreamDescription[] dashStreamDescriptions;  // queue encoder->writer, tileNumber, quality
    QueueThreadSafe decoderQueue;

    /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
    /// <param name="cfg"> Config file json </param>
    /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
    /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
    /// <param name="calibrationMode"> Bool to enter in calib mode and don't encode and send your own PC </param>
    public EntityPipeline Init(string userID, Config._User cfg, string url_pcc = "", string url_audio = "", bool calibrationMode=false) {
        //
        // Start by creating the preparer, which will prepare pointclouds for display in the scene.
        //
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
                //
                // Allocate queues we need for this sourceType
                //
                encoderQueue = new QueueThreadSafe(2, true);
                //
                // Create reader
                //
                if (cfg.sourceType == "pcself")
                {
                    var RS2ReaderConfig = PCSelfConfig.RS2ReaderConfig;
                    if (RS2ReaderConfig == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.RS2ReaderConfig config");

                    reader = new Workers.RS2Reader(RS2ReaderConfig.configFilename, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, preparerQueue, encoderQueue);
                }
                else // sourcetype == pccerth: same as pcself but using Certh capturer
                {
                    var CerthReaderConfig = PCSelfConfig.CerthReaderConfig;
                    if (CerthReaderConfig == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.CerthReaderConfig config");
                    reader = new Workers.CerthReader(CerthReaderConfig.ConnectionURI, CerthReaderConfig.PCLExchangeName, CerthReaderConfig.MetaExchangeName, PCSelfConfig.voxelSize, preparerQueue, encoderQueue);
                }

                if (!calibrationMode) {
                    //
                    // allocate and initialize per-stream outgoing stream datastructures
                    //
                    var Encoders = PCSelfConfig.Encoders;
                    int nTile = 1; // xxxjack For now
                    int nQuality = Encoders.Length;
                    int nStream = nQuality * nTile;
                    // xxxjack Unsure about C# array initialization: is what I do here and below in the loop correct?
                    encoderStreamDescriptions = new Workers.PCEncoder.EncoderStreamDescription[nStream];
                    dashStreamDescriptions = new Workers.B2DWriter.DashStreamDescription[nStream];
                    for (int it = 0; it < nTile; it++) {
                        for (int iq = 0; iq < nQuality; iq++) {
                            int i = it * nQuality + iq;
                            QueueThreadSafe thisQueue = new QueueThreadSafe();
                            int octreeBits = Encoders[iq].octreeBits;
                            encoderStreamDescriptions[i] = new Workers.PCEncoder.EncoderStreamDescription {
                                octreeBits = octreeBits,
                                tileNumber = it,
                                outQueue = thisQueue
                            };
                            dashStreamDescriptions[i] = new Workers.B2DWriter.DashStreamDescription {
                                tileNumber = (uint)it,
                                quality = (uint)(100 * octreeBits + 75),
                                inQueue = thisQueue
                            };
                        }
                    }

                    //
                    // Create encoders for transmission
                    //
                    try {
                        codec = new Workers.PCEncoder(encoderQueue, encoderStreamDescriptions);
                    } catch (System.EntryPointNotFoundException) {
                        Debug.LogError("EntityPipeline: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                        throw new System.Exception("EntityPipeline: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                    }
                    //
                    // Create bin2dash writer for PC transmission
                    //
                    var Bin2Dash = cfg.PCSelfConfig.Bin2Dash;
                    if (Bin2Dash == null)
                        throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.Bin2Dash config");
                    try {
                        writer = new Workers.B2DWriter(url_pcc, "pointcloud", "cwi1", Bin2Dash.segmentSize, Bin2Dash.segmentLife, dashStreamDescriptions);
                    } catch (System.EntryPointNotFoundException e) {
                        Debug.LogError($"EntityPipeline: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                        throw new System.Exception($"EntityPipeline: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                    }
                    //
                    // Create pipeline for audio, if needed.
                    // Note that this will create its own infrastructure (capturer, encoder, transmitter and queues) internally.
                    //
                    Debug.Log($"Config.Instance.audioType {Config.Instance.audioType}");
                    if (Config.Instance.audioType == Config.AudioType.Dash) {
                        var AudioBin2Dash = cfg.PCSelfConfig.AudioBin2Dash;
                        if (AudioBin2Dash == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.AudioBin2Dash config");
                        try {
                            gameObject.AddComponent<VoiceDashSender>().Init(url_audio, "audio", AudioBin2Dash.segmentSize, AudioBin2Dash.segmentLife); //Audio Pipeline
                        } catch (System.EntryPointNotFoundException e) {
                            Debug.LogError("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                            throw new System.Exception("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                        }
                    } else
                    if (Config.Instance.audioType == Config.AudioType.SocketIO) {
                        gameObject.AddComponent<VoiceIOSender>().Init(userID);
                    }
                }
                break;
            case "pcsub":
                var SUBConfig = cfg.SUBConfig;
                if (SUBConfig == null) throw new System.Exception("EntityPipeline: missing other-user SUBConfig config");
                // Allocate queues we need for this pipeline
                decoderQueue = new QueueThreadSafe(2, true);
                //
                // Create sub receiver
                //
                reader = new Workers.PCSubReader(url_pcc,"pointcloud", SUBConfig.streamNumber, SUBConfig.initialDelay, decoderQueue);
                //
                // Create pointcloud decoder, let it feed its pointclouds to the preparerQueue
                //
                codec = new Workers.PCDecoder(decoderQueue, preparerQueue);
                //
                // Create pipeline for audio, if needed.
                // Note that this will create its own infrastructure (capturer, encoder, transmitter and queues) internally.
                //
                if (Config.Instance.audioType == Config.AudioType.Dash) {
                    var AudioSUBConfig = cfg.AudioSUBConfig;
                    if (AudioSUBConfig == null) throw new System.Exception("EntityPipeline: missing other-user AudioSUBConfig config");
                    gameObject.AddComponent<VoiceDashReceiver>().Init(url_audio, "audio", AudioSUBConfig.streamNumber, AudioSUBConfig.initialDelay); //Audio Pipeline
                } else
                if (Config.Instance.audioType == Config.AudioType.SocketIO) {
                    gameObject.AddComponent<VoiceIOReceiver>().Init(userID); //Audio Pipeline
                }
                break;
        }
        //
        // Finally we modify the reference parameter transform, which will put the pointclouds at the correct position
        // in the scene.
        //
        //Position depending on config calibration done by PCCalibration Scene
        switch (cfg.sourceType) {
            case "pcself":
                transform.localPosition = new Vector3(PlayerPrefs.GetFloat("pcs_pos_x", 0), PlayerPrefs.GetFloat("pcs_pos_y", 0), PlayerPrefs.GetFloat("pcs_pos_z", 0));
                transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat("pcs_rot_x", 0), PlayerPrefs.GetFloat("pcs_rot_y", 0), PlayerPrefs.GetFloat("pcs_pos_z", 0));
                break;
            case "pccerth":
                transform.localPosition = new Vector3(PlayerPrefs.GetFloat("tvm_pos_x", 0), PlayerPrefs.GetFloat("tvm_pos_y", 0), PlayerPrefs.GetFloat("tvm_pos_z", 0));
                transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat("tvm_rot_x", 0), PlayerPrefs.GetFloat("tvm_rot_y", 0), PlayerPrefs.GetFloat("tvm_pos_z", 0));
                break;
            default:
                //Position in the center
                transform.localPosition = new Vector3(0, 0, 0);
                transform.localRotation = Quaternion.Euler(0, 0, 0);
                break;
        }

        transform.localScale = cfg.Render.scale;
        return this;
    }

    // Update is called once per frame
    void OnDestroy() {
        reader?.StopAndWait();
        codec?.StopAndWait();
        writer?.StopAndWait();
        preparer?.StopAndWait();
        // xxxjack the ShowTotalRefCount call may come too early, because the VoiceDashSender and VoiceDashReceiver seem to work asynchronously...
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
