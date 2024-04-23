using UnityEngine;
using VRT.Core;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using VRT.UserRepresentation.Voice;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;
using Cwipc;
using VRT.Pilots.Common;
using VRT.Transport.WebRTC;

namespace VRT.UserRepresentation.PointCloud
{
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;
    using EncoderStreamDescription = Cwipc.StreamSupport.EncoderStreamDescription;
    using PointCloudNetworkTileDescription = Cwipc.StreamSupport.PointCloudNetworkTileDescription;
    using static VRT.Core.VRTConfig._User;

    public class PointCloudPipelineSelf : PointCloudPipelineBase, IPointCloudPositionProvider
    {
        public static void Register()
        {
            RegisterPipelineClass(true, UserRepresentationType.PointCloud, AddPipelineComponent);
        }

        protected static BasePipeline AddPipelineComponent(GameObject dst, UserRepresentationType i)
        {
            return dst.AddComponent<PointCloudPipelineSelf>();
        }


        /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
        /// <param name="cfg"> Config file json </param>
        /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
        /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
        /// <param name="calibrationMode"> Bool to enter in calib mode and don't encode and send your own PC </param>
        public override BasePipeline Init(bool isLocalPlayer, object _user, VRTConfig._User cfg, bool preview = false)
        {
            if (!isLocalPlayer)
            {
                Debug.LogError("${Name()}: Init() called with isLocalPlayer==false");
            }
            //
            // Decoder queue size needs to be large for tiled receivers, so we never drop a packet for one
            // tile (because it would mean that the other tiles with the same timestamp become useless)
            //
            if (CwipcConfig.Instance.decoderQueueSizeOverride > 0) pcDecoderQueueSize = CwipcConfig.Instance.decoderQueueSizeOverride;
            //
            // PreparerQueueSize needs to be large enough that there is enough storage in it to handle the
            // largest conceivable latency needed by the Synchronizer.
            //
            if (CwipcConfig.Instance.preparerQueueSizeOverride > 0) pcPreparerQueueSize = CwipcConfig.Instance.preparerQueueSizeOverride;
            user = (User)_user;
            if (!preview)
            {
                SetupConfigDistributors();
            }

            // xxxjack this links synchronizer for all instances, including self. Is that correct?
            if (synchronizer == null)
            {
                synchronizer = FindObjectOfType<VRTSynchronizer>();
            }
            // xxxjack this links tileSelector for all instances, including self. Is that correct?
            // xxxjack also: it my also reuse tileSelector for all instances. That is definitely not correct.
            if (tileSelector == null)
            {
                tileSelector = FindObjectOfType<LiveTileSelector>();
            }
           
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"self=1, userid={user.userId}, representation={(int)user.userData.userRepresentationType}");
#endif
            _InitForSelfUser(cfg.PCSelfConfig, preview);
            
            return this;
        }

