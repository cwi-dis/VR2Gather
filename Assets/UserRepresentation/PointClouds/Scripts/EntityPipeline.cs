#define NO_VOICE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPipeline : MonoBehaviour {
    Workers.BaseWorker  reader;
    Workers.BaseWorker encoder;
    List<Workers.BaseWorker> decoders = new List<Workers.BaseWorker>();
    Workers.BaseWorker  writer;
    List<Workers.BaseWorker>  preparers = new List<Workers.BaseWorker>();
    List<MonoBehaviour> renderers = new List<MonoBehaviour>();

    List<QueueThreadSafe> preparerQueues = new List<QueueThreadSafe>();
    QueueThreadSafe encoderQueue; 
    Workers.PCEncoder.EncoderStreamDescription[] encoderStreamDescriptions; // octreeBits, tileNumber, queue encoder->writer
    Workers.B2DWriter.DashStreamDescription[] dashStreamDescriptions;  // queue encoder->writer, tileNumber, quality

    /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
    /// <param name="cfg"> Config file json </param>
    /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
    /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
    /// <param name="calibrationMode"> Bool to enter in calib mode and don't encode and send your own PC </param>
    public EntityPipeline Init(string userID, Config._User cfg, string url_pcc = "", string url_audio = "", bool calibrationMode=false) {

        switch (cfg.sourceType) {
            case "pcself": // old "rs2"
            case "pccerth":
                Workers.TiledWorker pcReader;
                var PCSelfConfig = cfg.PCSelfConfig;
                if (PCSelfConfig == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig config");
                //
                // Create renderer and preparer for self-view.
                //
                QueueThreadSafe selfPreparerQueue = _CreateRendererAndPreparer();

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

                    pcReader = new Workers.RS2Reader(RS2ReaderConfig.configFilename, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, selfPreparerQueue, encoderQueue);
                    reader = pcReader;
                }
                else // sourcetype == pccerth: same as pcself but using Certh capturer
                {
                    var CerthReaderConfig = PCSelfConfig.CerthReaderConfig;
                    if (CerthReaderConfig == null) throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.CerthReaderConfig config");
                    pcReader = new Workers.CerthReader(CerthReaderConfig.ConnectionURI, CerthReaderConfig.PCLExchangeName, CerthReaderConfig.MetaExchangeName, PCSelfConfig.voxelSize, selfPreparerQueue, encoderQueue);
                    reader = pcReader;
                }

                if (!calibrationMode) {
                    //
                    // allocate and initialize per-stream outgoing stream datastructures
                    //
                    var Encoders = PCSelfConfig.Encoders;
                    int minTileNum = 0;
                    int nTileToTransmit = 1;
                    if (PCSelfConfig.tiled)
                    {
                        Workers.TiledWorker.TileInfo[] tilesToTransmit = pcReader.getTiles();
                        if (tilesToTransmit != null && tilesToTransmit.Length > 1)
                        {
                            for (int i=0; i<tilesToTransmit.Length; i++)
                            {
                                Debug.Log($"xxxjack: tile {i}: normal=({tilesToTransmit[i].normal.x}, {tilesToTransmit[i].normal.y}, {tilesToTransmit[i].normal.z}), camName={tilesToTransmit[i].cameraName}, mask={tilesToTransmit[i].cameraMask}");

                            }
                            minTileNum = 1;
                            nTileToTransmit = tilesToTransmit.Length - 1;
                        }
                    }
                    int nQuality = Encoders.Length;
                    int nStream = nQuality * nTileToTransmit;
                    Debug.Log($"xxxjack minTile={minTileNum}, nTile={nTileToTransmit}, nQuality={nQuality}, nStream={nStream}");
                    // xxxjack Unsure about C# array initialization: is what I do here and below in the loop correct?
                    encoderStreamDescriptions = new Workers.PCEncoder.EncoderStreamDescription[nStream];
                    dashStreamDescriptions = new Workers.B2DWriter.DashStreamDescription[nStream];
                    for (int it = 0; it < nTileToTransmit; it++) {
                        for (int iq = 0; iq < nQuality; iq++) {
                            int i = it * nQuality + iq;
                            QueueThreadSafe thisQueue = new QueueThreadSafe();
                            int octreeBits = Encoders[iq].octreeBits;
                            encoderStreamDescriptions[i] = new Workers.PCEncoder.EncoderStreamDescription {
                                octreeBits = octreeBits,
                                tileNumber = it+minTileNum,
                                outQueue = thisQueue
                            };
                            dashStreamDescriptions[i] = new Workers.B2DWriter.DashStreamDescription {
                                tileNumber = (uint)(it+minTileNum),
                                quality = (uint)(100 * octreeBits + 75),
                                inQueue = thisQueue
                            };
                        }
                    }

                    //
                    // Create encoders for transmission
                    //
                    try {
                        encoder = new Workers.PCEncoder(encoderQueue, encoderStreamDescriptions);
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
                //
                // Determine how many tiles (and therefore decode/render pipelines) we need
                //
                int[] tileNumbers = SUBConfig.tileNumbers;
                int nTileToReceive = tileNumbers == null ? 0 : tileNumbers.Length;
                if (nTileToReceive == 0)
                {
                    tileNumbers = new int[1] { 0 };
                    nTileToReceive = 1;
                }
                //
                // Create the right number of rendering pipelines
                //

                Workers.PCSubReader.TileDescriptor[] tilesToReceive = new Workers.PCSubReader.TileDescriptor[nTileToReceive];

                for (int i=0; i< nTileToReceive; i++) 
                {
                    //
                    // Allocate queues we need for this pipeline
                    //
                    QueueThreadSafe decoderQueue = new QueueThreadSafe(2, true);
                    //
                    // Create renderer
                    //
                    QueueThreadSafe preparerQueue = _CreateRendererAndPreparer();
                    //
                    // Create pointcloud decoder, let it feed its pointclouds to the preparerQueue
                    //
                    Workers.BaseWorker decoder = new Workers.PCDecoder(decoderQueue, preparerQueue);
                    decoders.Add(decoder);
                    //
                    // And collect the relevant information for the Dash receiver
                    //
                    tilesToReceive[i] = new Workers.PCSubReader.TileDescriptor()
                    {
                        outQueue = decoderQueue,
                        tileNumber = tileNumbers[i]
                    };
                };
                reader = new Workers.PCSubReader(url_pcc,"pointcloud", SUBConfig.initialDelay, tilesToReceive);
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
                transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat("pcs_rot_x", 0), PlayerPrefs.GetFloat("pcs_rot_y", 0), PlayerPrefs.GetFloat("pcs_rot_z", 0));
                break;
            case "pccerth":
                transform.localPosition = new Vector3(PlayerPrefs.GetFloat("tvm_pos_x", 0), PlayerPrefs.GetFloat("tvm_pos_y", 0), PlayerPrefs.GetFloat("tvm_pos_z", 0));
                transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat("tvm_rot_x", 0), PlayerPrefs.GetFloat("tvm_rot_y", 0), PlayerPrefs.GetFloat("tvm_rot_z", 0));
                break;
            default:
                //Position in the center
                transform.localPosition = new Vector3(0, 0, 0);
                transform.localRotation = Quaternion.Euler(0, 0, 0);
                //transform.localPosition = new Vector3(1, 1, 1); // To use in vertical if cameraconfig is not properly configured
                //transform.localRotation = Quaternion.Euler(0, 0, 90); // To use in vertical if cameraconfig is not properly configured
                break;
        }

        transform.localScale = cfg.Render.scale;
        return this;
    }

    public QueueThreadSafe _CreateRendererAndPreparer()
    {
        //
        // Hack-ish code to determine whether we uses meshes or buffers to render (depends on graphic card).
        // We 
        Config._PCs PCs = Config.Instance.PCs;
        if (PCs == null) throw new System.Exception("EntityPipeline: missing PCs config");
        QueueThreadSafe preparerQueue = new QueueThreadSafe(2, false);
        preparerQueues.Add(preparerQueue);
        if (PCs.forceMesh || SystemInfo.graphicsShaderLevel < 50)
        { // Mesh
            Workers.MeshPreparer preparer = new Workers.MeshPreparer(preparerQueue, PCs.defaultCellSize, PCs.cellSizeFactor);
            preparers.Add(preparer);
            // For meshes we use a single renderer and multiple preparers (one per tile).
            Workers.PointMeshRenderer render = gameObject.AddComponent<Workers.PointMeshRenderer>();
            renderers.Add(render);
            render.SetPreparer(preparer);
        }
        else
        { // Buffer
            // For buffers we use a renderer/preparer for each tile
            Workers.BufferPreparer preparer = new Workers.BufferPreparer(preparerQueue, PCs.defaultCellSize, PCs.cellSizeFactor);
            preparers.Add(preparer);
            Workers.PointBufferRenderer render = gameObject.AddComponent<Workers.PointBufferRenderer>();
            renderers.Add(render);
            render.SetPreparer(preparer);
        }
        return preparerQueue;
    }

    // Update is called once per frame
    void OnDestroy() {
        reader?.StopAndWait();
        encoder?.StopAndWait();
        foreach(var decoder in decoders)
        {
            decoder?.StopAndWait();
        }
        writer?.StopAndWait();
        foreach(var preparer in preparers)
        {
            preparer?.StopAndWait();
        }
        // xxxjack the ShowTotalRefCount call may come too early, because the VoiceDashSender and VoiceDashReceiver seem to work asynchronously...
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
