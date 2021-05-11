using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
    [Serializable]
    public struct TilingConfig
    {
        [Serializable]
        public struct TileInformation
        {
            public Vector3 orientation;     // relative to current position. (0,0,0) for directionless
            [Serializable]
            public struct QualityInformation
            {
                public float bandwidthRequirement;     // How much bandwidth will this quality use?
                public float representation;     // 0.0 is worst (nothing) 1.0 is best/
            };
            public QualityInformation[] qualities;     // At which qualities is this tile available?
        };
        public TileInformation[] tiles;
    };

    // Used only for Prerecorded: information allowing the tile selector to find
    // the files with the bandwidth prediction information.
    // 
    public struct StaticPredictionInformation
    {
        public string baseDirectory;
        public string[] tileNames;
        public string[] qualityNames;
        public string predictionFilename;
    };
   

}