namespace VRT.Core
{
    /// <summary>
    /// The visual representation of a user.
    /// </summary>
    public enum UserRepresentationType
    {
        /// <summary>
        /// No representation, spectator/listener only.
        /// </summary>
        NoRepresentation,
        /// <summary>
        /// 2D video (WebCam) stream, usually shown as an avatar with a screen.
        /// </summary>
        VideoAvatar,
        /// <summary>
        /// Default avatar.
        /// </summary>
        SimpleAvatar,
        /// <summary>
        /// Point cloud (from Realsense camera)
        /// </summary>
        Old__PCC_CWI_,
        /// <summary>
        /// Point cloud (from Kinect camera)
        /// </summary>
        Old__PCC_CWIK4A_,
        /// <summary>
        /// Point cloud (from remote RGBD camera via proxy)
        /// </summary>
        Old__PCC_PROXY__,
        /// <summary>
        /// Synthetic point cloud
        /// </summary>
        Old__PCC_SYNTH__,
        /// <summary>
        /// Prerecorded point cloud
        /// </summary>
        Old__PCC_PRERECORDED__,
        /// <summary>
        /// User without visual representation but with audio feed
        /// </summary>
        AudioOnly,
        /// <summary>
        /// Special spectator that records view (possibly retransmitting as a web video feed)
        /// </summary>
        NoRepresentationCamera,
        /// <summary>
        /// Application-defined alternative representation
        /// </summary>
        AppDefinedRepresentationOne,
        /// <summary>
        /// Application-defined alternative representation
        /// </summary>
        AppDefinedRepresentationTwo
    }
}