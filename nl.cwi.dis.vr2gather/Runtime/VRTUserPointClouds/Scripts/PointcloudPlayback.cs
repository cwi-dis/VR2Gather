using Cwipc;
using UnityEngine;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
using VRT.Pilots.Common;
using static Cwipc.StreamSupport;

namespace VRT.UserRepresentation.PointCloud
{
    public class PointCloudPlayback : PointCloudPipelineOther
    {
        [Tooltip("Overrides PrerecordedReaderConfig setting: directory to read")]
        public string folder;
        [Tooltip("Overrides PrerecordedReaderConfig setting: per-tile subfolders")]
        public string[] tiles;
        [Tooltip("Overrides PrerecordedReaderConfig setting: per-quality subfolders")]
        public string[] qualities;
        [Tooltip("Read .ply files in stead of .cwipcdump files")]
        public bool ply;
        [Tooltip("Prefer best quality in stead of worst quality")]
        public bool preferBest;
        User dummyUser;
        VRTConfig._User cfg;


        /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
        /// <param name="cfg"> Config file json </param>
        /// <param name="url_pcc"> The url for pointclouds from sfuData of the Orchestrator </param> 
        /// <param name="url_audio"> The url for audio from sfuData of the Orchestrator </param>
        /// <param name="calibrationMode"> Bool to enter in calib mode and don't encode and send your own PC </param>
        public override BasePipeline Init(bool isLocalPlayer, object _user, VRTConfig._User cfg, bool preview = false)
        {
            if (isLocalPlayer)
            {
                Debug.LogError("${Name()}: Init() called with isLocalPlayer==true");
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
            _InitForPrerecordedPlayer(cfg.PCSelfConfig);
            
            return this;
        }

        protected void _InitForPrerecordedPlayer(VRTConfig._User._PCSelfConfig PCSelfConfig)
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
            reader = (IAsyncReader)_reader;
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

        public void Awake()
        {

            dummyUser = new User();
            dummyUser.userData = new UserData();
            dummyUser.userData.userRepresentationType = UserRepresentationType.PointCloud;

            VRTConfig._User realUser = VRTConfig.Instance.LocalUser;

            cfg = new VRTConfig._User();
            cfg.PCSelfConfig = new VRTConfig._User._PCSelfConfig();
            cfg.PCSelfConfig.capturerType = VRTConfig._User._PCSelfConfig.PCCapturerType.prerecorded;
            cfg.PCSelfConfig.PrerecordedReaderConfig = new VRTConfig._User._PCSelfConfig._PrerecordedReaderConfig();
            if (folder == null || folder == "")
            {
                folder = realUser.PCSelfConfig.PrerecordedReaderConfig.folder;
#if WITH_QUALITY_ASSESMENT
                tiles = realUser.PCSelfConfig.PrerecordedReaderConfig.tiles;
                qualities = realUser.PCSelfConfig.PrerecordedReaderConfig.qualities;
                ply = realUser.PCSelfConfig.PrerecordedReaderConfig.ply;
                preferBest = realUser.PCSelfConfig.PrerecordedReaderConfig.preferBest;
#endif
            }
            //Debug.Log($"{Name()}: folder={folder} ply={ply} {tiles.Length} tiles, {qualities.Length} qualities, preferBest={preferBest}");
            cfg.PCSelfConfig.PrerecordedReaderConfig.folder = folder;
#if WITH_QUALITY_ASSESMENT
            cfg.PCSelfConfig.PrerecordedReaderConfig.tiles = tiles;
            cfg.PCSelfConfig.PrerecordedReaderConfig.qualities = qualities;
            cfg.PCSelfConfig.PrerecordedReaderConfig.ply = ply;
            cfg.PCSelfConfig.PrerecordedReaderConfig.preferBest = preferBest;
#endif
            cfg.PCSelfConfig.frameRate = realUser.PCSelfConfig.frameRate;
            try
            {
                Init(false, dummyUser, cfg, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{Name()}: initialize: Exception: {e.Message} Stack: {e.StackTrace}");
                throw;
            }
            _InitTileSelector();
            
        }
        protected override void _InitTileSelector()
        {
            if (tileSelector == null)
            {
                Debug.LogWarning($"{Name()}: no tileSelector");
                return;
            }
            if (tiles == null)
            {
                // Untiled: disable tile selector
                Debug.Log($"{Name()}: untiled, disabling {tileSelector.Name()}");
                tileSelector.gameObject.SetActive(false);
                tileSelector = null;
                return;
            }
            int nQualities = qualities.Length;
            int nTiles = tiles.Length;
            Debug.Log($"{Name()}: nTiles={nTiles} nQualities={nQualities}");
            if (nQualities <= 1) return;
            if (tileSelector == null)
            {
                Debug.LogWarning($"{Name()}: no tileSelector");
            }
            var ts = tileSelector as PrerecordedTileSelector;
            ts?.Init(this, nQualities, nTiles, null);
        }
    }
}
