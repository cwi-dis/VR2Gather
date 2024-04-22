using System;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class NetworkPointCloudReader : AbstractPointCloudPreparer
    {
        private AsyncTCPPCReader reader;
        private AbstractPointCloudDecoder decoder;
        private QueueThreadSafe decoderQueue;
        private QueueThreadSafe myQueue;
        private cwipc.pointcloud currentPointCloud;
        Unity.Collections.NativeArray<byte> byteArray;
        [Tooltip("URL for pointcloud source server (tcp://host:port)")]
        public string url;
        [Tooltip("If true the pointclouds received are compressed")]
        public bool compressed;
        

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
            decoderQueue = new QueueThreadSafe($"{Name()}.decoderQueue");
            InitReader();
        }

        public void Stop()
        {
            reader?.Stop();
            reader = null;
            decoder?.Stop();
        }

        private void InitReader()
        {
            if (reader != null)
            {
                Debug.LogError($"{Name()}: already initialized");
                return;
            }
            string fourcc = compressed ? "cwi1" : "cwi0";
            StreamSupport.IncomingTileDescription[] tileDescriptions = new StreamSupport.IncomingTileDescription[1]
            {
                new StreamSupport.IncomingTileDescription
                {
                    name="0",
                    outQueue=decoderQueue
                }
            };
            reader = new AsyncTCPPCReader(url, fourcc, tileDescriptions);
            if (compressed)
            {
                decoder = new AsyncPCDecoder(decoderQueue, myQueue);
            }
            else
            {
                decoder = new AsyncPCNullDecoder(decoderQueue, myQueue);
            }
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
