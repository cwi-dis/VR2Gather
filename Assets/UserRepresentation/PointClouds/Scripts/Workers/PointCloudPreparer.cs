using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class PointCloudPreparer : BasePreparer
    {
        bool isReady = false;
        Unity.Collections.NativeArray<byte> byteArray;
        System.IntPtr currentBuffer;
        int currentSize;
        public ulong currentTimestamp;
        float currentCellSize = 0.008f;
        float defaultCellSize;
        float cellSizeFactor;
        public PointCloudPreparer(QueueThreadSafe _InQueue, float _defaultCellSize = 0, float _cellSizeFactor = 0) : base(_InQueue)
        {
            defaultCellSize = _defaultCellSize != 0 ? _defaultCellSize : 0.01f;
            cellSizeFactor = _cellSizeFactor != 0 ? _cellSizeFactor : 1.0f;
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            if (byteArray.Length != 0) byteArray.Dispose();
            Debug.Log("PointCloudPreparer Stopped");
        }

        public override bool LatchFrame()
        {
           lock (this)
            {
                if (synchronizer != null)
                {
                    ulong bestTimestamp = synchronizer.GetBestTimestampForCurrentFrame();
                    if (bestTimestamp != 0 &&  bestTimestamp <= currentTimestamp)
                    {
                        if (synchronizer.debugSynchronizer) Debug.Log($"{Name()}: show nothing for frame {UnityEngine.Time.frameCount}: {currentTimestamp-bestTimestamp} ms in the future: bestTimestamp={bestTimestamp}, currentTimestamp={currentTimestamp}");
                        return false;
                    }
                    if (bestTimestamp != 0 && synchronizer.debugSynchronizer) Debug.Log($"{Name()}: frame {UnityEngine.Time.frameCount} bestTimestamp={bestTimestamp}, currentTimestamp={currentTimestamp}, {bestTimestamp-currentTimestamp} ms too late");
                }
                // xxxjack Note: we are holding the lock during TryDequeue. Is this a good idea?
                // xxxjack Also: the 0 timeout to TryDecode may need thought.
                if (InQueue.IsClosed()) return false; // We are shutting down
                cwipc.pointcloud pc = (cwipc.pointcloud)InQueue.TryDequeue(0);
                if (pc == null)
                {
                    if (currentTimestamp != 0 && synchronizer != null && synchronizer.debugSynchronizer)
                    {
                        Debug.Log($"{Name()}: no pointcloud available");
                    }
                    return false;
                }
                unsafe
                {
                    currentSize = pc.get_uncompressed_size();
                    currentTimestamp = pc.timestamp();
                    if (currentSize <= 0)
                    {
                        // This happens very often with tiled pointclouds.
                        //Debug.Log("PointCloudPreparer: pc.get_uncompressed_size is 0");
                        return false;
                    }
                    currentCellSize = pc.cellsize();
                    // xxxjack if currentCellsize is != 0 it is the size at which the points should be displayed
                    if (currentSize > byteArray.Length)
                    {
                        if (byteArray.Length != 0) byteArray.Dispose();
                        byteArray = new Unity.Collections.NativeArray<byte>(currentSize, Unity.Collections.Allocator.Persistent);
                        currentBuffer = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(byteArray);
                    }
                    int ret = pc.copy_uncompressed(currentBuffer, currentSize);
                    pc.free();
                    if (ret * 16 != currentSize)
                    {
                        Debug.Log($"PointCloudPreparer decompress size problem: currentSize={currentSize}, copySize={ret * 16}, #points={ret}");
                        Debug.LogError("Programmer error while rendering a participant.");
                    }
                    isReady = true;
                }
            }
            return true;
        }

        public override void Synchronize()
        {
            // Synchronize playout for the current frame with other preparers (if needed)
            if (synchronizer)
            {
                ulong earliestTimestamp = currentTimestamp;
                if (earliestTimestamp == 0) earliestTimestamp = InQueue._PeekTimestamp();
                while (earliestTimestamp != 0 && earliestTimestamp < currentTimestamp)
                {
                    // This can happen when DASH switches streams: the newly selected stream produces
                    // a packet from earlier than the last packet of the previous stream.
                    // This looks very ugly, so we drop it.
                    var frameToDrop = InQueue.TryDequeue(0);
                    if (true) Debug.LogWarning($"{Name()}: Drop frame {earliestTimestamp} <= previous {currentTimestamp}, {currentTimestamp- earliestTimestamp}ms too late");
                    frameToDrop.free();
                    earliestTimestamp = InQueue._PeekTimestamp(currentTimestamp);
                }
                ulong latestTimestamp = InQueue.LatestTimestamp();
                synchronizer.SetTimestampRangeForCurrentFrame(Name(), earliestTimestamp, latestTimestamp);
            }
        }
        public int GetComputeBuffer(ref ComputeBuffer computeBuffer)
        {
            // xxxjack I don't understand this computation of size, the sizeof(float)*4 below and the byteArray.Length below that.
            int size = currentSize / 16; // Because every Point is a 16bytes sized, so I need to divide the buffer size by 16 to know how many points are.
            lock (this)
            {
                if (isReady && size != 0)
                {
                    unsafe
                    {
                        int dampedSize = (int)(size * Config.Instance.memoryDamping);
                        if (computeBuffer == null || computeBuffer.count < dampedSize)
                        {
                            if (computeBuffer != null) computeBuffer.Release();
                            computeBuffer = new ComputeBuffer(dampedSize, sizeof(float) * 4);
                        }
                        computeBuffer.SetData(byteArray, 0, 0, byteArray.Length);
                    }
                    isReady = false;
                }
            }
            return size;
        }

        public float GetPointSize()
        {
            if (currentCellSize > 0.0000f) return currentCellSize * cellSizeFactor;
            else return defaultCellSize * cellSizeFactor;
        }

    }
}
