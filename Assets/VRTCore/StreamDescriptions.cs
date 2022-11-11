using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;

namespace VRT.Core
{
    public class StreamSupport
    {
        static public uint VRT_4CC(char a, char b, char c, char d)
        {
            return (uint)(a << 24 | b << 16 | c << 8 | d);
        }
    }

    /// <summary>
    /// Structure describing a single outgoing (tile) stream.
    /// Can really only be used for tiled pointclouds.
    /// </summary>
    public struct OutgoingStreamDescription
    {
        /// <summary>
        /// Name, for debugging and for Dash manifest
        /// </summary>
        public string name;
        /// <summary>
        /// Index of this tile stream.
        /// </summary>
        public uint tileNumber;
        /// <summary>
        /// Index of the quality, for multi-quality streams.
        /// </summary>
        public int qualityIndex;
        /// <summary>
        /// Indication of the relative direction this tile points (relative to the pointcloud centroid)
        /// </summary>
        public Vector3 orientation;
        /// <summary>
        /// The queue on which the producer will produce pointclouds for this writer to transmit.
        /// </summary>
        public QueueThreadSafe inQueue;
    }

    /// <summary>
	/// Structure describing a single incoming (tiled, single quality) stream.
	/// Can really only be used for pointclouds.
	/// </summary>
    public struct IncomingStreamDescription
    {
        /// <summary>
		/// Index of the stream (in the underlying TCP or Dash protocol)
		/// </summary>
        public int streamIndex;
        /// <summary>
		/// Tile number for the pointclouds received on this stream.
		/// </summary>
        public int tileNumber;
        /// <summary>
        /// Indication of the relative direction this tile points (relative to the pointcloud centroid)
        /// </summary>
        public Vector3 orientation;
    }

    /// <summary>
    /// Structure describing a set of multi-quality streams for a single tile.
    /// </summary>
    public struct IncomingTileDescriptor
    {
        /// <summary>
        /// Name of the stream (for Dash manifest and for debugging/statistics printing)
        /// </summary>
        public string name;
        /// <summary>
        /// The queue on which frames for this stream will be deposited
        /// </summary>
        public QueueThreadSafe outQueue;
        /// <summary>
        /// Index of the tile
        /// </summary>
        public int tileNumber;
        /// <summary>
        /// Streams used for this tile (for its multiple qualities)
        /// </summary>
        public IncomingStreamDescription[] streamDescriptors;
    }
}