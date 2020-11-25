#define NO_VOICE

using Dash;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTCore;

public class EntityPipeline : MonoBehaviour {
    bool isSource = false;
    BaseWorker reader;
    BaseWorker encoder;
    List<BaseWorker> decoders = new List<BaseWorker>();
    BaseWorker writer;
    List<BaseWorker>  preparers = new List<BaseWorker>();
    List<MonoBehaviour> renderers = new List<MonoBehaviour>();
    VoiceSender audioSender;
    VoiceReceiver audioReceiver;

    List<QueueThreadSafe> preparerQueues = new List<QueueThreadSafe>();
    QueueThreadSafe encoderQueue; 
    Workers.PCEncoder.EncoderStreamDescription[] encoderStreamDescriptions; // octreeBits, tileNumber, queue encoder->writer
    B2DWriter.DashStreamDescription[] dashStreamDescriptions;  // queue encoder->writer, tileNumber, quality
    TilingConfig tilingConfig;  // Information on pointcloud tiling and quality levels
    OrchestratorWrapping.User user;
    const bool debugTiling = false;
    // Mainly for debug messages:
    static int instanceCounter = 0;
    int instanceNumber = instanceCounter++;

    public string Name()
    {
        return $"{this.GetType().Name}#{instanceNumber}";
    }

