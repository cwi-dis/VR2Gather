using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;


    /// <summary>
    /// Interface implemented by objects that provide pointclouds to a renderer (or other consumer).
    /// </summary>
    public interface IPointCloudPreparer : IPreparer
    {
        /// <summary>
        /// Store pointcloud data of frame locked by LatchFrame in a ComputeBuffer.
        /// ComputBuffer must be pre-allocated and will be increased in size to make the data fit.
        /// </summary>
        /// <param name="computeBuffer">Where the pointcloud data is stored.</param>
        /// <returns>Number of points in the pointcloud</returns>
        public int GetComputeBuffer(ref ComputeBuffer computeBuffer);

        /// <summary>
        /// Return size (in meters) of a single cell/point in the current pointcloud.
        /// Used for rendering the points at the correct size.
        /// </summary>
        /// <returns>Size of a cell</returns>
        public float GetPointSize();

        /// <summary>
        /// Timestamp of current frame (for debugging and statistics, mainly)
        /// </summary>
        public Timestamp currentTimestamp { get; }

        /// <summary>
        /// Metadata of current frame.
        /// </summary>
        public FrameMetadata currentMetadata { get; }

        /// <summary>
        /// Return name (for logging and statistics)
        /// </summary>
        /// <returns></returns>
        public string Name();
    }

    /// <summary>
    /// Abstract baseclass for MonoBehaviour that implements IPointcloudPreparer.
    /// </summary>
    abstract public class AbstractPointCloudPreparer : MonoBehaviour, IPointCloudPreparer
    {
        abstract public void Synchronize();
        abstract public bool LatchFrame();
        abstract public int GetComputeBuffer(ref ComputeBuffer computeBuffer);
        abstract public float GetPointSize();
        abstract public Timedelta getQueueDuration();
        abstract public bool EndOfData();
        abstract public Timestamp currentTimestamp { get; }
        abstract public FrameMetadata currentMetadata { get; }
        abstract public string Name();
    }
}