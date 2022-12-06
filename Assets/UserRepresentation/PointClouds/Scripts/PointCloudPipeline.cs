#define NO_VOICE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.UserRepresentation.Voice;
using VRT.Core;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Orchestrator.Wrapping;
using Cwipc;
using VRT.Pilots.Common;

namespace VRT.UserRepresentation.PointCloud
{
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;
    using EncoderStreamDescription = Cwipc.StreamSupport.EncoderStreamDescription;
    using PointCloudNetworkTileDescription = Cwipc.StreamSupport.PointCloudNetworkTileDescription;
    using static VRT.Core.Config._User;

    public class PointCloudPipeline : BasePipeline
    {
        [Tooltip("Object responsible for tile quality adaptation algorithm")]
        public BaseTileSelector tileSelector = null;
        [Tooltip("Object responsible for synchronizing playout")]
        public ISynchronizer synchronizer = null;
        static int pcDecoderQueueSize = 10;  // Was: 2.
        static int pcPreparerQueueSize = 15; // Was: 2.
        protected AsyncReader reader;
        AbstractPointCloudEncoder encoder;
        List<AbstractPointCloudDecoder> decoders = new List<AbstractPointCloudDecoder>();
        AsyncWriter writer;
        List<AsyncPointCloudPreparer> preparers = new List<AsyncPointCloudPreparer>();
        List<PointCloudRenderer> renderers = new List<PointCloudRenderer>();

        List<QueueThreadSafe> preparerQueues = new List<QueueThreadSafe>();
        QueueThreadSafe encoderQueue;
        EncoderStreamDescription[] encoderStreamDescriptions; // octreeBits, tileNumber, queue encoder->writer
        OutgoingStreamDescription[] outgoingStreamDescriptions;  // queue encoder->writer, tileNumber, quality
        PointCloudNetworkTileDescription networkTileDescription;  // Information on pointcloud tiling and quality levels
        User user;
        // Mainly for debug messages:
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public static void Register()
        {
            RegisterPipelineClass(true, UserRepresentationType.__PCC_CWIK4A_, AddPointCloudPipelineComponent);
            RegisterPipelineClass(true, UserRepresentationType.__PCC_CWI_, AddPointCloudPipelineComponent);
            RegisterPipelineClass(true, UserRepresentationType.__PCC_PROXY__, AddPointCloudPipelineComponent);
            RegisterPipelineClass(true, UserRepresentationType.__PCC_PRERECORDED__, AddPointCloudPipelineComponent);
            RegisterPipelineClass(true, UserRepresentationType.__PCC_SYNTH__, AddPointCloudPipelineComponent);

            RegisterPipelineClass(false, UserRepresentationType.__PCC_CWIK4A_, AddPointCloudPipelineComponent);
            RegisterPipelineClass(false, UserRepresentationType.__PCC_CWI_, AddPointCloudPipelineComponent);
            RegisterPipelineClass(false, UserRepresentationType.__PCC_PROXY__, AddPointCloudPipelineComponent);
            RegisterPipelineClass(false, UserRepresentationType.__PCC_PRERECORDED__, AddPointCloudPipelineComponent);
            RegisterPipelineClass(false, UserRepresentationType.__PCC_SYNTH__, AddPointCloudPipelineComponent);
        }

