namespace VRT.Core
{
    /// <summary>
    /// The visual representation of a user.
    /// </summary>
    public enum UserRepresentationType
    {
        /// <summary>
        /// Point cloud.
        /// </summary>
        PointCloud,

        /// <summary>
        /// Default avatar.
        /// </summary>
        SimpleAvatar,

        /// <summary>
        /// 2D video (WebCam) stream, usually shown as an avatar with a screen.
        /// </summary>
        VideoAvatar,

        /// <summary>
        /// No representation, spectator/listener only.
        /// </summary>
        NoRepresentation,

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
    };
    /// <summary>
    /// UserRepresentationPointCloud has a number of variants (which determine the capturer used)
    /// </summary>
    public enum RepresentationPointcloudVariant
    {
        /// <summary>
        /// One or more RGBD cameras capturing live. Governed by cameraconfig.json.
        /// </summary>
        camera,
        /// <summary>
        /// Human-sized generated pointcloud (for development purposes)
        /// </summary>
        synthetic,
        /// <summary>
        /// Remote live cameras (or stream), accessed by connecting to a TCP server.
        /// </summary>
        remote,
        /// <summary>
        /// Remote live cameras (or stream), we open a TCP server port and the camera connects to it.
        /// </summary>
        proxy,
        /// <summary>
        /// Play back prerecorded pointcloud streams from a disk file.
        /// </summary>
        prerecorded,
        /// <summary>
        /// Same as camera, but falls back to synthetic if no cameras available (for development purposes)
        /// </summary>
        developer,
        none,
    };
}