#define NO_VOICE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebCamPipeline : MonoBehaviour {
    public int              width = 1280;
    public int              height = 720;
    public int              fps = 12;
    bool                    ready = false;

    public Texture2D        texture;
    public WebCamTexture    webCamTexture;


    bool isSource = false;
    Workers.WebCamReader    webReader;
    Workers.BaseWorker      reader;
    Workers.BaseWorker      encoder;
    Workers.VideoDecoder    decoder;
    Workers.BaseWorker      writer;
    Workers.VideoPreparer   preparer;

    Component               audioComponent;

    QueueThreadSafe encoderQueue;
    QueueThreadSafe writerQueue         = new QueueThreadSafe();
    QueueThreadSafe videoCodecQueue     = new QueueThreadSafe();
    QueueThreadSafe videoPreparerQueue  = new QueueThreadSafe();

    TilingConfig tilingConfig;  // Information on pointcloud tiling and quality levels

    const bool debugTiling = false;

    /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
    /// <param name="cfg"> Config file json </param>
    /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
    /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
    public WebCamPipeline Init(OrchestratorWrapping.User user, Config._User cfg, string url_pcc = "", string url_audio = "") {
        Debug.Log($"----> WebCamPipeline Init userID {user.userId} cfg.sourceType {cfg.sourceType} url_pcc {url_pcc} url_audio {url_audio}");
        switch (cfg.sourceType) {
            case "pcself": // Local 
                isSource = true;
                //
                // Allocate queues we need for this sourceType
                //
                encoderQueue = new QueueThreadSafe(2, true);
                //
                // Create reader
                //
                webReader = new Workers.WebCamReader(width, height, fps, this, encoderQueue);
                webCamTexture = webReader.webcamTexture;
                //
                // Create encoders for transmission
                //
                try {
                    encoder = new Workers.VideoEncoder(encoderQueue, null, writerQueue, null);
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
                    Workers.B2DWriter.DashStreamDescription[] b2dStreams = new Workers.B2DWriter.DashStreamDescription[1] {
                        new Workers.B2DWriter.DashStreamDescription() {
                        tileNumber = 0,
                        quality = 0,
                        inQueue = writerQueue
                        }
                    };
                    //writer = new Workers.B2DWriter(url_pcc, "webcam", "wcwc", Bin2Dash.segmentSize, Bin2Dash.segmentLife, b2dStreams);
                    writer = new Workers.SocketIOWriter(user, url_pcc, "webcam", b2dStreams);
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
                        VoiceDashSender _audioComponent = gameObject.AddComponent<VoiceDashSender>();
                        _audioComponent.Init(url_audio, "audio", AudioBin2Dash.segmentSize, AudioBin2Dash.segmentLife); //Audio Pipeline
                        audioComponent = _audioComponent; //Audio Pipeline
                    }
                    catch (System.EntryPointNotFoundException e) {
                        Debug.LogError("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                        throw new System.Exception("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                    }
                } else
                if (Config.Instance.audioType == Config.AudioType.SocketIO) {
                    VoiceIOSender _audioComponent = gameObject.AddComponent<VoiceIOSender>();
                    _audioComponent.Init(user, url_audio, "audio");
                    audioComponent = _audioComponent;
                }
                break;
            case "pcsub": // Remoto
                Workers.PCSubReader.TileDescriptor[] tiles = new Workers.PCSubReader.TileDescriptor[1] {
                    new Workers.PCSubReader.TileDescriptor() {
                        outQueue = videoCodecQueue,
                        tileNumber = 0
                    }
                };
                //reader = new Workers.PCSubReader(url_pcc,"webcam", 1, tiles);
                //                Socket.IO
                reader = new Workers.SocketIOReader(user, url_pcc, "webcam", tiles);

                //
                // Create video decoder.
                //
                decoder = new Workers.VideoDecoder(videoCodecQueue, null, videoPreparerQueue, null);
                //
                // Create video preparer.
                //
                preparer = new Workers.VideoPreparer(videoPreparerQueue, null);
                //
                // Create pipeline for audio, if needed.
                // Note that this will create its own infrastructure (capturer, encoder, transmitter and queues) internally.
                //
                if (Config.Instance.audioType == Config.AudioType.Dash) {
                    var AudioSUBConfig = cfg.AudioSUBConfig;
                    if (AudioSUBConfig == null) throw new System.Exception("EntityPipeline: missing other-user AudioSUBConfig config");
                    VoiceDashReceiver _audioComponent = gameObject.AddComponent<VoiceDashReceiver>();
                    _audioComponent.Init(url_audio, "audio", AudioSUBConfig.streamNumber, AudioSUBConfig.initialDelay); //Audio Pipeline
                    audioComponent = _audioComponent;
                } else
                if (Config.Instance.audioType == Config.AudioType.SocketIO) {
                    VoiceIOReceiver _audioComponent = gameObject.AddComponent<VoiceIOReceiver>();
                    _audioComponent.Init(user, url_audio, "audio");
                    audioComponent = _audioComponent;
                }
                ready = true;
                break;
        }
        return this;
    }



    // Update is called once per frame
    System.DateTime lastUpdateTime;
    float timeToFrame = 0;
    private void Update() {
        if (ready) {
            lock (preparer) {
                if (preparer.availableVideo > 0) {
                    if (texture == null) {
                        texture = new Texture2D(decoder.Width, decoder.Height, TextureFormat.RGB24, false, true);
                        Transform screen = transform.Find("Screen");
                        var renderer = screen.GetComponent<Renderer>();
                        if (renderer != null) {
                            renderer.material.mainTexture = texture;
                            renderer.transform.localScale = new Vector3(0.5f, (decoder.Height / (float)decoder.Width) * 0.5f, 1);
                        }
                    }
                    texture.LoadRawTextureData(preparer.GetVideoPointer(preparer.videFrameSize), preparer.videFrameSize);
                    texture.Apply();
                }
            }
        }



        if (debugTiling)
        {
            // Debugging: print position/orientation of camera and others every 10 seconds.
            if (lastUpdateTime == null || (System.DateTime.Now > lastUpdateTime + System.TimeSpan.FromSeconds(10)))
            {
                lastUpdateTime = System.DateTime.Now;
                if (isSource)
                {
                    ViewerInformation vi = GetViewerInformation();
                    Debug.Log($"xxxjack EntityPipeline self: pos=({vi.position.x}, {vi.position.y}, {vi.position.z}), lookat=({vi.gazeForwardDirection.x}, {vi.gazeForwardDirection.y}, {vi.gazeForwardDirection.z})");
                }
                else
                {
                    Vector3 position = GetPosition();
                    Vector3 rotation = GetRotation();
                    Debug.Log($"xxxjack EntityPipeline other: pos=({position.x}, {position.y}, {position.z}), rotation=({rotation.x}, {rotation.y}, {rotation.z})");
                }
            }
        }
    }

    void OnDestroy() {
        webReader?.StopAndWait();
        reader?.StopAndWait();
        encoder?.StopAndWait();
        decoder?.StopAndWait();
        writer?.StopAndWait();
        preparer?.StopAndWait();
        // xxxjack the ShowTotalRefCount call may come too early, because the VoiceDashSender and VoiceDashReceiver seem to work asynchronously...
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }


    public SyncConfig GetSyncConfig()
    {
        if (!isSource)
        {
            Debug.LogError("EntityPipeline: GetSyncConfig called for pipeline that is not a source");
            return new SyncConfig();
        }
        SyncConfig rv = new SyncConfig();
        Workers.B2DWriter pcWriter = (Workers.B2DWriter)writer;
        if (pcWriter != null)
        {
            rv.visuals = pcWriter.GetSyncInfo();
        }
        else
        {
            Debug.LogWarning("EntityPipeline: GetSyncCOnfig: isSource, but writer is not a B2DWriter");
        }
        VoiceDashSender audioDashWriter = (VoiceDashSender)audioComponent;
        if (audioDashWriter != null)
        {
            rv.audio = audioDashWriter.GetSyncInfo();
        }
        // xxxjack also need to do something for VioceIOSender....
        return rv;
    }

    public void SetSyncConfig(SyncConfig config)
    {
        if (isSource)
        {
            Debug.LogError("EntityPipeline: SetSyncConfig called for pipeline that is a source");
            return;
        }
        Workers.PCSubReader pcReader = (Workers.PCSubReader)reader;
        if (pcReader != null)
        {
            pcReader.SetSyncInfo(config.visuals);
        }
        else
        {
            Debug.LogWarning("EntityPipeline: SetSyncConfig: reader is not a PCSubReader");
        }
        VoiceDashReceiver audioReader = (VoiceDashReceiver)audioComponent;
        if (audioReader != null)
        {
            audioReader.SetSyncInfo(config.audio);
        }
    }

    public Vector3 GetPosition()
    {
        if (isSource)
        {
            Debug.LogError("EntityPipeline: GetPosition called for pipeline that is a source");
            return new Vector3();
        }
        return transform.position;
    }

    public Vector3 GetRotation()
    {
        if (isSource)
        {
            Debug.LogError("EntityPipeline: GetRotation called for pipeline that is a source");
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
            Debug.LogError("EntityPipeline: GetViewerInformation called for pipeline that is not a source");
            return new ViewerInformation();
        }
        // The camera object is nested in another object on our parent object, so getting at it is difficult:
        Camera _camera = gameObject.transform.parent.GetComponentInChildren<Camera>();
        if (_camera == null)
        {
            Debug.LogError("EntityPipeline: no Camera object for self user");
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
