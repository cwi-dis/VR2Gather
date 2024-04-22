using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    /// <summary>
    /// Base class for a MonoBehaviour that is a sink of pointclouds.
    /// A class that transmits pointclouds (possibly after compressing them).
    /// Concrete implementation subclasses for either tiled or untiled streams exist.
    /// </summary>
    abstract public class AbstractPointCloudSink : MonoBehaviour
    {
        public enum SinkType
        {
            TCP,
            WebRTC
        }
        [Tooltip("Type of output sink (protocol)")]
        [SerializeField] public SinkType sinkType;
        [Tooltip("Specifies TCP server to create as sink, in the form tcp://host:port")]
        [SerializeField] public string outputUrl;
        [Tooltip("Insert a compressed pointcloud encoder into the output stream")]
        [SerializeField] public bool compressedOutputStreams;
        [Tooltip("For compressed streams: how many bits in the octree. Higher numbers are higher quality")]
        [SerializeField] protected int defaultOctreeBits;

        protected QueueThreadSafe ReaderEncoderQueue;
        protected AsyncWorker PCencoder;
        protected AsyncWriter PCtransmitter;

        public QueueThreadSafe InitializeTransmitterQueue()
        {
            ReaderEncoderQueue = new QueueThreadSafe("ReaderEncoderQueue", 2, true);
            return ReaderEncoderQueue;
        }

        abstract public void InitializeTransmitter(PointCloudTileDescription[] tileDescriptions);


        protected void OnDestroy()
        {
            PCencoder?.StopAndWait();
            PCtransmitter?.StopAndWait();
        }
    }
}