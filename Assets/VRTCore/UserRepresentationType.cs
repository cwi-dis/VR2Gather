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
        __NONE__,
        /// <summary>
        /// 2D video (WebCam) stream, usually shown as an avatar with a screen.
        /// </summary>
        __2D__,
        /// <summary>
        /// Default avatar.
        /// </summary>
        __AVATAR__,
        /// <summary>
        /// Point cloud (from Realsense camera)
        /// </summary>
        __PCC_CWI_,
        /// <summary>
        /// Point cloud (from Kinect camera)
        /// </summary>
        __PCC_CWIK4A_,
        /// <summary>
        /// Point cloud (from remote RGBD camera via proxy)
        /// </summary>
        __PCC_PROXY__,
        /// <summary>
        /// Synthetic point cloud
        /// </summary>
        __PCC_SYNTH__,
        /// <summary>
        /// Prerecorded point cloud
        /// </summary>
        __PCC_PRERECORDED__,
        /// <summary>
        /// User without visual representation but with audio feed
        /// </summary>
        __SPECTATOR__,
        /// <summary>
        /// Special spectator that records view (possibly retransmitting as a web video feed)
        /// </summary>
        __CAMERAMAN__,
        /// <summary>
        /// Application-defined alternative representation
        /// </summary>
        __ALT1__,
        /// <summary>
        /// Application-defined alternative representation
        /// </summary>
        __ALT2__
    }
}