        public static BasePipeline AddPointCloudPipelineComponent(GameObject dst, UserRepresentationType i)
        {
            return dst.AddComponent<PointCloudPipeline>();
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
        /// <param name="cfg"> Config file json </param>
        /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
        /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
        /// <param name="calibrationMode"> Bool to enter in calib mode and don't encode and send your own PC </param>
        public override BasePipeline Init(bool isLocalPlayer, object _user, Config._User cfg, bool preview = false)
        {
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
            switch (cfg.sourceType)
            {
                case "self":
                    if (!isLocalPlayer) Debug.LogError($"{Name()}: sourceType==self but not isLocalPlayer");
#if VRT_WITH_STATS
                    Statistics.Output(Name(), $"self=1, userid={user.userId}, representation={(int)user.userData.userRepresentationType}");
#endif
                    _InitForSelfUser(cfg.PCSelfConfig, preview);
                    break;
                case "prerecorded":
                    _InitForPrerecordedPlayer(cfg.PCSelfConfig);
                    break;
                case "remote":
                    if (isLocalPlayer) Debug.LogError($"{Name()}: sourceType!=self but isLocalPlayer==true");
#if VRT_WITH_STATS
                    Statistics.Output(Name(), $"self=0, userid={user.userId}");
#endif
                    //
                    // Determine how many tiles (and therefore decode/render pipelines) we need
                    //
                    Debug.Log($"{Name()}: delay _InitForOtherUser until tiling information received");
                    break;
                default:
                    Debug.LogError($"Programmer error: {Name()}: unknown sourceType {cfg.sourceType}");
                    break;
            }
            return this;
        }

        private void _InitForSelfUser(Config._User._PCSelfConfig PCSelfConfig, bool preview)
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
            encoderQueue = new QueueThreadSafe("PCEncoder", 2, true);
            //
            // Ensure we can determine from the log file who this is.
            //

            //
            // Create reader
            //
            switch(user.userData.userRepresentationType)
            {
                case UserRepresentationType.__PCC_CWI_:
                    var RS2ReaderConfig = PCSelfConfig.RS2ReaderConfig;
                    if (RS2ReaderConfig == null) throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.RS2ReaderConfig config");
                    pcReader = new AsyncRealsenseReader(RS2ReaderConfig.configFilename, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, selfPreparerQueue, encoderQueue);
                    break;
                case UserRepresentationType.__PCC_CWIK4A_:
                    var KinectReaderConfig = PCSelfConfig.RS2ReaderConfig; // Note: config shared with rs2
                    if (KinectReaderConfig == null) throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.RS2ReaderConfig config");
                    pcReader = new AsyncKinectReader(KinectReaderConfig.configFilename, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, selfPreparerQueue, encoderQueue);
                    break;
                case UserRepresentationType.__PCC_PROXY__:
                    var ProxyReaderConfig = PCSelfConfig.ProxyReaderConfig;
                    if (ProxyReaderConfig == null) throw new System.Exception($"{Name()}: missing self-user PCSelfConfig.ProxyReaderConfig config");
                    pcReader = new ProxyReader(ProxyReaderConfig.localIP, ProxyReaderConfig.port, PCSelfConfig.voxelSize, PCSelfConfig.frameRate, selfPreparerQueue, encoderQueue);
                    break;
                case UserRepresentationType.__PCC_SYNTH__:
                    int nPoints = 0;
                    var SynthReaderConfig = PCSelfConfig.SynthReaderConfig;
                    if (SynthReaderConfig != null) nPoints = SynthReaderConfig.nPoints;
                    pcReader = new AsyncSyntheticReader(PCSelfConfig.frameRate, nPoints, selfPreparerQueue, encoderQueue);
                    break;
                case UserRepresentationType.__PCC_PRERECORDED__:
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
       
            reader = pcReader;

            if (!preview)
            {
                // Which encoder do we want?
                string pointcloudCodec = CwipcConfig.Instance.Codec;
                // For TCP we want short queues and we want them leaky (so we don't hang)
                bool leakyQueues = Config.Instance.protocolType == Config.ProtocolType.TCP;
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
                switch(pointcloudCodec)
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
                switch(Config.Instance.protocolType)
                {
                    case Config.ProtocolType.Dash:
                        writer = new AsyncB2DWriter(user.sfuData.url_pcc, "pointcloud", pointcloudCodec, PCSelfConfig.Bin2Dash.segmentSize, PCSelfConfig.Bin2Dash.segmentLife, outgoingStreamDescriptions);
                        break;
                    case Config.ProtocolType.TCP:
                        writer = new AsyncTCPWriter(user.userData.userPCurl, pointcloudCodec, outgoingStreamDescriptions);
                        break;
                    case Config.ProtocolType.None:
                    case Config.ProtocolType.SocketIO:
                        writer = new AsyncSocketIOWriter(user, "pointcloud", pointcloudCodec, outgoingStreamDescriptions);
                        break;
                    default:
                        throw new System.Exception($"{Name()}: Unknown protocolType {Config.Instance.protocolType}");
                }
               
#if VRT_WITH_STATS
                Statistics.Output(Name(), $"reader={reader.Name()}, encoder={encoder.Name()}, writer={writer.Name()}, ntile={tilesToTransmit.Length}, nquality={PCSelfConfig.Encoders.Length}, nStream={outgoingStreamDescriptions.Length}");
#endif
            }
        }

        private void _InitForPrerecordedPlayer(Config._User._PCSelfConfig PCSelfConfig)
        {
            var PrerecordedReaderConfig = PCSelfConfig.PrerecordedReaderConfig;
            if (PrerecordedReaderConfig == null || PrerecordedReaderConfig.folder == null)
                throw new System.Exception($"{Name()}: missing PCSelfConfig.PrerecordedReaderConfig.folders");
            var _reader = new PrerecordedPlaybackReader(PrerecordedReaderConfig.folder, 0, PCSelfConfig.frameRate);
            StaticPredictionInformation info = _reader.GetStaticPredictionInformation();
            string[] tileSubdirs = info.tileNames;
            int nTiles = tileSubdirs.Length;
            int nQualities = info.qualityNames.Length;
            if (tileSubdirs == null || tileSubdirs.Length == 0)
            {
                // Untiled. 
                var _prepQueue = _CreateRendererAndPreparer();
                _reader.Add(null, _prepQueue);
            }
            else
            {
                int curTile = 0;
                foreach (var tileFolder in tileSubdirs)
                {
                    var _prepQueue = _CreateRendererAndPreparer(curTile);
                    _reader.Add(tileFolder, _prepQueue);
                    curTile++;
                }

            }
            reader = _reader;
            //
            // Initialize tiling configuration. We invent this, but it has the correct number of tiles
            // and the correct number of qualities, and the qualities are organized so that earlier
            // ones have lower utility and lower bandwidth than later ones.
            //
            Cwipc.PointCloudTileDescription[] tileInfos = _reader.getTiles();
            if (tileInfos.Length != nTiles)
            {
                Debug.LogError($"{Name()}: Inconsistent number of tiles: {tileInfos.Length} vs {nTiles}");
            }
            networkTileDescription = new PointCloudNetworkTileDescription();
            networkTileDescription.tiles = new PointCloudNetworkTileDescription.NetworkTileInformation[nTiles];
            for (int i = 0; i < nTiles; i++)
            {
                // Initialize per-tile information
                var ti = new PointCloudNetworkTileDescription.NetworkTileInformation();
                networkTileDescription.tiles[i] = ti;
                ti.orientation = tileInfos[i].normal;
                ti.qualities = new PointCloudNetworkTileDescription.NetworkTileInformation.NetworkQualityInformation[nQualities];
                for (int j = 0; j < nQualities; j++)
                {
                    ti.qualities[j] = new PointCloudNetworkTileDescription.NetworkTileInformation.NetworkQualityInformation();
                    //
                    // Insert bullshit numbers: every next quality takes twice as much bandwidth
                    // and is more useful than the previous one
                    //
                    ti.qualities[j].bandwidthRequirement = 10000 * Mathf.Pow(2, j);
                    ti.qualities[j].representation = (float)j / (float)nQualities;
                }
            }

        }

        private void _CreateDescriptionsForOutgoing(Cwipc.PointCloudTileDescription[] tilesToTransmit, Config._User._PCSelfConfig._Encoder[] Encoders, bool leakyQueues)
        {
            int[] octreeBitsArray = new int[Encoders.Length];
            for (int i=0; i<Encoders.Length; i++)
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

        private void _InitForOtherUser()
        {
            // Dump tiles/qualities/bandwidth, for debugging.
            for (int tileNum = 0; tileNum < networkTileDescription.tiles.Length; tileNum++)
            {
                var tile = networkTileDescription.tiles[tileNum];
                Debug.Log($"{Name()}: xxxjack tile {tileNum}: #qualities: {tile.qualities.Length}");
                foreach (var quality in tile.qualities)
                {
                    Debug.Log($"{Name()}: xxxjack tile {tileNum} quality: representation {quality.representation} bandwidth {quality.bandwidthRequirement}");
                }
            }
            
            //
            // Create the right number of rendering pipelines
            //

            IncomingTileDescription[] tilesToReceive = StreamSupport.CreateIncomingTileDescription(networkTileDescription);
            int nTileToReceive = tilesToReceive.Length;

            string pointcloudCodec = CwipcConfig.Instance.Codec;
            for (int tileIndex = 0; tileIndex < nTileToReceive; tileIndex++)
            {
                //
                // Allocate queues we need for this pipeline
                //
                QueueThreadSafe decoderQueue = new QueueThreadSafe($"PCdecoderQueue-{tileIndex}", pcDecoderQueueSize, true);
                //
                // Create renderer
                //
                QueueThreadSafe preparerQueue = _CreateRendererAndPreparer(tileIndex);
                //
                // Create pointcloud decoder, let it feed its pointclouds to the preparerQueue
                //
                AbstractPointCloudDecoder decoder = _CreateDecoder(pointcloudCodec, decoderQueue, preparerQueue);
                decoders.Add(decoder);
                //
                // And collect the relevant information for the Dash receiver
                //
                tilesToReceive[tileIndex].outQueue = decoderQueue;
#if VRT_WITH_STATS
                Statistics.Output(Name(), $"tile={tileIndex}, tile_number={tilesToReceive[tileIndex].tileNumber}, decoder={decoder.Name()}");
#endif
            };

            switch (Config.Instance.protocolType)
            {
                case Config.ProtocolType.None:
                case Config.ProtocolType.SocketIO:
                    reader = new AsyncSocketIOReader(user, "pointcloud", pointcloudCodec, tilesToReceive);
                    break;
                case Config.ProtocolType.Dash:
                    reader = new AsyncSubPCReader(user.sfuData.url_pcc, "pointcloud", pointcloudCodec, tilesToReceive);
                    break;
                case Config.ProtocolType.TCP:
                    reader = new AsyncTCPPCReader(user.userData.userPCurl, pointcloudCodec, tilesToReceive);
                    break;
                default:
                    throw new System.Exception($"{Name()}: unknown protocolType {Config.Instance.protocolType}");
            }
           
            string synchronizerName = "none";
            if (synchronizer != null && synchronizer.isEnabled())
            {
                synchronizerName = synchronizer.Name();
            }
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"reader={reader.Name()}, synchronizer={synchronizerName}");
#endif
        }

        AbstractPointCloudDecoder _CreateDecoder(string pointcloudCodec, QueueThreadSafe decoderQueue, QueueThreadSafe preparerQueue)
        {
            AbstractPointCloudDecoder decoder = null;
            switch (pointcloudCodec)
            {
                case "cwi0":
                    decoder = new AsyncPCNullDecoder(decoderQueue, preparerQueue);
                    break;
                case "cwi1":
                    decoder = new AsyncPCDecoder(decoderQueue, preparerQueue);
                    break;
                default:
                    throw new System.Exception($"{Name()}: Unknown pointcloudCodec \"{pointcloudCodec}\"");
            }
            return decoder;

        }

        public QueueThreadSafe _CreateRendererAndPreparer(int curTile = -1)
        {
            CwipcConfig PCs = CwipcConfig.Instance;
            if (PCs == null) throw new System.Exception($"{Name()}: missing PCs config");
            QueueThreadSafe preparerQueue = new QueueThreadSafe("PCPreparerQueue", pcPreparerQueueSize, false);
            preparerQueues.Add(preparerQueue);
            AsyncPointCloudPreparer preparer = new AsyncPointCloudPreparer(preparerQueue, PCs.defaultCellSize, PCs.cellSizeFactor);
            preparer.SetSynchronizer(synchronizer); 
            preparers.Add(preparer);
            PointCloudRenderer render = gameObject.AddComponent<PointCloudRenderer>();
            string msg = $"preparer={preparer.Name()}, renderer={render.Name()}";
            if (curTile >= 0)
            {
                msg += $", tile={curTile}";
            }
#if VRT_WITH_STATS
            Statistics.Output(Name(), msg);
#endif
            renderers.Add(render);
            render.SetPreparer(preparer);
            return preparerQueue;
        }


        void OnDestroy()
        {
            reader?.StopAndWait();
            encoder?.StopAndWait();
            foreach (var decoder in decoders)
            {
                decoder?.StopAndWait();
            }
            writer?.StopAndWait();
            foreach (var preparer in preparers)
            {
                preparer?.StopAndWait();
            }
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"finished=1");
#endif
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

        public void SetTilingConfig(PointCloudNetworkTileDescription config)
        {
            if (isSource)
            {
                Debug.LogError($"Programmer error: {Name()}: SetTilingConfig called for pipeline that is a source");
                return;
            }
            if (networkTileDescription.tiles != null && networkTileDescription.tiles.Length > 0)
            {
                //Debug.Log($"{Name()}: xxxjack ignoring second tilingConfig");
                return;
            }
            networkTileDescription = config;
            Debug.Log($"{Name()}: received tilingConfig with {networkTileDescription.tiles.Length} tiles");
           
            _InitForOtherUser();
            _InitTileSelector();
        }

       protected virtual void _InitTileSelector()
        {
            if (tileSelector == null)
            {
                //Debug.LogWarning($"{Name()}: no tileSelector");
                return;
            }
            if (networkTileDescription.tiles == null || networkTileDescription.tiles.Length == 0)
            {
                throw new System.Exception($"{Name()}: Programmer error: _initTileSelector with uninitialized tilingConfig");
            }
            int nTiles = networkTileDescription.tiles.Length;
            int nQualities = networkTileDescription.tiles[0].qualities.Length;
            if (nTiles <= 1 && nQualities <= 1)
            {
                // Only single quality, single tile. Nothing to
                // do for the tile selector, so disable it.
                Debug.Log($"{Name()}: single-tile single-quality, disabling {tileSelector.Name()}");
                tileSelector.gameObject.SetActive(false);
                tileSelector = null;
            }
            // Sanity check: all tiles should have the same number of qualities
            foreach (var t in networkTileDescription.tiles)
            {
                if (t.qualities.Length != nQualities)
                {
                    throw new System.Exception($"{Name()}: All tiles should have same number of qualities");
                }
            }
            Debug.Log($"{Name()}: nTiles={nTiles} nQualities={nQualities}");
            if (nQualities <= 1) return;
            LiveTileSelector ts = (LiveTileSelector)tileSelector;
            if (ts == null)
            {
                Debug.LogError($"{Name()}: tileSelector is not a LiveTileSelector");
            }
            ts?.Init(this, networkTileDescription);
        }

        public void SelectTileQualities(int[] tileQualities)
        {
            if (tileQualities.Length != networkTileDescription.tiles.Length)
            {
                Debug.LogError($"{Name()}: SelectTileQualities: {tileQualities.Length} values but only {networkTileDescription.tiles.Length} tiles");
            }
            AsyncPrerecordedBaseReader _prreader = reader as AsyncPrerecordedBaseReader;
            if (_prreader != null)
            {
                _prreader.SelectTileQualities(tileQualities);
                return;
            }
            AsyncSubPCReader _subreader = reader as AsyncSubPCReader;
            if (_subreader != null)
            {
                for (int tileIndex = 0; tileIndex < decoders.Count; tileIndex++)
                {
                    int qualIndex = tileQualities[tileIndex];
                    Debug.Log($"{Name()}: xxxjack +subreader.setTileQualityIndex({tileIndex}, {qualIndex})");
                    _subreader.setTileQualityIndex(tileIndex, qualIndex);
                }
                return;
            }
            AsyncTCPPCReader _tcpreader = reader as AsyncTCPPCReader;
            if (_tcpreader != null)
            {
                for (int tileIndex = 0; tileIndex < decoders.Count; tileIndex++)
                {
                    int qualIndex = tileQualities[tileIndex];
                    Debug.Log($"{Name()}: xxxjack +tcpreader.setTileQualityIndex({tileIndex}, {qualIndex})");
                    _tcpreader.setTileQualityIndex(tileIndex, qualIndex);
                }
                return;
            }
            Debug.LogError($"{Name()}: SelectTileQualities not implemented for reader {reader.Name()}");
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
            VoiceSender voiceSender = gameObject.transform.parent.GetComponentInChildren<VoiceSender>();
            if (voiceSender != null)
            {
                rv.audio = voiceSender.GetSyncInfo();
            }
            Debug.Log($"{Name()}: GetSyncConfig: visual {rv.visuals.wallClockTime}={rv.visuals.streamClockTime}, audio {rv.audio.wallClockTime}={rv.audio.streamClockTime}");
            return rv;
        }

        public new void SetSyncConfig(SyncConfig config)
        {
            if (isSource)
            {
                Debug.LogError($"Programmer error: {Name()}: SetSyncConfig called for pipeline that is a source");
                return;
            }
            if (reader == null) return; // Too early
            Debug.Log($"{Name()}: SetSyncConfig: visual {config.visuals.wallClockTime}={config.visuals.streamClockTime}, audio {config.audio.wallClockTime}={config.audio.streamClockTime}");
            AsyncReader pcReader = reader as AsyncReader;
            if (pcReader != null)
            {
                pcReader.SetSyncInfo(config.visuals);
            }
            else
            {
                Debug.Log($"{Name()}: SetSyncConfig: reader is not a BaseReader");
            }
            // The voice sender object is nested in another object on our parent object, so getting at it is difficult:
            VoiceReceiver voiceReceiver = gameObject.transform.parent.GetComponentInChildren<VoiceReceiver>();
            if (voiceReceiver != null)
            {
                voiceReceiver.SetSyncInfo(config.audio);
            } else
            {
                Debug.Log($"{Name()}: SetSyncConfig: no voiceReceiver");
            }
            //Debug.Log($"{Name()}: xxxjack SetSyncConfig: visual {config.visuals.wallClockTime}={config.visuals.streamClockTime}, audio {config.audio.wallClockTime}={config.audio.streamClockTime}");

        }

        public new float GetBandwidthBudget()
        {
            return 999999.0f;
        }
    }
}