using UnityEngine;


namespace VRT.UserRepresentation.PointCloud
{
  
    public class LiveTileSelector : BaseTileSelector
    {

        //
        // Temporary variable (should be measured from internet connection): total available bitrate for this run.
        //
        public double bitRatebudget = 1000000;
       
        TilingConfig tilingConfig;

        string Name()
        {
            return "LiveTileSelector";
        }


        public void Init(PointCloudPipeline _pipeline, int _nQualities, int _nTiles, TilingConfig? _tilingConfig)
        {
            if (_tilingConfig == null)
            {
                throw new System.Exception($"{Name()}: Must have tilingConfig argument");
            }
            tilingConfig = (TilingConfig)_tilingConfig;
            pipeline = _pipeline;
            nQualities = _nQualities;
            Debug.Log($"{Name()}: PrerecordedTileSelector nQualities={nQualities}, nTiles={nTiles}");
            nTiles = _nTiles;
            TileOrientation = new Vector3[_nTiles];
            for (int i = 0; i < _nTiles; i++)
            {
                TileOrientation[i] = tilingConfig.tiles[i].orientation;
            }
        }

        protected override double[][] getBandwidthUsageMatrix(long currentFrameNumber)
        {
            if (currentFrameNumber != 0)
            {
                Debug.LogError($"{Name()}: Programmer error: currentFrameNumber={currentFrameNumber}");
            }
            int nTiles = tilingConfig.tiles.Length;
            if (nTiles == 0) return null; // Not yet initialized
            int nQualities = tilingConfig.tiles[0].qualities.Length;
            double[][] bandwidthUsageMatrix = new double[nTiles][];
            for (int ti = 0; ti < nTiles; ti++)
            {
                var thisTile = tilingConfig.tiles[ti];
                var thisBandwidth = new double[nQualities];
                for (int qi = 0; qi < nQualities; qi++)
                {
                    var thisQuality = thisTile.qualities[qi];
                    thisBandwidth[qi] = thisQuality.bandwidthRequirement;
                }

                bandwidthUsageMatrix[ti] = thisBandwidth;
            }
            return bandwidthUsageMatrix;
        }
        protected override double getBitrateBudget()
        {
            return bitRatebudget;
        }
        protected override long getCurrentFrameIndex()
        {
            return 0;
        }


        protected override Vector3 getCameraForward()
        {
            // xxxjack currently returns camera viedw angle (as the name implies)
            // but maybe camera position is better. Or both.
            var cam = FindObjectOfType<Camera>().gameObject;
            if (cam == null)
                Debug.LogError("Camera not found!");
            //Debug.Log("<color=red> Camera Transform </color>" + cameraForward.x + " " + cameraForward.y + " " + cameraForward.z);
            Transform cameraTransform = cameraTransform = cam.transform;
            return cameraTransform.forward;

        }

        protected override Vector3 getPointcloudPosition(long currentFrameNumber)
        {
            return new Vector3(0, 0, 0);
        }


    }
}