        protected void _InitForSelfUser(VRTConfig._User._PCSelfConfig PCSelfConfig, bool preview)
        {
            isSource = true;
            if (synchronizer != null)
            {
                // We disable the synchronizer for self. It serves
                // no practical purpose and emits confusing stats: lines.
                Debug.Log($"{Name()}: disabling {synchronizer.Name()} for self-view");
                synchronizer.disable();
                synchronizer = null;
            }
            if (tileSelector != null)
            {
                // We disable the tileSelector for self. It serves
                // no practical purpose.
                Debug.Log($"{Name()}: disabling {tileSelector.Name()} for self-view");
                tileSelector.gameObject.SetActive(false);
                tileSelector = null;
            }
            AsyncPointCloudReader pcReader;
            //
            // Create renderer and preparer for self-view.
            //
            QueueThreadSafe selfPreparerQueue = _CreateRendererAndPreparer();

            //
            // Allocate queues we need for this sourceType
            //
            if (preview)
            {
                encoderQueue = null;
            }
            else
            {
                encoderQueue = new QueueThreadSafe("PCEncoder", 2, true);
            }
            //
            // Ensure we can determine from the log file who this is.
            //

            //
            // Create reader
            //
            pcReader = PointCloudCapturerFactory.Create(PCSelfConfig, selfPreparerQueue, encoderQueue);
#if xxxjack_old
            switch (user.userData.userRepresentationType)
            {
                case UserRepresentationType.Old__PCC_CWI_:
                    break;
             
                case UserRepresentationType.Old__PCC_CWIK4A_:
                    var KinectReaderConfig = PCSelfConfig.CameraReaderConfig; // Note: config shared with rs2
                    if (KinectReaderConfig == null) throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.CameraReaderConfig config");
                    pcReader = new AsyncKinectReader(KinectReaderConfig.configFilename, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, selfPreparerQueue, encoderQueue);
                    break;
                case UserRepresentationType.Old__PCC_PROXY__:
                    var ProxyReaderConfig = PCSelfConfig.ProxyReaderConfig;
                    if (ProxyReaderConfig == null) throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.ProxyReaderConfig config");
                    pcReader = new ProxyReader(ProxyReaderConfig.localIP, ProxyReaderConfig.port, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, selfPreparerQueue, encoderQueue);
                    break;
                case UserRepresentationType.Old__PCC_SYNTH__:
                    int nPoints = 0;
                    var SynthReaderConfig = PCSelfConfig.SynthReaderConfig;
                    if (SynthReaderConfig != null) nPoints = SynthReaderConfig.nPoints;
                    pcReader = new AsyncSyntheticReader(PCSelfConfig.frameRate, nPoints, selfPreparerQueue, encoderQueue);
                    break;
                case UserRepresentationType.Old__PCC_PRERECORDED__:
                    var prConfig = PCSelfConfig.PrerecordedReaderConfig;
                    if (prConfig.folder == null || prConfig.folder == "")
                    {
                        throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.PrerecordedReaderConfig.folder config");
                    }
                    pcReader = new AsyncPrerecordedReader(prConfig.folder, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, selfPreparerQueue, encoderQueue);
                    break;
                default:
                    throw new System.Exception($"{Name()}: Unknown representation {user.userData.userRepresentationType}");

            }
#endif

            reader = pcReader;

            if (!preview)
            {
                // Which encoder do we want?
                string pointcloudCodec = SessionConfig.Instance.pointCloudCodec;
               // For TCP we want short queues and we want them leaky (so we don't hang)
                bool leakyQueues = SessionConfig.Instance.protocolType == SessionConfig.ProtocolType.TCP;
                //
                // Determine tiles to transmit
                //
                Cwipc.PointCloudTileDescription[] tilesToTransmit = null;
                if (PCSelfConfig.tiled)
                {
                    tilesToTransmit = pcReader.getTiles();
                    if (tilesToTransmit != null && tilesToTransmit.Length > 1)
                    {
                        // Skip tile 0, it is the untiled cloud that has all points.
                        tilesToTransmit = tilesToTransmit[1..];
                        for (int i = 0; i < tilesToTransmit.Length; i++)
                        {
                            Debug.Log($"{Name()}: tiling sender: tile {i}: normal=({tilesToTransmit[i].normal.x}, {tilesToTransmit[i].normal.y}, {tilesToTransmit[i].normal.z}), camName={tilesToTransmit[i].cameraName}, mask={tilesToTransmit[i].cameraMask}");
                        }
                    }
                }
                if (tilesToTransmit == null)
                {
                    // If we don't want tiled sending, or the source isn't tiled, we invent a tile description
                    tilesToTransmit = new PointCloudTileDescription[1]
                    {
                        new PointCloudTileDescription()
                        {
                            cameraMask=0,
                            cameraName="untiled",
                            normal=Vector3.zero
                        }
                    };
                }
                //
                // allocate and initialize per-stream outgoing stream datastructures
                //
                _CreateDescriptionsForOutgoing(tilesToTransmit, PCSelfConfig.Encoders, leakyQueues);


                //
                // Create encoders for transmission
                //
                switch (pointcloudCodec)
                {
                    case "cwi0":
                        encoder = new AsyncPCNullEncoder(encoderQueue, encoderStreamDescriptions);
                        break;
                    case "cwi1":
                        encoder = new AsyncPCEncoder(encoderQueue, encoderStreamDescriptions);
                        break;
                    default:
                        throw new System.Exception($"{Name()}: Unknown pointcloudCodec \"{pointcloudCodec}\"");
                }

                //
                // Create correct writer for PC transmission
                //
                switch (SessionConfig.Instance.protocolType)
                {
                    case SessionConfig.ProtocolType.Dash:
                        writer = new AsyncB2DWriter(user.sfuData.url_pcc, "pointcloud", pointcloudCodec, PCSelfConfig.Bin2Dash.segmentSize, PCSelfConfig.Bin2Dash.segmentLife, outgoingStreamDescriptions);
                        break;
                    case SessionConfig.ProtocolType.TCP:
                        writer = new AsyncTCPWriter(user.userData.userPCurl, pointcloudCodec, outgoingStreamDescriptions);
                        break;
                    case SessionConfig.ProtocolType.None:
                    case SessionConfig.ProtocolType.SocketIO:
                        writer = new AsyncSocketIOWriter(user, "pointcloud", pointcloudCodec, outgoingStreamDescriptions);
                        break;
                    case SessionConfig.ProtocolType.WebRTC:
                        writer = new AsyncWebRTCWriter(user.sfuData.url_gen, pointcloudCodec, outgoingStreamDescriptions);
                        break;
                    default:
                        throw new System.Exception($"{Name()}: Unknown protocolType {SessionConfig.Instance.protocolType}");
                }

#if VRT_WITH_STATS
                Statistics.Output(Name(), $"reader={reader.Name()}, encoder={encoder.Name()}, writer={writer.Name()}, ntile={tilesToTransmit.Length}, nquality={PCSelfConfig.Encoders.Length}, nStream={outgoingStreamDescriptions.Length}");
#endif
            }
        }

