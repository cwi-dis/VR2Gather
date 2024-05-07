#define NO_VOICE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.UserRepresentation.Voice;
using VRT.Core;
using Cwipc;
using VRT.Video;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Transport.TCP;
using VRT.Orchestrator.Wrapping;
using VRT.Pilots.Common;

namespace VRT.UserRepresentation.WebCam
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;

    public class WebCamPipeline : BasePipeline
    {
        public int width = 1280;
        public int height = 720;
        public int fps = 12;
        public int bitrate = 200000;
        bool ready = false;

        public Texture2D texture;
        public WebCamTexture webCamTexture;


        AsyncWebCamReader webReader;
        AsyncWorker reader;
        AsyncWorker encoder;
        AsyncVideoDecoder decoder;
        AsyncWorker writer;
        AsyncVideoPreparer preparer;

        QueueThreadSafe encoderQueue;
        QueueThreadSafe writerQueue = new QueueThreadSafe("WebCamPipelineWriter");
        QueueThreadSafe videoCodecQueue = new QueueThreadSafe("WebCamPipelineCodec", 2, true);
        QueueThreadSafe videoPreparerQueue = new QueueThreadSafe("WebCamPipelinePreparer");

        public static void Register()
        {
            RegisterPipelineClass(true, UserRepresentationType.VideoAvatar, AddWebCamPipelineComponent);
            RegisterPipelineClass(false, UserRepresentationType.VideoAvatar, AddWebCamPipelineComponent);
        }

        public static BasePipeline AddWebCamPipelineComponent(GameObject dst, UserRepresentationType i)
        {
            if (VRTConfig.Instance.ffmpegDLLDir != "")
            {
                FFmpeg.AutoGen.ffmpeg.RootPath = VRTConfig.Instance.ffmpegDLLDir;
            }
            return dst.AddComponent<WebCamPipeline>();
        }

        /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
        /// <param name="cfg"> Config file json </param>
        /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
        /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
        public override BasePipeline Init(bool isLocalPlayer, object _user, VRTConfig._User cfg, bool preview = false)
        {
            User user = (User)_user;
            if (user == null || user.userData == null)
            {
                Debug.LogError($"WebCamPipeline: programmer error: incorrect user parameter");
                return null;
            }
            //bool useDash = Config.Instance.protocolType == Config.ProtocolType.Dash;
            FFmpeg.AutoGen.AVCodecID codec = FFmpeg.AutoGen.AVCodecID.AV_CODEC_ID_H264;
            if (SessionConfig.Instance.videoCodec == "h264")
            {
                codec = FFmpeg.AutoGen.AVCodecID.AV_CODEC_ID_H264;
            }
            else
            {
                Debug.LogError($"WebCamPipeline: unknown codec: {SessionConfig.Instance.videoCodec}");
            }
            isSource = isLocalPlayer;
            if (user.userData.webcamName == "None") return this;
            if (isLocalPlayer)
            {
                //
                // Allocate queues we need for this sourceType
                //
                encoderQueue = new QueueThreadSafe("WebCamPipelineEncoder", 2, true);
                //
                // Create reader
                //
                webReader = new AsyncWebCamReader(user.userData.webcamName, width, height, fps, this, encoderQueue);
                webCamTexture = webReader.webcamTexture;
                if (!preview)
                {
                    //
                    // Create encoders for transmission
                    //
                    try
                    {
                        encoder = new AsyncVideoEncoder(new AsyncVideoEncoder.Setup() { codec = codec, width = width, height = height, fps = fps, bitrate = bitrate }, encoderQueue, null, writerQueue, null);
                    }
                    catch (System.EntryPointNotFoundException e)
                    {
                        Debug.Log($"WebCamPipeline: VideoEncoder: EntryPointNotFoundException: {e}");
                        throw new System.Exception("WebCamPipeline: PCEncoder() raised EntryPointNotFound exception, skipping PC encoding");
                    }
                    //
                    // Create bin2dash writer for PC transmission
                    //
                    var Bin2Dash = cfg.PCSelfConfig.Bin2Dash;
                    if (Bin2Dash == null)
                        throw new System.Exception("WebCamPipeline: missing self-user PCSelfConfig.Bin2Dash config");
                    try
                    {
                        OutgoingStreamDescription[] dashStreamDescriptions = new OutgoingStreamDescription[1] {
                                new OutgoingStreamDescription() {
                                tileNumber = 0,
                                qualityIndex = 0,
                                inQueue = writerQueue
                                }
                            };
                       // We need some backward-compatibility hacks, depending on protocol type.
                        string url = user.sfuData.url_gen;
                        switch (SessionConfig.Instance.protocolType)
                        {
                            case SessionConfig.ProtocolType.None:
                            case SessionConfig.ProtocolType.SocketIO:
                                url = user.userId;
                                break;
                            case SessionConfig.ProtocolType.TCP:
                                url = user.sfuData.url_pcc;
                                break;
                        }
                        if (SessionConfig.Instance.protocolType == SessionConfig.ProtocolType.Dash)
                        {
                            writer = new AsyncDashWriter(url, "webcam", "wcwc", Bin2Dash.segmentSize, Bin2Dash.segmentLife, dashStreamDescriptions);
                        }
                        else
                        if (SessionConfig.Instance.protocolType == SessionConfig.ProtocolType.TCP)
                        {
                            writer = new AsyncTCPDirectWriter(url, "webcam", "wcwc", dashStreamDescriptions);
                        }
                        else
                        if (SessionConfig.Instance.protocolType == SessionConfig.ProtocolType.SocketIO)
                        {
                            writer = new AsyncSocketIOWriter(url, "webcam", "wcwc", dashStreamDescriptions);
                        }
                        else
                        {
                            Debug.LogError($"{Name()}: Unknown protocolType {SessionConfig.Instance.protocolType}");
                        }

                    }
                    catch (System.EntryPointNotFoundException e)
                    {
                        Debug.Log($"WebCamPipeline: SocketIOWriter(): EntryPointNotFound({e.Message})");
                        throw new System.Exception($"WebCamPipeline: B2DWriter() raised EntryPointNotFound({e.Message}) exception, skipping PC writing");
                    }

                }
                else
                {
                    Transform screen = transform.Find("PlayerHeadScreen");
                    var renderer = screen.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.mainTexture = webCamTexture;
                        renderer.material.SetFloat("_convertGamma", preview ? 0 : 1);

                        renderer.transform.localScale = new Vector3(0.5f, webCamTexture.height / (float)webCamTexture.width * 0.5f, 1);
                    }
                }
            }
            else
            {
       
                if (SessionConfig.Instance.protocolType == SessionConfig.ProtocolType.Dash)
                {
                    reader = new AsyncDashReader(user.sfuData.url_pcc, "webcam", 0, "wcwc", videoCodecQueue);
                }
                else
                if (SessionConfig.Instance.protocolType == SessionConfig.ProtocolType.TCP)
                {
                    reader = new AsyncTCPDirectReader(user.userData.userPCurl, "wcwc", videoCodecQueue);
                }
                else
                if (SessionConfig.Instance.protocolType == SessionConfig.ProtocolType.SocketIO)
                {
                    reader = new AsyncSocketIOReader(user.userId, "webcam", "wcwc", videoCodecQueue);
                }
                else
                {
                    Debug.LogError($"{Name()}: Unknown protocolType {SessionConfig.Instance.protocolType}");
                }

                //
                // Create video decoder.
                //
                decoder = new AsyncVideoDecoder(codec, videoCodecQueue, null, videoPreparerQueue, null);
                //
                // Create video preparer.
                //
                preparer = new AsyncVideoPreparer(videoPreparerQueue, null);
                // xxxjack should set Synchronizer here

                ready = true;
            }
            return this;
        }

        // Update is called once per frame
        System.DateTime lastUpdateTime;

        private void Update()
        {
            if (preparer == null) return;
            preparer.Synchronize();
        }

        private void LateUpdate()
        {
            if (preparer == null) return;
            if (ready)
            {
                lock (preparer)
                {
                    preparer.LatchFrame();
                    if (preparer.availableVideo > 0)
                    {
                        if (texture == null)
                        {
                            texture = new Texture2D(decoder != null ? decoder.Width : width, decoder != null ? decoder.Height : height, TextureFormat.RGB24, false, true);
                            Transform screen = transform.Find("PlayerHeadScreen");
                            var renderer = screen.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.material.mainTexture = texture;
                                renderer.transform.localScale = new Vector3(0.5f, texture.height / (float)texture.width * 0.5f, 1);
                            }
                        }
                        try
                        {
                            texture.LoadRawTextureData(preparer.GetVideoPointer(preparer.videFrameSize), preparer.videFrameSize);
                            texture.Apply();
                        }
                        catch
                        {
                            Debug.LogError("[FPA] ERROR on LoadRawTextureData.");
                        }
                    }
                }
            }



        }

        void OnDestroy()
        {
            ready = false;
            if (texture != null)
            {
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
        }


        public new SyncConfig GetSyncConfig()
        {
            if (!isSource)
            {
                Debug.LogError("Programmer error: WebCamPipeline: GetSyncConfig called for pipeline that is not a source");
                return new SyncConfig();
            }
            SyncConfig rv = new SyncConfig();
            AsyncDashWriter pcWriter = (AsyncDashWriter)writer;
            if (pcWriter != null)
            {
                rv.visuals = pcWriter.GetSyncInfo();
            }
            else
            {
                Debug.LogWarning("WebCamPipeline: GetSyncCOnfig: isSource, but writer is not a B2DWriter");
            }
            return rv;
        }

        public new void SetSyncConfig(SyncConfig config)
        {
            if (isSource)
            {
                Debug.LogError("Programmer error: WebCamPipeline: SetSyncConfig called for pipeline that is a source");
                return;
            }
            AsyncDashReader_PC pcReader = (AsyncDashReader_PC)reader;
            if (pcReader != null)
            {
                pcReader.SetSyncInfo(config.visuals);
            }
            else
            {
                Debug.LogWarning("WebCamPipeline: SetSyncConfig: reader is not a PCSubReader");
            }

        }

        public new float GetBandwidthBudget()
        {
            return 999999.0f;
        }
    }
}