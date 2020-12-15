#define NO_VOICE

using VRTDash;
using Orchestrator;
using VRTSocketIO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voice;
using VRTCore;
using VRTVideo;

public class WebCamPipeline : BasePipeline {
    public int              width = 1280;
    public int              height = 720;
    public int              fps = 12;
    public int              bitrate = 200000;
    bool                    ready = false;

    public Texture2D        texture;
    public WebCamTexture    webCamTexture;


    WebCamReader webReader;
    BaseWorker reader;
    BaseWorker encoder;
    VideoDecoder decoder;
    BaseWorker writer;
    VideoPreparer preparer;

    VoiceSender audioSender;
    VoiceReceiver audioReceiver;

    QueueThreadSafe encoderQueue;
    QueueThreadSafe writerQueue         = new QueueThreadSafe("WebCamPipelineWriter");
    QueueThreadSafe videoCodecQueue     = new QueueThreadSafe("WebCamPipelineCodec", 2,true);
    QueueThreadSafe videoPreparerQueue  = new QueueThreadSafe("WebCamPipelinePreparer");

    public static void Register()
    {
        BasePipeline.RegisterPipelineClass(UserRepresentationType.__2D__, AddWebCamPipelineComponent);
    }

    public static BasePipeline AddWebCamPipelineComponent(GameObject dst, UserRepresentationType i)
    {
        return dst.AddComponent<WebCamPipeline>();
    }

