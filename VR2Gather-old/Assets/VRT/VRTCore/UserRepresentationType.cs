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
    }
}