    /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
    /// <param name="cfg"> Config file json </param>
    /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
    /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
    /// <param name="calibrationMode"> Bool to enter in calib mode and don't encode and send your own PC </param>
    public EntityPipeline Init(OrchestratorWrapping.User _user, Config._User cfg, bool preview = false) {
        user = _user;
        switch (cfg.sourceType) {
            case "self": // old "rs2"
                isSource = true;
                TiledWorker pcReader;
                var PCSelfConfig = cfg.PCSelfConfig;
                if (PCSelfConfig == null) throw new System.Exception($"{Name()}: missing self-user PCSelfConfig config");
                Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={Name()}, self=1, userid={_user.userId}, representation={(int)user.userData.userRepresentationType}");
                //
                // Create renderer and preparer for self-view.
                //
                QueueThreadSafe selfPreparerQueue = _CreateRendererAndPreparer();

                //
                // Allocate queues we need for this sourceType
                //
                encoderQueue = new QueueThreadSafe("PCEncoder", 2, true);
                //
                // Ensure we can determine from the log file who this is.
                //

                //
                // Create reader
                //
                if (user.userData.userRepresentationType == OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CWI_) // PCSELF
                {
                    var RS2ReaderConfig = PCSelfConfig.RS2ReaderConfig;
                    if (RS2ReaderConfig == null) throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.RS2ReaderConfig config");
                    pcReader = new Workers.RS2Reader(RS2ReaderConfig.configFilename, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, selfPreparerQueue, encoderQueue);
                    reader = pcReader;
                }
                else if (user.userData.userRepresentationType == OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CWIK4A_)
                {
                    var RS2ReaderConfig = PCSelfConfig.RS2ReaderConfig;
                    if (RS2ReaderConfig == null) throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.RS2ReaderConfig config");
                    pcReader = new Workers.K4AReader(RS2ReaderConfig.configFilename, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, selfPreparerQueue, encoderQueue);
                    reader = pcReader;
                }
                else if (user.userData.userRepresentationType == OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_PROXY__)
                {
                    var ProxyReaderConfig = PCSelfConfig.ProxyReaderConfig;
                    if (ProxyReaderConfig == null) throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.ProxyReaderConfig config");
                    pcReader = new Workers.ProxyReader(ProxyReaderConfig.localIP, ProxyReaderConfig.port, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, selfPreparerQueue, encoderQueue);
                    reader = pcReader;
                }
                else if (user.userData.userRepresentationType == OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_SYNTH__)
                {
                    int nPoints = 0;
                    var SynthReaderConfig = PCSelfConfig.SynthReaderConfig;
                    if (SynthReaderConfig != null) nPoints = SynthReaderConfig.nPoints;
                    pcReader = new Workers.RS2Reader(PCSelfConfig.frameRate, nPoints, selfPreparerQueue, encoderQueue);
                    reader = pcReader;
                }
                else // sourcetype == pccerth: same as pcself but using Certh capturer
                {
                    var CerthReaderConfig = PCSelfConfig.CerthReaderConfig;
                    if (CerthReaderConfig == null) throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.CerthReaderConfig config");
                    pcReader = new Workers.CerthReader(
                        CerthReaderConfig.ConnectionURI,
                        CerthReaderConfig.PCLExchangeName,
                        CerthReaderConfig.MetaExchangeName,
                        CerthReaderConfig.OriginCorrection,
                        CerthReaderConfig.BoundingBotLeft,
                        CerthReaderConfig.BoundingTopRight,
                        PCSelfConfig.voxelSize,
                        selfPreparerQueue,
                        encoderQueue);
                    reader = pcReader;
                }

                if (!preview) {
                    //
                    // allocate and initialize per-stream outgoing stream datastructures
                    //
                    var Encoders = PCSelfConfig.Encoders;
                    int minTileNum = 0;
                    int nTileToTransmit = 1;
                    Vector3[] tileNormals = null;
                    if (PCSelfConfig.tiled)
                    {
                        TiledWorker.TileInfo[] tilesToTransmit = pcReader.getTiles();
                        if (tilesToTransmit != null && tilesToTransmit.Length > 1)
                        {
                            minTileNum = 1;
                            nTileToTransmit = tilesToTransmit.Length - 1;
                            tileNormals = new Vector3[nTileToTransmit];
                            for (int i=0; i<tilesToTransmit.Length; i++)
                            {
                                Debug.Log($"{Name()}: tiling sender: tile {i}: normal=({tilesToTransmit[i].normal.x}, {tilesToTransmit[i].normal.y}, {tilesToTransmit[i].normal.z}), camName={tilesToTransmit[i].cameraName}, mask={tilesToTransmit[i].cameraMask}");
                                if (i >= minTileNum)
                                {
                                    tileNormals[i - minTileNum] = tilesToTransmit[i].normal;
                                }
                            }
                        }
                    }
                    if (tileNormals == null)
                    {
                        tileNormals = new Vector3[1]
                        {
                            new Vector3(0, 0, 0)
                        };
                    }
                    int nQuality = Encoders.Length;
                    int nStream = nQuality * nTileToTransmit;
                    Debug.Log($"{Name()}: tiling sender: minTile={minTileNum}, nTile={nTileToTransmit}, nQuality={nQuality}, nStream={nStream}");
                    // xxxjack Unsure about C# array initialization: is what I do here and below in the loop correct?
                    encoderStreamDescriptions = new Workers.PCEncoder.EncoderStreamDescription[nStream];
                    dashStreamDescriptions = new B2DWriter.DashStreamDescription[nStream];
                    tilingConfig = new TilingConfig();
                    tilingConfig.tiles = new TilingConfig.TileInformation[nTileToTransmit];
                    for (int it = 0; it < nTileToTransmit; it++) {
                        tilingConfig.tiles[it].orientation = tileNormals[it];
                        tilingConfig.tiles[it].qualities = new TilingConfig.TileInformation.QualityInformation[nQuality];
                        for (int iq = 0; iq < nQuality; iq++) {
                            int i = it * nQuality + iq;
                            QueueThreadSafe thisQueue = new QueueThreadSafe($"PCEncoder{it}_{iq}" );
                            int octreeBits = Encoders[iq].octreeBits;
                            encoderStreamDescriptions[i] = new Workers.PCEncoder.EncoderStreamDescription {
                                octreeBits = octreeBits,
                                tileNumber = it+minTileNum,
                                outQueue = thisQueue
                            };
                            dashStreamDescriptions[i] = new B2DWriter.DashStreamDescription {
                                tileNumber = (uint)(it+minTileNum),
                                quality = (uint)(100 * octreeBits + 75),
                                inQueue = thisQueue
                            };
                            tilingConfig.tiles[it].qualities[iq].bandwidthRequirement = octreeBits*octreeBits*octreeBits; // xxxjack
                            tilingConfig.tiles[it].qualities[iq].representation = ((float)octreeBits / 20); // guessing octreedepth of 20 is completely ridiculously high
                        }
                    }

                    //
                    // Create encoders for transmission
                    //
                    try {
                        encoder = new Workers.PCEncoder(encoderQueue, encoderStreamDescriptions);
                    } catch (System.EntryPointNotFoundException) {
                        Debug.Log($"{Name()}: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                        throw new System.Exception($"{Name()}: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                    }
                    //
                    // Create bin2dash writer for PC transmission
                    //
                    var Bin2Dash = cfg.PCSelfConfig.Bin2Dash;
                    if (Bin2Dash == null)
                        throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.Bin2Dash config");
                    try {
                        if( Config.Instance.protocolType == Config.ProtocolType.Dash )
                            writer = new B2DWriter(user.sfuData.url_pcc, "pointcloud", "cwi1", Bin2Dash.segmentSize, Bin2Dash.segmentLife, dashStreamDescriptions);
                        else
                            writer = new Workers.SocketIOWriter(user, "pointcloud", dashStreamDescriptions);
                    } catch (System.EntryPointNotFoundException e) {
                        Debug.Log($"{Name()}: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                        throw new System.Exception($"{Name()}: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                    }
                }
                break;
            case "remote":
                var SUBConfig = cfg.SUBConfig;
                if (SUBConfig == null) throw new System.Exception($"{Name()}: missing other-user SUBConfig config");
                Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={Name()}, self=0, userid={_user.userId}");
                //
                // Determine how many tiles (and therefore decode/render pipelines) we need
                //
                Debug.Log($"{Name()} delay CreatePointcloudReader until tiling information received");
                break;
            default:
                Debug.LogError($"Programmer error: {Name()}: unknown sourceType {cfg.sourceType}");
                break;
        }
        //
        // Finally we modify the reference parameter transform, which will put the pointclouds at the correct position
        // in the scene.
        //
        //Position in the center
        transform.localPosition = new Vector3(0, 0, 0);
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        transform.localScale = cfg.Render.scale;
        return this;
    }

    private void _CreatePointcloudReader(int[] tileNumbers, int initialDelay)
    {

        int nTileToReceive = tileNumbers == null ? 0 : tileNumbers.Length;
        if (nTileToReceive == 0)
        {
            tileNumbers = new int[1] { 0 };
            nTileToReceive = 1;
        }
        //
        // Create the right number of rendering pipelines
        //

        PCSubReader.TileDescriptor[] tilesToReceive = new PCSubReader.TileDescriptor[nTileToReceive];

        for (int i = 0; i < nTileToReceive; i++)
        {
            //
            // Allocate queues we need for this pipeline
            //
            QueueThreadSafe decoderQueue = new QueueThreadSafe("PCdecoderQueue",2, true);
            //
            // Create renderer
            //
            QueueThreadSafe preparerQueue = _CreateRendererAndPreparer();
            //
            // Create pointcloud decoder, let it feed its pointclouds to the preparerQueue
            //
            BaseWorker decoder = new Workers.PCDecoder(decoderQueue, preparerQueue);
            decoders.Add(decoder);
            //
            // And collect the relevant information for the Dash receiver
            //
            tilesToReceive[i] = new PCSubReader.TileDescriptor()
            {
                outQueue = decoderQueue,
                tileNumber = tileNumbers[i]
            };
        };
        if (Config.Instance.protocolType == Config.ProtocolType.Dash)
            reader = new PCSubReader(user.sfuData.url_pcc, "pointcloud", initialDelay, tilesToReceive);
        else
            reader = new Workers.SocketIOReader(user, "pointcloud", tilesToReceive);
        Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={Name()}, reader={reader.Name()}");
    }

    public QueueThreadSafe _CreateRendererAndPreparer()
    {
        //
        // Hack-ish code to determine whether we uses meshes or buffers to render (depends on graphic card).
        // We 
        Config._PCs PCs = Config.Instance.PCs;
        if (PCs == null) throw new System.Exception($"{Name()}: missing PCs config");
        QueueThreadSafe preparerQueue = new QueueThreadSafe("PCPreparerQueue", 2, false);
        preparerQueues.Add(preparerQueue);
        if (PCs.forceMesh || SystemInfo.graphicsShaderLevel < 50)
        { // Mesh
            Workers.MeshPreparer preparer = new Workers.MeshPreparer(preparerQueue, PCs.defaultCellSize, PCs.cellSizeFactor);
            preparers.Add(preparer);
            // For meshes we use a single renderer and multiple preparers (one per tile).
            Workers.PointMeshRenderer render = gameObject.AddComponent<Workers.PointMeshRenderer>();
            Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={Name()}, preparer={preparer.Name()}, renderer={render.Name()}");
            renderers.Add(render);
            render.SetPreparer(preparer);
        }
        else
        { // Buffer
            // For buffers we use a renderer/preparer for each tile
            Workers.BufferPreparer preparer = new Workers.BufferPreparer(preparerQueue, PCs.defaultCellSize, PCs.cellSizeFactor);
            preparers.Add(preparer);
            Workers.PointBufferRenderer render = gameObject.AddComponent<Workers.PointBufferRenderer>();
            Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={Name()}, preparer={preparer.Name()}, renderer={render.Name()}");
            renderers.Add(render);
            render.SetPreparer(preparer);
        }
        return preparerQueue;
    }

    // Update is called once per frame
    System.DateTime lastUpdateTime;
    private void Update()
    {
        if (debugTiling)
        {
            // Debugging: print position/orientation of camera and others every 10 seconds.
            if (lastUpdateTime == null || (System.DateTime.Now > lastUpdateTime + System.TimeSpan.FromSeconds(10)))
            {
                lastUpdateTime = System.DateTime.Now;
                if (isSource)
                {
                    ViewerInformation vi = GetViewerInformation();
                    Debug.Log($"xxxjack {Name()} self: pos=({vi.position.x}, {vi.position.y}, {vi.position.z}), lookat=({vi.gazeForwardDirection.x}, {vi.gazeForwardDirection.y}, {vi.gazeForwardDirection.z})");
                }
                else
                {
                    Vector3 position = GetPosition();
                    Vector3 rotation = GetRotation();
                    Debug.Log($"xxxjack {Name()} other: pos=({position.x}, {position.y}, {position.z}), rotation=({rotation.x}, {rotation.y}, {rotation.z})");
                }
            }
        }
    }

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
        Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={Name()}, finished=1");
        // xxxjack the ShowTotalRefCount call may come too early, because the VoiceDashSender and VoiceDashReceiver seem to work asynchronously...
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }

    public TilingConfig GetTilingConfig()
    {
        if (!isSource)
        {
            Debug.LogError($"Programmer error: {Name()}: GetTilingConfig called for pipeline that is not a source");
            return new TilingConfig();
        }
        // xxxjack we need to update the orientation vectors, or we need an extra call to get rotation parameters.
        return tilingConfig;
    }

    public void SetTilingConfig(TilingConfig config)
    {
        if (isSource)
        {
            Debug.LogError($"Programmer error: {Name()}: SetTilingConfig called for pipeline that is a source");
            return;
        }
        if (tilingConfig.tiles != null && tilingConfig.tiles.Length > 0)
        {
            //Debug.Log($"{Name()}: xxxjack ignoring second tilingConfig");
            return;
        }
        tilingConfig = config;
        Debug.Log($"{Name()}: received tilingConfig with {tilingConfig.tiles.Length} tiles");
        int[] tileNumbers = new int[tilingConfig.tiles.Length];
        //
        // At some stage we made the decision that tilenumer 0 represents the whole untiled pointcloud.
        // So if we receive an untiled stream we want tile 0 only, and if we receive a tiled stream we
        // never want tile 0.
        //
        int curTileNumber = tilingConfig.tiles.Length == 1 ? 0 : 1;
        int curTileIndex = 0;
        foreach (var tile in tilingConfig.tiles)
        {
            tileNumbers[curTileIndex] = curTileNumber;
            Debug.Log($"{Name()}: xxxjack tile: #qualities: {tile.qualities.Length}");
            foreach(var quality in tile.qualities)
            {
                Debug.Log($"{Name()}: xxxjack quality: representation {quality.representation} bandwidth {quality.bandwidthRequirement}");
            }
            curTileNumber++;
            curTileIndex++;
        }
        _CreatePointcloudReader(tileNumbers, 0);
    }

    public SyncConfig GetSyncConfig()
    {
        if (!isSource)
        {
            Debug.LogError($"Programmer error: {Name()}: GetSyncConfig called for pipeline that is not a source");
            return new SyncConfig();
        }
        SyncConfig rv = new SyncConfig();
        if (writer is B2DWriter pcWriter)
        {
            rv.visuals = pcWriter.GetSyncInfo();
        }
        else
        {
            Debug.LogWarning($"{Name()}: GetSyncCOnfig: isSource, but writer is not a B2DWriter");
        }

        if (audioSender != null)
        {
            rv.audio = audioSender.GetSyncInfo();
        }
        // xxxjack also need to do something for VioceIOSender....
        return rv;
    }

    public void SetSyncConfig(SyncConfig config)
    {
        if (isSource)
        {
            Debug.LogError($"Programmer error: {Name()}: SetSyncConfig called for pipeline that is a source");
            return;
        }
        PCSubReader pcReader = (PCSubReader)reader;
        if (pcReader != null)
        {
            pcReader.SetSyncInfo(config.visuals);
        }
        else
        {
            Debug.LogWarning($"{Name()}: SetSyncConfig: reader is not a PCSubReader");
        }
        audioReceiver?.SetSyncInfo(config.audio);
    }

    public Vector3 GetPosition()
    {
        if (isSource)
        {
            Debug.LogError($"Programmer error: {Name()}: GetPosition called for pipeline that is a source");
            return new Vector3();
        }
        return transform.position;
    }

    public Vector3 GetRotation()
    {
        if (isSource)
        {
            Debug.LogError($"Programmer error: {Name()}: GetRotation called for pipeline that is a source");
            return new Vector3();
        }
        return transform.rotation * Vector3.forward;
    }

    public float GetBandwidthBudget()
    {
        return 999999.0f;
    }

    public ViewerInformation GetViewerInformation()
    {
        if (!isSource)
        {
            Debug.LogError($"Programmer error: {Name()}: GetViewerInformation called for pipeline that is not a source");
            return new ViewerInformation();
        }
        // The camera object is nested in another object on our parent object, so getting at it is difficult:
        Camera _camera = gameObject.transform.parent.GetComponentInChildren<Camera>();
        if (_camera == null)
        {
            Debug.LogError($"Programmer error: {Name()}: no Camera object for self user");
            return new ViewerInformation();
        }
        Vector3 position = _camera.transform.position;
        Vector3 forward = _camera.transform.rotation * Vector3.forward;
        return new ViewerInformation()
        {
            position = position,
            gazeForwardDirection = forward
        };
    }
}
