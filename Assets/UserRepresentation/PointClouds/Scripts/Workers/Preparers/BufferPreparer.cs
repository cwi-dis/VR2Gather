using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTCore;

namespace VRT.UserRepresentation.PointCloud
{
    public class BufferPreparer : BasePreparer
    {
        bool isReady = false;
        Unity.Collections.NativeArray<byte> byteArray;
        System.IntPtr currentBuffer;
        int currentSize;
        public ulong currentTimestamp;
        float currentCellSize = 0.008f;
        float defaultCellSize;
        float cellSizeFactor;
        QueueThreadSafe InQueue;
        public BufferPreparer(QueueThreadSafe _InQueue, float _defaultCellSize = 0, float _cellSizeFactor = 0) : base(WorkerType.End)
        {
            defaultCellSize = _defaultCellSize != 0 ? _defaultCellSize : 0.008f;
            cellSizeFactor = _cellSizeFactor != 0 ? _cellSizeFactor : 0.71f;
            if (_InQueue == null)
            {
                throw new System.Exception("BufferPreparer: InQueue is null");
            }
            InQueue = _InQueue;
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            if (byteArray.Length != 0) byteArray.Dispose();
            Debug.Log("BufferPreparer Stopped");
        }

        public override void LatchFrame()
        {
           lock (this)
            {
                if (synchronizer != null)
                {
                    ulong bestTimestamp = synchronizer.GetBestTimestampForCurrentFrame();
                    if (bestTimestamp == currentTimestamp)
                    {
                        Debug.Log($"{Name()}: xxxjack not getting frame {UnityEngine.Time.frameCount} {currentTimestamp}");
                        return;
                    }
                    Debug.Log($"{Name()}: xxxjack getting frame {UnityEngine.Time.frameCount} {bestTimestamp}");

                }
                // xxxjack Note: we are holding the lock during TryDequeue. Is this a good idea?
                // xxxjack Also: the 0 timeout to TryDecode may need thought.
                if (InQueue.IsClosed()) return; // Weare shutting down
                cwipc.pointcloud pc = (cwipc.pointcloud)InQueue.TryDequeue(0);
                if (pc == null) return;
                unsafe
                {
                    currentSize = pc.get_uncompressed_size();
                    currentTimestamp = pc.timestamp();
                    if (currentSize <= 0)
                    {
                        // This happens very often with tiled pointclouds.
                        //Debug.Log("BufferPreparer: pc.get_uncompressed_size is 0");
                        return;
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
                        Debug.Log($"BufferPreparer decompress size problem: currentSize={currentSize}, copySize={ret * 16}, #points={ret}");
                        Debug.LogError("Programmer error while rendering a participant.");
                    }
                    isReady = true;
                }
            }
        }

        public override void Synchronize()
        {
            // Synchronize playout for the current frame with other preparers (if needed)
            if (synchronizer)
            {
                synchronizer.SetEarliestTimestampForCurrentFrame(currentTimestamp);
                if (InQueue != null && InQueue._CanDequeue())
                {
                    // xxxjack Not the correct timestamp for next frame, but goot enough for now
                    synchronizer.SetLatestTimestampForCurrentFrame(currentTimestamp + 1);
                }
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
