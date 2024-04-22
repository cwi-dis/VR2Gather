using System;
using UnityEngine;
using static Cwipc.StreamSupport;

namespace Cwipc
{
    /// <summary>
    /// Structures and methods used to help implementing streaming pointclouds (and other media)
    /// across the net.
    /// Includes methods to convert the various representations of tiling information into each other (insofar as applicable).
    /// </summary>
    public class StreamSupport
    {
        /// <summary>
        /// Helper method to convert 4 characters into a 32-bit 4CC.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        static public uint VRT_4CC(char a, char b, char c, char d)
        {
            return (uint)(a << 24 | b << 16 | c << 8 | d);
        }

        /// <summary>
        /// Structure describing a single outgoing stream to an AsyncWriter.
        /// This includes the information that needs to go into a DASH manifest (or other stream
        /// description).
        /// Can really only be used for tiled pointclouds.
        /// </summary>
        [Serializable]
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
        /// Structure describing the parameters of a single encoder (possibly part of a multi-tile, multi-quality
        /// encoder group). This structure is only valid for the "cwi1" encoder, also known as the MPEG Anchor codec.
        /// </summary>
        [Serializable]
        public struct EncoderStreamDescription
        {
            /// <summary>
            /// Encoder parameter. Depth of the octree used during encoding. Compressed pointcloud will have at most 8**octreeBits points.
            /// </summary>
            public int octreeBits;
            /// <summary>
            /// Tile number to filter pointcloud on before encoding. 0 means no filtering.
            /// </summary>
            public int tileFilter;
            /// <summary>
            /// Output queue for this encoder, will usually be shared with the corresponding transmitter (as its input queue).
            /// </summary>
            public QueueThreadSafe outQueue;
        }

        /// <summary>
        /// Structure describing a single incoming (tiled, single quality) stream.
        /// Note: implementation detail for DASH support.
        /// </summary>
        [Serializable]
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
        [Serializable]
        public struct IncomingTileDescription
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
            /// Streams used for this tile (for its multiple qualities).
            /// Note: implementation detail for DASH support, cal be null.
            /// </summary>
            public IncomingStreamDescription[] streamDescriptors;
        }

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

        /// <summary>
        /// Create a PointCloudNetworkTileDescription from a PointCloudTileDescription array and an octreeBits array.
        /// </summary>
        /// <param name="tilesToTransmit"></param>
        /// <param name="octreeBitsArray"></param>
        /// <returns></returns>
        static public PointCloudNetworkTileDescription CreateNetworkTileDescription(Cwipc.PointCloudTileDescription[] tilesToTransmit, int[] octreeBitsArray)
        {
            int nTileToTransmit = tilesToTransmit.Length;
            int minTileNum = nTileToTransmit == 1 ? 0 : 1;
            int nQuality = octreeBitsArray.Length;
            int nStream = nQuality * nTileToTransmit;
            //
            // Create all three sets of descriptions needed.
            //
            PointCloudNetworkTileDescription networkTileDescription = new PointCloudNetworkTileDescription()
            {
                tiles = new PointCloudNetworkTileDescription.NetworkTileInformation[nTileToTransmit]
            };
            for (int tileNum = 0; tileNum < nTileToTransmit; tileNum++)
            {
                var tileOrientation = tilesToTransmit[tileNum].normal;
                networkTileDescription.tiles[tileNum].orientation = tileOrientation;
                networkTileDescription.tiles[tileNum].qualities = new PointCloudNetworkTileDescription.NetworkTileInformation.NetworkQualityInformation[nQuality];
                for (int qualityNum = 0; qualityNum < nQuality; qualityNum++)
                {
                    int streamNum = tileNum * nQuality + qualityNum;
                    int octreeBits = octreeBitsArray[qualityNum];
                   
                    //
                    // Invent a description of tile/quality bandwidth requirements and visual quality.
                    //
                    networkTileDescription.tiles[tileNum].qualities[qualityNum].bandwidthRequirement = octreeBits * octreeBits * octreeBits; // xxxjack
                    networkTileDescription.tiles[tileNum].qualities[qualityNum].representation = (float)octreeBits / 20; // guessing octreedepth of 20 is completely ridiculously high
                }
            }
            return networkTileDescription;
        }

