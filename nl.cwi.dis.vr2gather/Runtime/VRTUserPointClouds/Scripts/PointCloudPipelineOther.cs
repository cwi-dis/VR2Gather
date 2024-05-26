using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using VRT.UserRepresentation.Voice;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Transport.TCP;
using VRT.Orchestrator.Wrapping;
using Cwipc;
using VRT.Pilots.Common;
using UnityEngine.Rendering;

namespace VRT.UserRepresentation.PointCloud
{
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;
    using EncoderStreamDescription = Cwipc.StreamSupport.EncoderStreamDescription;
    using PointCloudNetworkTileDescription = Cwipc.StreamSupport.PointCloudNetworkTileDescription;
    using static VRT.Core.VRTConfig._User;


    public class PointCloudPipelineOther : PointCloudPipelineBase
    {
        protected ITransportProtocolReader_Tiled reader;
        
        protected List<AbstractPointCloudDecoder> decoders = new List<AbstractPointCloudDecoder>();

        public static void Register()
        {
            RegisterPipelineClass(false, UserRepresentationType.PointCloud, AddPipelineComponent);
        }

        private static BasePipeline AddPipelineComponent(GameObject dst, UserRepresentationType i)
        {
            return dst.AddComponent<PointCloudPipelineOther>();
        }


        /// <summary> Orchestrator based Init. Start is called before the first frame update </summary> 
        /// <param name="cfg"> Config file json (xxxjack unused should be removed)</param>
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
            SetupConfigDistributors();

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
                    Statistics.Output(Name(), $"self=0, userid={user.userId}");
#endif
            //
            // Determine how many tiles (and therefore decode/render pipelines) we need
            //
            Debug.Log($"{Name()}: delay _InitForOtherUser until tiling information received");
            
            return this;
        }


        protected void _InitForOtherUser()
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

            string pointcloudCodec = SessionConfig.Instance.pointCloudCodec;
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
            // We need some backward-compatibility hacks, depending on protocol type.
            // We need some backward-compatibility hacks, depending on protocol type.
            string url = user.sfuData.url_gen;
            string proto = SessionConfig.Instance.protocolType;
            switch (proto)
            {
                case "tcp":
                    url = user.userData.userAudioUrl;
                    break;
            }
            reader = TransportProtocol.NewReader_Tiled(proto).Init(url, user.userId, "pointcloud", pointcloudCodec, tilesToReceive);

            string synchronizerName = "none";
            if (synchronizer != null && synchronizer.isEnabled())
            {
                synchronizerName = synchronizer.Name();
            }
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"reader={reader.Name()}, codec={pointcloudCodec}, synchronizer={synchronizerName}");
#endif
        }
        new void OnDestroy()
        {
            reader?.StopAndWait();
            foreach (var decoder in decoders)
            {
                decoder?.StopAndWait();
            }
            base.OnDestroy();
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
                Debug.LogWarning($"{Name()}: SetTilingConfig: ignoring second tilingConfig");
                return;
            }
            networkTileDescription = config;
            Debug.Log($"{Name()}: received tilingConfig with {networkTileDescription.tiles.Length} tiles");

            _InitForOtherUser();
            _InitTileSelector();
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
            ts?.Init((PointCloudPipelineOther)this, networkTileDescription);
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
            AsyncDashReader_Tiled _subreader = reader as AsyncDashReader_Tiled;
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
            AsyncTCPDirectReader_Tiled _tcpreader = reader as AsyncTCPDirectReader_Tiled;
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
            VoicePipelineOther voiceReceiver = gameObject.transform.parent.GetComponentInChildren<VoicePipelineOther>();
            if (voiceReceiver != null && voiceReceiver.enabled && voiceReceiver.gameObject.activeInHierarchy)
            {
                voiceReceiver.SetSyncInfo(config.audio);
            }
            else
            {
                Debug.Log($"{Name()}: SetSyncConfig: no voiceReceiver for this player");
            }
            //Debug.Log($"{Name()}: xxxjack SetSyncConfig: visual {config.visuals.wallClockTime}={config.visuals.streamClockTime}, audio {config.audio.wallClockTime}={config.audio.streamClockTime}");

        }
    }

}

