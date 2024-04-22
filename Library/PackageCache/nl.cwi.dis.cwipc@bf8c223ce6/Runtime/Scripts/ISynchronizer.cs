namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    /// <summary>
    /// Interface implemented by class responsible for synchronizing multiple media streams (inter-stream synchronization).
    /// Per-frame synchronization is a two-step process: first all stream handlers inform the synchronizer of the available
    /// range of timestamps, and in the second step all stream handlers ask the synchronizer what the best timestamp is
    /// for the current frame (and they will then attempt to present that frame to the renderers).
    /// </summary>
    public interface ISynchronizer
    {
        /// <summary>
        /// Inform synchronizer of available timestamp range for next frame to be presented.
        /// </summary>
        /// <param name="caller">Name of the caller, used to differentiate between callers and statistic printing</param>
        /// <param name="earliestFrameTimestamp">Earliest possible timestamp (milliseconds)</param>
        /// <param name="latestFrameTimestamp">Latest possible timestamp (milliseconds)</param>
        public void SetTimestampRangeForCurrentFrame(string caller, Timestamp earliestFrameTimestamp, Timestamp latestFrameTimestamp);
        /// <summary>
        /// Inform synchronizer of available timestamp range for next frame to be presented.
        /// One stream can use this call in stead of SetTimestampRangeForCurrentFrame, which signals that the continuity of
        /// this stream is more important than the continuity of other streams.
        /// </summary>
        /// <param name="caller">Name of the caller, used to differentiate between callers and statistic printing</param>
        /// <param name="earliestFrameTimestamp">Earliest possible timestamp (milliseconds)</param>
        /// <param name="latestFrameTimestamp">Latest possible timestamp (milliseconds)</param>
        public void SetAudioTimestampRangeForCurrentFrame(string caller, Timestamp earliestFrameTimestamp, Timestamp latestFrameTimestamp);
        /// <summary>
        /// Return best timestamp for the next frame to display, taking into account current system time and available ranges
        /// reported by all users of this synchronizer during the current frame.
        /// </summary>
        /// <returns>Timestamp in milliseconds</returns>
        public Timestamp GetBestTimestampForCurrentFrame();
        /// <summary>
        /// True if the synchronizer is in debug mode, clients should print out information on synchronization decisions.
        /// </summary>
        public bool debugSynchronizer { get; }
        /// <summary>
        /// True if the synchronizer is enabled.
        /// </summary>
        /// <returns>True if enabled.</returns>
        public bool isEnabled();
        /// <summary>
        /// Disable this synchronizer. No more calls should be made after this.
        /// </summary>
        public void disable();
        /// <summary>
        /// Name of the synchronizer (mainly for printing debug and statistics messages)
        /// </summary>
        /// <returns>A string with the name</returns>
        public string Name();
    }
}