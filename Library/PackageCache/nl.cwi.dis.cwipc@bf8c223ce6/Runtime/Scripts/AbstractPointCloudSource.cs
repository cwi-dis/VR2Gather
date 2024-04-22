using System;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    /// <summary>
    /// Base class for MonoBehaviour that is a source of pointclouds.
    /// </summary>
    abstract public class AbstractPointCloudSource : AbstractPointCloudPreparer, IPointCloudReaderImplementation
    {
        protected cwipc.source reader = null;
        protected cwipc.pointcloud currentPointcloud = null;
        protected Unity.Collections.NativeArray<byte> currentByteArray;
        protected System.IntPtr currentBuffer;
        protected bool isReady;

        [Header("Introspection (for debugging)")]
        [Tooltip("Size of current pointcloud (in bytes)")]
        public int currentSize;
        [Tooltip("Timestamp of current pointcloud")]
        [SerializeField] protected Timestamp _currentTimestamp;
        public override Timestamp currentTimestamp { get { return _currentTimestamp; } }
        public override FrameMetadata currentMetadata {  get { return currentPointcloud?.metadata;  } }
        [Tooltip("Cell size of current pointcloud cell (in meters)")]
        [SerializeField] protected float currentCellSize = 0;
        [Tooltip("How many pointclouds have been read")]
        [SerializeField] protected int nRead;
        [Tooltip("How many pointclouds have been read and dropped")]
        [SerializeField] protected int nDropped;

        [Header("Fields valid for all reader implementations")]
        [Tooltip("Voxelize pointclouds to this size (if nonzero)")]
        public float voxelSize = 0;
        [Tooltip("Cellsize for pointclouds that don't specify a cellsize")]
        public float defaultCellSize = 0.01f;
        [Tooltip("Multiplication factor for cellsize")]
        public float cellSizeFactor = 1.0f;

        protected System.TimeSpan frameInterval;  // Interval between frame grabs, if maximum framerate specified
        protected System.DateTime earliestNextCapture;    // Earliest time we want to do the next capture, if non-null.
        
        public override string Name()
        {
            return $"{GetType().Name}";
        }

        public virtual void _AllocateReader()
        {
            throw new System.Exception($"{Name()}: _AllocateReader must be overridden");
        }

        public void OnDestroy()
        {
            currentPointcloud?.free();
            currentPointcloud = null;
            reader?.free();
            reader = null;
            if (currentByteArray.IsCreated)
            {
                currentByteArray.Dispose();
            }
        }

        public void Start()
        {
            if (reader == null)
            {
                _AllocateReader();
            }
            else
            {
                Debug.LogWarning("${Name()}: Start called twice");
            }
        }

       
        public void Stop()
        {
            reader?.free();
            reader = null;
#if CWIPC_WITH_LOGGING
            Debug.Log($"{Name()}: Stopped.");
#endif
        }

        protected void Update()
        {
            if (reader == null) return;
            //
            // Limit framerate, if required
            //
            if (earliestNextCapture != null)
            {
                if (System.DateTime.Now < earliestNextCapture)
                {
                    return;
                }
            }
           
            cwipc.pointcloud pc = reader.get();
            if (pc == null)
            {
                return;
            }
            if (frameInterval != null)
            {
                earliestNextCapture = System.DateTime.Now + frameInterval;
            }
            //
            // Do optional downsampling
            //
            Timedelta downsampleDuration = 0;
            if (voxelSize != 0)
            {
                System.DateTime downsampleStartTime = System.DateTime.Now;
                var newPc = cwipc.downsample(pc, voxelSize);
                if (newPc == null)
                {
                    Debug.LogWarning($"{Name()}: Voxelating pointcloud with {voxelSize} got rid of all points?");
                }
                else
                {
                    pc.free();
                    pc = newPc;
                }
                System.DateTime downsampleStopTime = System.DateTime.Now;
                downsampleDuration = (Timedelta)(downsampleStopTime - downsampleStartTime).TotalMilliseconds;

            }
            //
            // Do optional filtering
            //
            pc = filter(pc);
            //
            // Store this as the current point cloud, to be picked up by the next LatchFrame() call.
            //
            lock(this)
            {
                if (currentPointcloud != null)
                {
                    currentPointcloud.free();
                    currentPointcloud = null;
                    nDropped++;
                }
                currentPointcloud = pc;
                nRead++;
            }
        }

        virtual protected cwipc.pointcloud filter(cwipc.pointcloud pc)
        {
            return pc;
        }

        override public void Synchronize()
        {

        }

        override public bool LatchFrame()
        {
            lock (this)
            {
                cwipc.pointcloud pc = currentPointcloud;
                if (pc == null)
                {
                    return false;
                }
                currentPointcloud = null;
                unsafe
                {
                    currentSize = pc.get_uncompressed_size();
                    _currentTimestamp = pc.timestamp();
                    currentCellSize = pc.cellsize();
                    // xxxjack if currentCellsize is != 0 it is the size at which the points should be displayed
                    if (currentSize > currentByteArray.Length)
                    {
                        if (currentByteArray.Length != 0) currentByteArray.Dispose();
                        currentByteArray = new Unity.Collections.NativeArray<byte>(currentSize, Unity.Collections.Allocator.Persistent);
                        currentBuffer = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(currentByteArray);
                    }
                    if (currentSize > 0)
                    {
                        int ret = pc.copy_uncompressed(currentBuffer, currentSize);
                        if (ret * 16 != currentSize)
                        {
                            Debug.LogError($"{Name()}: Pointcloud size error");
                        }
                    }
                    pc.free();
                    isReady = true;
                }
            }
            return true;
        }

        override public int GetComputeBuffer(ref ComputeBuffer computeBuffer)
        {
            int size = currentSize / 16; // Because every Point is a 16bytes sized, so I need to divide the buffer size by 16 to know how many points are.
            lock (this)
            {
                if (isReady && size != 0)
                {
                    unsafe
                    {
                        if (computeBuffer == null || computeBuffer.count < size)
                        {
                            int dampedSize = size + 4 + size / 4; // We allocate 25% (and a bit) more, so we don't see too many reallocations

                            if (computeBuffer != null) computeBuffer.Release();
                            computeBuffer = new ComputeBuffer(dampedSize, sizeof(float) * 4);
                        }
                        computeBuffer.SetData(currentByteArray, 0, 0, currentByteArray.Length);
                    }
                    isReady = false;
                }
            }
             return size;
        }

        override public float GetPointSize()
        {
            if (currentCellSize > 0.0000f) return currentCellSize * cellSizeFactor;
            else return defaultCellSize * cellSizeFactor;
        }

        override public Timedelta getQueueDuration()
        {
            return 0;
        }
    }
}