        private void _CreateDescriptionsForOutgoing(Cwipc.PointCloudTileDescription[] tilesToTransmit, VRTConfig._User._PCSelfConfig._Encoder[] Encoders, bool leakyQueues)
        {
            int[] octreeBitsArray = new int[Encoders.Length];
            for (int i = 0; i < Encoders.Length; i++)
            {
                octreeBitsArray[i] = Encoders[i].octreeBits;
            }
            int nTileToTransmit = tilesToTransmit.Length;
            int minTileNum = nTileToTransmit == 1 ? 0 : 1;
            int nQuality = Encoders.Length;
            int nStream = nQuality * nTileToTransmit;
            Debug.Log($"{Name()}: tiling sender: minTileNum={minTileNum}, nTile={nTileToTransmit}, nQuality={nQuality}, nStream={nStream}");
            //
            // Create all three sets of descriptions needed.
            //
            encoderStreamDescriptions = StreamSupport.CreateEncoderStreamDescription(tilesToTransmit, octreeBitsArray);
            outgoingStreamDescriptions = StreamSupport.CreateOutgoingStreamDescription(tilesToTransmit, octreeBitsArray);
            networkTileDescription = StreamSupport.CreateNetworkTileDescription(tilesToTransmit, octreeBitsArray);
            //
            // Create the queues and link the encoders and transmitters together through their individual queues.
            //
            // For the TCP connections we want legth 1 leaky queues. For
            // DASH we want length 2 non-leaky queues.
            bool e2tQueueDrop = false;
            int e2tQueueSize = 2;
            if (leakyQueues)
            {
                e2tQueueDrop = true;
                e2tQueueSize = 1;
            }
            for (int tileNum = 0; tileNum < nTileToTransmit; tileNum++)
            {
                for (int qualityNum = 0; qualityNum < nQuality; qualityNum++)
                {
                    int streamNum = tileNum * nQuality + qualityNum;
                    QueueThreadSafe thisQueue = new QueueThreadSafe($"PCEncoder{tileNum}_{qualityNum}", e2tQueueSize, e2tQueueDrop);
                    encoderStreamDescriptions[streamNum].outQueue = thisQueue;
                    outgoingStreamDescriptions[streamNum].inQueue = thisQueue;
                }
            }
        }

        public void SetCrop(float[] _bbox)
        {
            if (!isSource)
            {
                Debug.LogError($"Programmer error: {Name()}: SetCrop called for pipeline that is not a source");
                return;
            }
            AsyncPointCloudReader pcReader = reader as AsyncPointCloudReader;
            if (pcReader == null)
            {
                Debug.Log($"{Name()}: SetCrop: not a PCReader");
                return;
            }
            pcReader.SetCrop(_bbox);
        }

        public void ClearCrop()
        {
            SetCrop(null);
        }

        public PointCloudNetworkTileDescription GetTilingConfig()
        {
            if (!isSource)
            {
                Debug.LogError($"Programmer error: {Name()}: GetTilingConfig called for pipeline that is not a source");
                return new PointCloudNetworkTileDescription();
            }
            // xxxjack we need to update the orientation vectors, or we need an extra call to get rotation parameters.
            return networkTileDescription;
        }

        public new SyncConfig GetSyncConfig()
        {
            if (!isSource)
            {
                Debug.LogError($"Programmer error: {Name()}: GetSyncConfig called for pipeline that is not a source");
                return new SyncConfig();
            }
            SyncConfig rv = new SyncConfig();
            if (writer is AsyncWriter pcWriter)
            {
                rv.visuals = pcWriter.GetSyncInfo();
            }
            else
            {
                Debug.LogError($"{Name()}: GetSyncConfig: isSource, but writer is not a BaseWriter");
            }
            // The voice sender object is nested in another object on our parent object, so getting at it is difficult:
            VoicePipelineSelf voiceSender = gameObject.transform.parent.GetComponentInChildren<VoicePipelineSelf>();
            if (voiceSender != null)
            {
                rv.audio = voiceSender.GetSyncInfo();
            }
            Debug.Log($"{Name()}: GetSyncConfig: visual {rv.visuals.wallClockTime}={rv.visuals.streamClockTime}, audio {rv.audio.wallClockTime}={rv.audio.streamClockTime}");
            return rv;
        }

        public Vector3? GetPosition()
        {
            AsyncPointCloudReader pcReader = reader as AsyncPointCloudReader;
            if (pcReader == null)
            {
                return null;
            }
            return pcReader.GetPosition();
        }

        public int GetCameraCount()
        {
            AsyncPointCloudReader pcReader = reader as AsyncPointCloudReader;
            if (pcReader == null)
            {
                return 0;
            }
            return pcReader.GetCameraCount();
        }
    }
}