    /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
    /// <param name="cfg"> Config file json </param>
    /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
    /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
    public override BasePipeline Init(System.Object _user, Config._User cfg, bool preview = false) {
        User user = (User)_user;
        if (user == null)
        {
            Debug.LogError($"WebCamPipeline: programmer error: incorrect user parameter");
            return null;
        }
        bool useDash = Config.Instance.protocolType == Config.ProtocolType.Dash;
        FFmpeg.AutoGen.AVCodecID codec = FFmpeg.AutoGen.AVCodecID.AV_CODEC_ID_H264;
        if (Config.Instance.videoCodec == "h264")
        {
            codec = FFmpeg.AutoGen.AVCodecID.AV_CODEC_ID_H264;
        } else
        {
            Debug.LogError($"WebCamPipeline: unknown codec: {Config.Instance.videoCodec}");
        }
        if (user!=null && user.userData != null && user.userData.webcamName == "None") return this;
        switch (cfg.sourceType) {
            case "self": // Local
                isSource = true;
                //
                // Allocate queues we need for this sourceType
                //
                encoderQueue = new QueueThreadSafe("WebCamPipelineEncoder", 2, true);
                //
                // Create reader
                //
                webReader = new WebCamReader(user.userData.webcamName, width, height, fps, this, encoderQueue);
                webCamTexture = webReader.webcamTexture;
                if (!preview) {
                    //
                    // Create encoders for transmission
                    //
                    try {
                        encoder = new VideoEncoder( new VideoEncoder.Setup() { codec = codec, width = width, height= height, fps= fps, bitrate = bitrate }, encoderQueue, null, writerQueue, null);
                    }
                    catch (System.EntryPointNotFoundException e) {
                        Debug.Log($"WebCamPipeline: VideoEncoder: EntryPointNotFoundException: {e}");
                        throw new System.Exception("WebCamPipeline: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                    }
                    //
                    // Create bin2dash writer for PC transmission
                    //
                    var Bin2Dash = cfg.PCSelfConfig.Bin2Dash;
                    if (Bin2Dash == null)
                        throw new System.Exception("WebCamPipeline: missing self-user PCSelfConfig.Bin2Dash config");
                    try {
                        B2DWriter.DashStreamDescription[] b2dStreams = new B2DWriter.DashStreamDescription[1] {
                        new B2DWriter.DashStreamDescription() {
                        tileNumber = 0,
                        quality = 0,
                        inQueue = writerQueue
                        }
                    };
                        if (useDash)
                            writer = new B2DWriter(user.sfuData.url_pcc, "webcam", "wcwc", Bin2Dash.segmentSize, Bin2Dash.segmentLife, b2dStreams);
                        else
                            writer = new SocketIOWriter(user, "webcam", b2dStreams);
                    }
                    catch (System.EntryPointNotFoundException e) {
                        Debug.Log($"WebCamPipeline: SocketIOWriter(): EntryPointNotFound({e.Message})");
                        throw new System.Exception($"WebCamPipeline: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                    }
                    /*
                                        //
                                        // Create pipeline for audio, if needed.
                                        // Note that this will create its own infrastructure (capturer, encoder, transmitter and queues) internally.
                                        //
                                        var AudioBin2Dash = cfg.PCSelfConfig.AudioBin2Dash;
                                        if (AudioBin2Dash == null)
                                            throw new System.Exception("WebCamPipeline: missing self-user PCSelfConfig.AudioBin2Dash config");
                                        try {
                                            audioSender = gameObject.AddComponent<VoiceSender>();
                                            audioSender.Init(user, "audio", AudioBin2Dash.segmentSize, AudioBin2Dash.segmentLife, Config.Instance.protocolType == Config.ProtocolType.Dash); //Audio Pipeline
                                        }
                                        catch (System.EntryPointNotFoundException e) {
                                            Debug.LogError("WebCamPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                                            throw new System.Exception("WebCamPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                                        }
                    */
                }
                else {
                    Transform screen = transform.Find("PlayerHeadScreen");
                    var renderer = screen.GetComponent<Renderer>();
                    if (renderer != null) {
                        renderer.material.mainTexture = webCamTexture;
                        renderer.material.SetFloat("_convertGamma", preview ? 0 : 1);

                        renderer.transform.localScale = new Vector3(0.5f, (webCamTexture.height / (float)webCamTexture.width) * 0.5f, 1);
                    }
                }
                break;
            case "remote": // Remoto
                if (useDash)    reader = new BaseSubReader(user.sfuData.url_pcc, "webcam", 1, 0, videoCodecQueue);
                else            reader = new SocketIOReader(user, "webcam", videoCodecQueue);

                //
                // Create video decoder.
                //
                decoder = new VideoDecoder(codec, videoCodecQueue, null, videoPreparerQueue, null);
                //
                // Create video preparer.
                //
                preparer = new VideoPreparer(videoPreparerQueue, null);
                /*
                                //
                                // Create pipeline for audio, if needed.
                                // Note that this will create its own infrastructure (capturer, encoder, transmitter and queues) internally.
                                //
                                var AudioSUBConfig = cfg.AudioSUBConfig;
                                if (AudioSUBConfig == null) throw new System.Exception("WebCamPipeline: missing other-user AudioSUBConfig config");
                                audioReceiver = gameObject.AddComponent<VoiceReceiver>();
                                audioReceiver.Init(user, "audio", AudioSUBConfig.streamNumber, AudioSUBConfig.initialDelay, Config.Instance.protocolType == Config.ProtocolType.Dash); //Audio Pipeline                
                */
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
                    UnityEngine.Debug.Log($"WebCamPipeline.Update ");
                    if (texture == null) {
                        texture = new Texture2D( decoder!=null?decoder.Width: width, decoder != null ? decoder.Height:height, TextureFormat.RGB24, false, true);
                        Transform screen = transform.Find("PlayerHeadScreen");
                        var renderer = screen.GetComponent<Renderer>();
                        if (renderer != null) {
                            renderer.material.mainTexture = texture;
                            renderer.transform.localScale = new Vector3(0.5f, (texture.height / (float)texture.width) * 0.5f, 1);
                        }
                    }
                    try {
                        texture.LoadRawTextureData(preparer.GetVideoPointer(preparer.videFrameSize), preparer.videFrameSize);
                        texture.Apply();
                    } catch {
                        UnityEngine.Debug.Log($"WebCamPipeline.Update ERR");
                        Debug.Log("[FPA] ERROR on LoadRawTextureData.");
                    }
                    UnityEngine.Debug.Log($"WebCamPipeline.Update OK");
                }
            }
        }



    }

    void OnDestroy() {
        ready = false;
        if (texture != null) {
            DestroyImmediate(texture);
            texture = null;
        }
        webReader?.StopAndWait();
        reader?.StopAndWait();
        encoder?.StopAndWait();
        decoder?.StopAndWait();
        writer?.StopAndWait();
        preparer?.StopAndWait();
        // xxxjack the ShowTotalRefCount call may come too early, because the VoiceDashSender and VoiceDashReceiver seem to work asynchronously...
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }


    public new SyncConfig GetSyncConfig()
    {
        if (!isSource)
        {
            Debug.LogError("Programmer error: WebCamPipeline: GetSyncConfig called for pipeline that is not a source");
            return new SyncConfig();
        }
        SyncConfig rv = new SyncConfig();
        B2DWriter pcWriter = (B2DWriter)writer;
        if (pcWriter != null)
        {
            rv.visuals = pcWriter.GetSyncInfo();
        }
        else
        {
            Debug.LogWarning("WebCamPipeline: GetSyncCOnfig: isSource, but writer is not a B2DWriter");
        }
        if (audioSender != null)
        {
            rv.audio = audioSender.GetSyncInfo();
        }
        // xxxjack also need to do something for VioceIOSender....
        return rv;
    }

    public new void SetSyncConfig(SyncConfig config)
    {
        if (isSource)
        {
            Debug.LogError("Programmer error: WebCamPipeline: SetSyncConfig called for pipeline that is a source");
            return;
        }
        PCSubReader pcReader = (PCSubReader)reader;
        if (pcReader != null)
        {
            pcReader.SetSyncInfo(config.visuals);
        }
        else
        {
            Debug.LogWarning("WebCamPipeline: SetSyncConfig: reader is not a PCSubReader");
        }

        audioReceiver?.SetSyncInfo(config.audio);
    }

    public new Vector3 GetPosition()
    {
        if (isSource)
        {
            Debug.LogError("Programmer error: WebCamPipeline: GetPosition called for pipeline that is a source");
            return new Vector3();
        }
        return transform.position;
    }

    public new Vector3 GetRotation()
    {
        if (isSource)
        {
            Debug.LogError("Programmer error: WebCamPipeline: GetRotation called for pipeline that is a source");
            return new Vector3();
        }
        return transform.rotation * Vector3.forward;
    }

    public new float GetBandwidthBudget()
    {
        return 999999.0f;
    }

    public new ViewerInformation GetViewerInformation()
    {
        if (!isSource)
        {
            Debug.LogError("Programmer error: WebCamPipeline: GetViewerInformation called for pipeline that is not a source");
            return new ViewerInformation();
        }
        // The camera object is nested in another object on our parent object, so getting at it is difficult:
        Camera _camera = gameObject.transform.parent.GetComponentInChildren<Camera>();
        if (_camera == null)
        {
            Debug.LogError("Programmer error: WebCamPipeline: no Camera object for self user");
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
