using System;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class PrerecordedPointCloudReader : AbstractPointCloudPreparer
    {
        private AsyncPrerecordedReader reader;
        private QueueThreadSafe myQueue;
        private cwipc.pointcloud currentPointCloud;
        Unity.Collections.NativeArray<byte> byteArray;
        [Tooltip("If non-zero, voxelize each point cloud to this voxel size")]
        [SerializeField] private float voxelSize = 0;
        [Tooltip("Frame rate at which to read point clouds")]
        public float frameRate = 15;
        [Tooltip("How often the point cloud stream is looped (0 for infinite)")]
        public int loopCount = 0;
        [Tooltip("Directory name (or file name) where point clouds and/or tileconfig.json or are stored.")]
        [SerializeField] private string _dirName;
        public string dirName
        {
            get => _dirName;
            set {
                _dirName = value;
                if (myQueue != null)
                {
                    // We have already started.
                    InitReader();
                }
            }
        }
        [Tooltip("Point size to use if a point cloud does not contain a pointsize")]
        [SerializeField] float defaultPointSize = 0;

        const float allocationFactor = 1.3f;

        public override long currentTimestamp
        {
            get
            {
                if (currentPointCloud == null) return 0;
                return currentPointCloud.timestamp();
            }
        }

        public override FrameMetadata? currentMetadata
        {
            get
            {
                if (currentPointCloud == null) return null;
                return currentPointCloud.metadata;
            }
        }

        private void Start()
        {
            myQueue = new QueueThreadSafe($"{Name()}.queue");
            if(dirName != null && dirName != "") InitReader();
        }

        public void Stop()
        {
            reader?.Stop();
            reader = null;
        }

        private void InitReader()
        {
            if (reader != null)
            {
                Debug.LogError($"{Name()}: cannot restart with a new dirName");
                return;
            }
            reader = new AsyncPrerecordedReader(dirName, voxelSize, frameRate, myQueue, loopCount: loopCount);
        }

        private void OnDestroy()
        {
            Stop();
            if (byteArray.IsCreated)
            {
                byteArray.Dispose();
            }
        }

        public override int GetComputeBuffer(ref ComputeBuffer computeBuffer)
        {
            lock(this)
            {
                if (currentPointCloud == null) return 0;
                unsafe
                {
                    //
                    // Get the point cloud data into an unsafe native array.
                    //
                    int currentSize = currentPointCloud.get_uncompressed_size();
                    const int sizeofPoint = sizeof(float) * 4;
                    int nPoints = currentSize / sizeofPoint;
                    // xxxjack if currentCellsize is != 0 it is the size at which the points should be displayed
                    if (currentSize > byteArray.Length)
                    {
                        if (byteArray.Length != 0) byteArray.Dispose();
                        byteArray = new Unity.Collections.NativeArray<byte>(currentSize, Unity.Collections.Allocator.Persistent);
                    }
                    if (currentSize > 0)
                    {
                        System.IntPtr currentBuffer = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(byteArray);

                        int ret = currentPointCloud.copy_uncompressed(currentBuffer, currentSize);
                        if (ret * 16 != currentSize)
                        {
                            Debug.Log($"PointCloudPreparer decompress size problem: currentSize={currentSize}, copySize={ret * 16}, #points={ret}");
                            Debug.LogError("Programmer error while rendering a participant.");
                        }
                    }
                    //
                    // Copy the unsafe native array to the computeBuffer
                    //
                    if (computeBuffer == null || computeBuffer.count < nPoints)
                    {
                        int dampedSize = (int)(nPoints * allocationFactor);
                        if (computeBuffer != null) computeBuffer.Release();
                        computeBuffer = new ComputeBuffer(dampedSize, sizeofPoint);
                    }
                    computeBuffer.SetData(byteArray, 0, 0, currentSize);
                    return nPoints;
                }
            }
        }

        public override float GetPointSize()
        {
            if (currentPointCloud == null) return 0;
            float pointSize = currentPointCloud.cellsize();
            if (pointSize == 0) pointSize = defaultPointSize;
            return pointSize;
        }

        public override long getQueueDuration()
        {
            return myQueue.QueuedDuration();
        }

        public override bool LatchFrame()
        {
            if (currentPointCloud != null)
            {
                currentPointCloud.free();
                currentPointCloud = null;
            }
            currentPointCloud = (cwipc.pointcloud)myQueue.TryDequeue(0);
            return currentPointCloud != null;
        }

        public override string Name()
        {
            return $"{GetType().Name}";
        }

        public override void Synchronize()
        {
            
        }

        public override bool EndOfData()
        {
            return myQueue == null || (myQueue.IsClosed() && myQueue.Count() == 0);
        }

    }
}
