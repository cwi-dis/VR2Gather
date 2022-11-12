using System;
using UnityEngine;

namespace Cwipc
{
    /// <summary>
    /// Configuration variables, in a way that allows saving to json.
    /// </summary>
    [Serializable]
    public class CwipcConfig
    {
        /// <summary>
        /// Codec for pointclouds. cwi1 is MPEG Anchor codec, cwi0 is uncompressed
        /// </summary>
        public string Codec = "cwi1";
        /// <summary>
        /// Cell size for pointclouds received or read that how no explicit cellsize.
        /// </summary>
        public float defaultCellSize;
        /// <summary>
        /// Multiply cellSize of pointclouds by this factor before rendering.
        /// </summary>
        public float cellSizeFactor;
        /// <summary>
        /// For debugging colorize each received pointcloud a little bit.
        /// </summary>
        public bool debugColorize;
        /// <summary>
        /// If no pointclouds are received for this many seconds the pointcloud will be ghosted (displayed with a much smaller pointsize)
        /// </summary>
        public float timeoutBeforeGhosting = 5.0f;
        /// <summary>
        /// If non-zero sets decoder queue size.
        /// </summary>
        public int decoderQueueSizeOverride = 0;
        /// <summary>
        /// If non-zero sets preparer queue size.
        /// </summary>
        public int preparerQueueSizeOverride = 0;
        /// <summary>
        /// If non-zero sets how many threads can be used in the encoder.
        /// </summary>
        public int encoderParallelism = 0;
        /// <summary>
        /// If non-zero sets how many threads can be used in the decoder.
        /// </summary>
        public int decoderParallelism = 0;

        //
        // Helper code for singleton access, and for loading config from VR2Gather config.json file.
        static CwipcConfig _Instance;
        public static CwipcConfig Instance
        {
            get
            {
                if (_Instance == null) _Instance = new CwipcConfig();
                return _Instance;
            }
        }

        public static void SetInstance(CwipcConfig newInstance)
        {
            _Instance = newInstance;
        }
    };
}

