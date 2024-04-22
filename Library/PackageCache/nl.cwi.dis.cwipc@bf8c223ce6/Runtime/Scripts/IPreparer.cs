using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    /// <summary>
    /// Provide media objects (pointclouds, audio, video) to renderers or other consumers.
    /// This interface is provided by media stream readers and decoders and such.
    /// Base interface handles synchronization between mutiple media streams.
    /// </summary>
    public interface IPreparer
    {
        /// <summary>
        /// Prepare synchronizer. Called on all preparers in a group so they can determine possible
        /// range of frame to display.
        /// </summary>
        public void Synchronize();

        /// <summary>
        /// Lock the synchronizer. Called on all preparers so they can determine which frame they will return.
        /// After this the various Get methods of the per-media-type interface subclasses can be used to obtain the
        /// data.
        /// </summary>
        /// <returns>False if no suitable frame is available</returns>
        public bool LatchFrame();

        /// <summary>
        /// Returns input queue length: how much input data is available for this preparer.
        /// Mainly for statstics printing.
        /// </summary>
        /// <returns>Input queue duration in milliseconds</returns>
        public Timedelta getQueueDuration();

        /// <summary>
        /// Returns true if no more data is available.
        /// </summary>
        /// <returns></returns>
        public bool EndOfData();
    }
}