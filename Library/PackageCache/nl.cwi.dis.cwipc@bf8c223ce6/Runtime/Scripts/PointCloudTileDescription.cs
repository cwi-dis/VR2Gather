using System;
using UnityEngine;

namespace Cwipc
{
    /// <summary>
    /// Structure defining a pointcloud tile.
    /// </summary>
    [Serializable]
    public struct PointCloudTileDescription
    {
        /// <summary>
        /// Direction of this tile, as seen from the centroid of the pointcloud.
        /// </summary>
        public Vector3 normal;
        /// <summary>
        /// Name (or serial number, or other identifier) of the camera that created this tile.
        /// </summary>
        public string cameraName;
        /// <summary>
        /// 8-bit bitmask representing this tile in each point.
        /// </summary>
        public int cameraMask;
    }

    /// <summary>
    /// Interface provided by classes that represent tiled pointcloud sources.
    /// </summary>
    public interface ITileDescriptionProvider
    {
        /// <summary>
        /// Get description of the tiles produced by this source.
        /// Returns null if this instance of the object does not provide tiled
        /// pointclouds.
        /// </summary>
        /// <returns>Array of tile descriptions</returns>
        public PointCloudTileDescription[] getTiles();
    }
}

