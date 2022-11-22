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
            if (Config.Instance.ffmpegDLLDir != "")
            {
                FFmpeg.AutoGen.ffmpeg.RootPath = Config.Instance.ffmpegDLLDir;
            }
            RegisterPipelineClass(UserRepresentationType.__2D__, AddWebCamPipelineComponent);
        }

        public static BasePipeline AddWebCamPipelineComponent(GameObject dst, UserRepresentationType i)
        {
            return dst.AddComponent<WebCamPipeline>();
        }

        /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
        /// <param name="cfg"> Config file json </param>
        /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
        /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
        public override BasePipeline Init(object _user, Config._User cfg, bool preview = false)
        {
            User user = (User)_user;
            if (user == null || user.userData == null)
            {
                Debug.LogError($"WebCamPipeline: programmer error: incorrect user parameter");
                return null;
            }
            //bool useDash = Config.Instance.protocolType == Config.ProtocolType.Dash;
            FFmpeg.AutoGen.AVCodecID codec = FFmpeg.AutoGen.AVCodecID.AV_CODEC_ID_H264;
            if (Config.Instance.Video.Codec == "h264")
            {
                codec = FFmpeg.AutoGen.AVCodecID.AV_CODEC_ID_H264;
            }
            else
            {
                Debug.LogError($"WebCamPipeline: unknown codec: {Config.Instance.Video.Codec}");
            }
            isSource = (cfg.sourceType == "self");
            if (user.userData.webcamName == "None") return this;
            switch (cfg.sourceType)
            {
                case "self": // Local
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
                            if (Config.Instance.protocolType == Config.ProtocolType.Dash)
                            {
                                writer = new AsyncB2DWriter(user.sfuData.url_pcc, "webcam", "wcwc", Bin2Dash.segmentSize, Bin2Dash.segmentLife, dashStreamDescriptions);
                            }
                            else
                             if (Config.Instance.protocolType == Config.ProtocolType.TCP)
                            {
                                writer = new AsyncTCPWriter(user.userData.userPCurl, "wcwc", dashStreamDescriptions);
                            }
                            else
                            {
                                writer = new AsyncSocketIOWriter(user, "webcam", "wcwc", dashStreamDescriptions);
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
                    break;
                case "remote": // Remoto
                    
                    if (Config.Instance.protocolType == Config.ProtocolType.Dash)
                    {
                        reader = new AsyncSubReader(user.sfuData.url_pcc, "webcam", 0, "wcwc", videoCodecQueue);
                    }
                    else if (Config.Instance.protocolType == Config.ProtocolType.TCP)
                    {
                        reader = new AsyncTCPReader(user.userData.userPCurl, "wcwc", videoCodecQueue);
                    }
                    else
                    {
                        reader = new AsyncSocketIOReader(user, "webcam", "wcwc", videoCodecQueue);
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
                    break;
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
            AsyncB2DWriter pcWriter = (AsyncB2DWriter)writer;
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
            AsyncSubPCReader pcReader = (AsyncSubPCReader)reader;
            if (pcReader != null)
            {
                pcReader.SetSyncInfo(config.visuals);
            }
            else
            {
                Debug.LogWarning("WebCamPipeline: SetSyncConfig: reader is not a PCSubReader");
            }

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
            PlayerManager player = gameObject.GetComponentInParent<PlayerManager>();
            Transform cameraTransform = player?.getCameraTransform();
            if (cameraTransform == null)
            {
                Debug.LogError("Programmer error: WebCamPipeline: no Camera object for self user");
                return new ViewerInformation();
            }
            Vector3 position = cameraTransform.position;
            Vector3 forward = cameraTransform.rotation * Vector3.forward;
            return new ViewerInformation()
            {
                position = position,
                gazeForwardDirection = forward
            };
        }
    }
}