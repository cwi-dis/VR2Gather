using Cwipc;
using UnityEditor.UI;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    using PointCloudNetworkTileDescription = Cwipc.StreamSupport.PointCloudNetworkTileDescription;

    public class LiveTileSelector : BaseTileSelector
    {

        //
        // Temporary variable (should be measured from internet connection): total available bitrate for this run.
        //
        [Tooltip("If non-zero: don't measure the bitrate budget but use this number")]
        public double bitRatebudget = 1000000;
        //
        // For live: we precompute the bandwidth usage matrix based on the reported
        // figures in the tiling configuration. It's all guesswork for now.
        //
        double[][] guessedBandwidthUsageMatrix;
        
        public void Init(PointCloudPipelineOther _prerecordedPointcloud, PointCloudNetworkTileDescription _tilingConfig)
        {

            pipeline = _prerecordedPointcloud;
            nTiles = _tilingConfig.tiles.Length;
            nQualities = _tilingConfig.tiles[0].qualities.Length;
            Debug.Log($"{Name()}: nQualities={nQualities}, nTiles={nTiles}");
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"nQualities={nQualities}, nTiles={nTiles}");
#endif
            TileOrientation = new Vector3[nTiles];
            for (int ti = 0; ti < nTiles; ti++)
            {
                TileOrientation[ti] = _tilingConfig.tiles[ti].orientation;
            }
            guessedBandwidthUsageMatrix = new double[nTiles][];
            for (int ti = 0; ti < nTiles; ti++)
            {
                guessedBandwidthUsageMatrix[ti] = new double[nQualities];
                for (int qi = 0; qi < nQualities; qi++)
                {
                    guessedBandwidthUsageMatrix[ti][qi] = _tilingConfig.tiles[ti].qualities[qi].bandwidthRequirement;
                }
            }
        }

        new public void Start()
        {
            base.Start();
            var settings = VRTConfig.Instance.TileSelector;
            if (settings.bitrateBudget != 0) {
                bitRatebudget = settings.bitrateBudget;
            }
        }
        

        protected override double[][] getBandwidthUsageMatrix(long currentFrameNumber)
        {
            return guessedBandwidthUsageMatrix;
        }

        protected override double getBitrateBudget()
        {
            return bitRatebudget;
        }
        protected override long getCurrentFrameIndex()
        {
            //Debug.LogError($"{Name()}: getCurrentFrameIndex not yet implemented");
            return 0; // xxxjack
        }

    }
}