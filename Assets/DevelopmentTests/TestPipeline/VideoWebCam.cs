using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using VRT.Core;
using VRT.Video;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;
using Cwipc;

public class VideoWebCam : MonoBehaviour {
    public Renderer rendererOrg;
    public Renderer rendererDst;

    WebCamReader    recorder;
    VideoEncoder    encoder;
    AsyncWorker      writer;
    AsyncWorker      reader;

    VideoDecoder    decoder;
    VideoPreparer   preparer;

    QueueThreadSafe         videoDataQueue = new QueueThreadSafe("VideoWebReader");
    QueueThreadSafe         writerQueue = new QueueThreadSafe("VideoWebCamWriter");
    QueueThreadSafe         videoCodecQueue = new QueueThreadSafe("VideoWebCamCodec");
    QueueThreadSafe         videoPreparerQueue = new QueueThreadSafe("VideoWebCamPreparer",5);

    Texture2D       texture;
    public int      width = 1280;
    public int      height = 720;
    public int      fps = 12;
    public int      bitrate = 200000;
    bool            ready = false;

    public bool     useDash = false;

    private IEnumerator Start() {
        ready = false;
        while (OrchestratorController.Instance==null || OrchestratorController.Instance.MySession==null) yield return null;

        if (Config.Instance.ffmpegDLLDir != "")
        {
            FFmpeg.AutoGen.ffmpeg.RootPath = Config.Instance.ffmpegDLLDir;
        }
        WebCamDevice[] devices = WebCamTexture.devices;
        Init(FFmpeg.AutoGen.AVCodecID.AV_CODEC_ID_H264, devices[0].name);

        rendererOrg.material.mainTexture = recorder.webcamTexture;
        rendererOrg.transform.localScale = new Vector3(1, 1, recorder.webcamTexture.height / (float)recorder.webcamTexture.width);
    }

    // Start is called before the first frame update
    public void Init(FFmpeg.AutoGen.AVCodecID codec, string deviceName) {
        string remoteURL = OrchestratorController.Instance.SelfUser.sfuData.url_gen;
        string remoteStream = "webcam";
        try {
            recorder = new WebCamReader(deviceName, width, height, fps, this, videoDataQueue);
            encoder  = new VideoEncoder(new VideoEncoder.Setup() { codec =  codec, width = width, height = height, fps = fps, bitrate = bitrate },  videoDataQueue, null, writerQueue, null);
            B2DWriter.DashStreamDescription[] b2dStreams = new B2DWriter.DashStreamDescription[1] {
                new B2DWriter.DashStreamDescription() {
                    tileNumber = 0,
                    qualityIndex = 0,
                    inQueue = writerQueue
                }
            };
            if(useDash) writer = new B2DWriter(remoteURL, remoteStream, "wcss", 2000, 10000, b2dStreams);
            else writer = new SocketIOWriter(OrchestratorController.Instance.SelfUser, remoteStream, "wcss", b2dStreams);

//            if (useDash) reader = new BaseSubReader(remoteURL, remoteStream, 1, 0, videoCodecQueue);
//            else reader = new SocketIOReader(OrchestratorController.Instance.SelfUser, remoteStream, videoCodecQueue);

            decoder = new VideoDecoder(codec, videoCodecQueue, null, videoPreparerQueue, null);
            preparer = new VideoPreparer(videoPreparerQueue, null);
        }
        catch (System.Exception e) {
            Debug.Log($"VideoWebCam.Init: Exception: {e.Message}");
            throw;
        }
        ready = true;
    }

    private void Update()
    {
        preparer.Synchronize();
    }
    void LateUpdate() {
        if (Keyboard.current.vKey.wasPressedThisFrame){
            string remoteURL = OrchestratorController.Instance.SelfUser.sfuData.url_gen;
            string remoteStream = "webcam";

            if (useDash) reader = new BaseSubReader(remoteURL, remoteStream, 1, "wcwc", videoCodecQueue);
            else reader = new SocketIOReader(OrchestratorController.Instance.SelfUser, remoteStream, "wcwc", videoCodecQueue);

        }


        if (ready) {
            lock (preparer) {
                preparer.LatchFrame();
                if (preparer.availableVideo > 0) {
                    if (texture == null) {
                        texture = new Texture2D(decoder.Width, decoder.Height, TextureFormat.RGB24, false, true);
                        rendererDst.material.mainTexture = texture;
                        rendererDst.transform.localScale = new Vector3(1, 1, decoder.Height / (float)decoder.Width);
                    }
                    texture.LoadRawTextureData(preparer.GetVideoPointer(preparer.videFrameSize), preparer.videFrameSize);
                    texture.Apply();
                }
            }
        }
    }

    void OnDestroy() {
        Debug.Log("VideoDashReceiver: OnDestroy");
        encoder?.StopAndWait();
        recorder?.StopAndWait();
        decoder?.StopAndWait();
        preparer?.StopAndWait();

        Debug.Log($"VideoDashReceiver: Queues references counting: videoCodecQueue {videoCodecQueue._Count} videoPreparerQueue {videoPreparerQueue._Count} ");
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
