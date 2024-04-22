using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using EncoderStreamDescription = StreamSupport.EncoderStreamDescription;
    using OutgoingStreamDescription = StreamSupport.OutgoingStreamDescription;

    /// <summary>
    /// Subclass of PointCloudPipelineSimple that not only displays a pointcloud stream
    /// (for self view) but also transmits it (so others can see you). Transmission may be tiled.
    /// This is a subclass of PointCloudPipelineSimple because the self-view is never tiled: the whole
    /// pointcloud is always shown. The only thing that is (optionally) tiled is the transmission.
    ///
    /// Both transmission and self-view can be disabled (before Start() is called), for example if you have a use case that does
    /// not need either of them.
    ///
    /// </summary>
    public class PointCloudSelfPipelineTiled : PointCloudPipelineSimple
    {
        [Header("Self-view settings")]
        [Tooltip("Enable pointcloud display")]
        [SerializeField] protected bool _enableSelfView = true;
        protected override bool enableOutput { get { return _enableSelfView; } }
        [Header("Transmission settings")]
        [Tooltip("Transmitter to use (if any)")]
        [SerializeField] protected AbstractPointCloudSink _transmitter;
        public override AbstractPointCloudSink transmitter { get { return _transmitter; } }
        [Tooltip("Enable transmission")]
        [SerializeField] bool enableTransmission = true;
        [Tooltip("Force untiled")]
        [SerializeField] bool _forceUntiled = false;
        public override bool forceUntiled
        {
            set { _forceUntiled = value; }
            get { return _forceUntiled; }
        }

        protected override QueueThreadSafe InitializeTransmitterQueue()
        {
            if (!enableTransmission) return null;
            //
            // Create queue from reader to encoder.
            // Iis declared in our base class, and will be picked up by its
            // Initialize method.
            //
            return new QueueThreadSafe("ReaderEncoderQueue", 2, true);
        }


        void InitializeEncoder(bool isWebRTC)
        {

           string fourcc = compressedInputStream ? "cwi1" : "cwi0";
            RendererInputQueue = new QueueThreadSafe("DecoderOutputQueue", 2, false);
            if (isWebRTC)
            {
                PCreceiver = new AsyncWebRTCReader(inputUrl, clientId, fourcc, ReaderRenderQueue);
            }
            else
            {
                PCreceiver = new AsyncTCPReader(inputUrl, fourcc, ReaderRenderQueue);
            }
            if (compressedInputStream)
            {
                PCdecoder = new AsyncPCDecoder(ReaderRenderQueue, RendererInputQueue);
            }
            else
            {
                PCdecoder = new AsyncPCNullDecoder(ReaderRenderQueue, RendererInputQueue);
            }

        }

     
        // Update is called once per frame
        void Update()
        {

        }
    }
}
