using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
    /// <summary>
    /// Structure describing the available tiles, and what representations are available
    /// for each tile, how "good" each representation is and how much bandwidth it uses.
    /// </summary>
    [Serializable]
    public struct PointCloudNetworkTileDescription
    {
        /// <summary>
        /// Structure describing a single tile.
        /// </summary>
        [Serializable]
        public struct NetworkTileInformation
        {
            /// <summary>
            /// Orientation of the tile, relative to the centroid of the whole pointcloud.
            /// (0,0,0) for directionless.
            /// </summary>
            public Vector3 orientation;
            /// <summary>
            /// Structure describing a single stream (within a tile).
            /// </summary>
            [Serializable]
            public struct NetworkQualityInformation
            {
                /// <summary>
                /// Indication of how much bandwidth this stream requires.
                /// </summary>
                public float bandwidthRequirement;
                /// <summary>
                /// Indication of how "good" this stream is, visually. 0.0 is worst
                /// quality, 1.0 is best quality. 
                /// </summary>
                public float representation;
            };
            /// <summary>
            /// Streams available for this tile (at various quality levels)
            /// </summary>
            public NetworkQualityInformation[] qualities;
        };
        /// <summary>
        /// All tiles for this aggregate pointcloud stream.
        /// </summary>
        public NetworkTileInformation[] tiles;
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