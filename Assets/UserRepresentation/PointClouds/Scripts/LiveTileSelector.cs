using UnityEngine;
using VRT.Pilots.Common;

namespace VRT.UserRepresentation.PointCloud
{
    using PointCloudNetworkTileDescription = Cwipc.StreamSupport.PointCloudNetworkTileDescription;

    public class LiveTileSelector : BaseTileSelector
    {

        //
        // Temporary variable (should be measured from internet connection): total available bitrate for this run.
        //
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

        protected override Vector3 getCameraForward()
        {
            // xxxjack currently returns camera viedw angle (as the name implies)
            // but maybe camera position is better. Or both.
            PlayerControllerSelf player = gameObject.GetComponentInParent<PlayerControllerSelf>();
            Transform cameraTransform = player?.getCameraTransform();
            if (cameraTransform == null)
                Debug.LogError($"{Name()}: Camera not found");
            return cameraTransform.forward;

        }

        protected override Vector3 getPointCloudPosition(long currentFrameNumber)
        {
            return new Vector3(0, 0, 0);
        }
    }
}