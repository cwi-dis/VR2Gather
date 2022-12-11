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


    public class PointCloudPipelineOther : PointCloudPipelineBase
    {

        protected override void _InitForPrerecordedPlayer(Config._User._PCSelfConfig PCSelfConfig)
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

      

        protected override void _InitForOtherUser()
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
            }
            else
            {
                Debug.Log($"{Name()}: SetSyncConfig: no voiceReceiver");
            }
            //Debug.Log($"{Name()}: xxxjack SetSyncConfig: visual {config.visuals.wallClockTime}={config.visuals.streamClockTime}, audio {config.audio.wallClockTime}={config.audio.streamClockTime}");

        }
    }

}

