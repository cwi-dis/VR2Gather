using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using EncoderStreamDescription = StreamSupport.EncoderStreamDescription;
    using OutgoingStreamDescription = StreamSupport.OutgoingStreamDescription;


    /// <summary>
    /// Subclass of PointCloudPipelineSimple that not only displays a pointcloud stream
    /// (for self view) but also transmits it (so others can see you).
    /// </summary>
    public class PointCloudSelfPipelineSimple : PointCloudPipelineSimple
    {
        [Header("Transmission settings")]
        [Tooltip("Transmitter to use (if any)")]
        [SerializeField] protected AbstractPointCloudSink _transmitter;
        public override AbstractPointCloudSink transmitter { get { return _transmitter; } }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