        /// <summary>
        /// Create a OutgoingStreamDescription array from a PointCloudTileDescription array and an octreeBits array.
        /// </summary>
        /// <param name="tilesToTransmit"></param>
        /// <param name="octreeBitsArray"></param>
        /// <returns></returns>
        static public OutgoingStreamDescription[] CreateOutgoingStreamDescription(Cwipc.PointCloudTileDescription[] tilesToTransmit, int[] octreeBitsArray)
        {
            int nTileToTransmit = tilesToTransmit.Length;
            int minTileNum = 0; // nTileToTransmit == 1 ? 0 : 1;
            int nQuality = octreeBitsArray.Length;
            int nStream = nQuality * nTileToTransmit;
            //
            // Create all three sets of descriptions needed.
            //
            OutgoingStreamDescription[] outgoingStreamDescriptions = new OutgoingStreamDescription[nStream];
         
            for (int tileNum = 0; tileNum < nTileToTransmit; tileNum++)
            {
                var tileOrientation = tilesToTransmit[tileNum].normal;
                for (int qualityNum = 0; qualityNum < nQuality; qualityNum++)
                {
                    int streamNum = tileNum * nQuality + qualityNum;
                    int octreeBits = octreeBitsArray[qualityNum];
                   
                    outgoingStreamDescriptions[streamNum] = new OutgoingStreamDescription
                    {
                        tileNumber = (uint)(tileNum + minTileNum),
                        // quality = (uint)(100 * octreeBits + 75),
                        qualityIndex = qualityNum,
                        orientation = tileOrientation,
                    };
                }
            }
            return outgoingStreamDescriptions;
        }

        /// <summary>
        /// Create a EncoderStreamDescription array from a PointCloudTileDescription array and an octreeBits array.
        /// </summary>
        /// <param name="tilesToTransmit"></param>
        /// <param name="octreeBitsArray"></param>
        /// <returns></returns>
        static public EncoderStreamDescription[] CreateEncoderStreamDescription(Cwipc.PointCloudTileDescription[] tilesToTransmit, int[] octreeBitsArray)
        {
            int nTileToTransmit = tilesToTransmit.Length;
            int minTileNum = nTileToTransmit == 1 ? 0 : 1;
            int nQuality = octreeBitsArray.Length;
            int nStream = nQuality * nTileToTransmit;
            //
            // Create all three sets of descriptions needed.
            //
            EncoderStreamDescription[] encoderStreamDescriptions = new EncoderStreamDescription[nStream];
            for (int tileNum = 0; tileNum < nTileToTransmit; tileNum++)
            {
                var tileOrientation = tilesToTransmit[tileNum].normal;
                for (int qualityNum = 0; qualityNum < nQuality; qualityNum++)
                {
                    int streamNum = tileNum * nQuality + qualityNum;
                    int octreeBits = octreeBitsArray[qualityNum];
                    encoderStreamDescriptions[streamNum] = new EncoderStreamDescription
                    {
                        octreeBits = octreeBits,
                        tileFilter = tileNum + minTileNum,
                    };
                }
            }
            return encoderStreamDescriptions;
        }

        /// <summary>
        /// Create an IncomingTileDescription array from a NetworkTileDescription.
        /// </summary>
        /// <param name="networkTileDescription"></param>
        /// <returns></returns>
        static public IncomingTileDescription[] CreateIncomingTileDescription(PointCloudNetworkTileDescription networkTileDescription)
        {
            //
            // At some stage we made the decision that tilenumer 0 represents the whole untiled pointcloud.
            // So if we receive an untiled stream we want tile 0 only, and if we receive a tiled stream we
            // never want tile 0.
            //
            int nTileToReceive = networkTileDescription.tiles.Length;
            int minTileNumber = networkTileDescription.tiles.Length == 1 ? 0 : 1;

            //
            // Create the right number of rendering pipelines
            //

            IncomingTileDescription[] tilesToReceive = new IncomingTileDescription[nTileToReceive];

            for (int tileIndex = 0; tileIndex < nTileToReceive; tileIndex++)
            {

                //
                // And collect the relevant information for the Dash receiver
                //
                tilesToReceive[tileIndex] = new IncomingTileDescription()
                {
                    tileNumber = tileIndex + minTileNumber,
                    name = $"tile-{tileIndex}",
                };
            }
            return tilesToReceive;
        }
    }
}