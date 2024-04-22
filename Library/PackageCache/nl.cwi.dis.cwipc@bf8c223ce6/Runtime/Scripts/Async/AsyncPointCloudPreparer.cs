using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class AsyncPointCloudPreparer : AsyncPreparer, IPointCloudPreparer
    {
        const float allocationFactor = 1.3f; // Must be >= 1: How much size to allocate. Bigger means fewer re-allocations.
        bool isReady = false;
        Unity.Collections.NativeArray<byte> byteArray;
        System.IntPtr currentBuffer;
        int currentSize;
        Timestamp _currentTimestamp;
        public Timestamp currentTimestamp {  get { return _currentTimestamp;  } }
        FrameMetadata _currentMetadata;
        public FrameMetadata currentMetadata {  get { return _currentMetadata;  } }
        float currentCellSize = 0.008f;
        float defaultCellSize;
        float cellSizeFactor;

        public AsyncPointCloudPreparer(QueueThreadSafe _InQueue, float _defaultCellSize = 0, float _cellSizeFactor = 0) : base(_InQueue)
        {
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
            defaultCellSize = _defaultCellSize != 0 ? _defaultCellSize : 0.01f;
            cellSizeFactor = _cellSizeFactor != 0 ? _cellSizeFactor : 1.0f;
            Start();
        }

        public override void AsyncOnStop()
        {
            base.AsyncOnStop();
            if (byteArray.Length != 0) byteArray.Dispose();
        }

        protected override void AsyncUpdate()
        {
        }

        public override bool LatchFrame()
        {
           lock (this)
            {
                int dropCount = 0;
                Timestamp bestTimestamp = 0;
                if (synchronizer != null)
                {
                    bestTimestamp = synchronizer.GetBestTimestampForCurrentFrame();
                    if (bestTimestamp != 0 &&  bestTimestamp <= _currentTimestamp)
                    {
                        if (synchronizer.debugSynchronizer) Debug.Log($"{Name()}: show nothing for frame {UnityEngine.Time.frameCount}: {_currentTimestamp-bestTimestamp} ms in the future: bestTimestamp={bestTimestamp}, currentTimestamp={_currentTimestamp}");
                        return false;
                    }
                    if (bestTimestamp != 0 && synchronizer.debugSynchronizer) Debug.Log($"{Name()}: frame {UnityEngine.Time.frameCount} bestTimestamp={bestTimestamp}, currentTimestamp={_currentTimestamp}, {bestTimestamp-_currentTimestamp} ms too late");
                }
                // xxxjack Note: we are holding the lock during TryDequeue. Is this a good idea?
                // xxxjack Also: the 0 timeout to TryDecode may need thought.
                if (InQueue.IsClosed()) return false; // We are shutting down
                cwipc.pointcloud pc = (cwipc.pointcloud)InQueue.TryDequeue(0);
                if (pc == null)
                {
                    if (_currentTimestamp != 0 && synchronizer != null && synchronizer.debugSynchronizer)
                    {
                        Debug.Log($"{Name()}: no pointcloud available");
                    }
#if VRT_WITH_STATS
                    stats.statsUpdate(0, true);
#endif
                    return false;
                }
                // See if there are more pointclouds in the queue that are no later than bestTimestamp
                while (pc.timestamp() < bestTimestamp)
                {
                    Timestamp nextTimestamp = InQueue._PeekTimestamp();
                    // If there is no next queue entry, or it has no timestamp, or it is after bestTimestamp we break out of the loop
                    if (nextTimestamp == 0 || nextTimestamp > bestTimestamp) break;
                    // We know there is another pointcloud in the queue, and we know it is better than what we have now. Replace it.
                    dropCount++;
                    pc.free();
                    pc = (cwipc.pointcloud)InQueue.Dequeue();
                }
                unsafe
                {
                    currentSize = pc.get_uncompressed_size();
                    _currentTimestamp = pc.timestamp();
                    currentCellSize = pc.cellsize();
                    _currentMetadata = pc.metadata;
                    // xxxjack if currentCellsize is != 0 it is the size at which the points should be displayed
                    if (currentSize > byteArray.Length)
                    {
                        if (byteArray.Length != 0) byteArray.Dispose();
                        byteArray = new Unity.Collections.NativeArray<byte>(currentSize, Unity.Collections.Allocator.Persistent);
                        currentBuffer = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(byteArray);
                    }
                    if (currentSize > 0)
                    {
                        int ret = pc.copy_uncompressed(currentBuffer, currentSize);
                        if (ret * 16 != currentSize)
                        {
                            Debug.Log($"PointCloudPreparer decompress size problem: currentSize={currentSize}, copySize={ret * 16}, #points={ret}");
                            Debug.LogError("Programmer error while rendering a participant.");
                        }
                    }
                    pc.free();
                    isReady = true;
                }
#if VRT_WITH_STATS
                stats.statsUpdate(dropCount, false);
#endif
            }
            return true;
        }

        public override void Synchronize()
        {
            // Synchronize playout for the current frame with other preparers (if needed)
            if (synchronizer != null)
            {
                Timestamp earliestTimestamp = _currentTimestamp;
                if (earliestTimestamp == 0) earliestTimestamp = InQueue._PeekTimestamp();
                while (earliestTimestamp != 0 && earliestTimestamp < _currentTimestamp)
                {
                    // This can happen when DASH switches streams: the newly selected stream produces
                    // a packet from earlier than the last packet of the previous stream.
                    // This looks very ugly, so we drop it.
                    var frameToDrop = InQueue.TryDequeue(0);
                    if (true) Debug.LogWarning($"{Name()}: Drop frame {earliestTimestamp} <= previous {_currentTimestamp}, {_currentTimestamp- earliestTimestamp}ms too late");
                    frameToDrop.free();
                    earliestTimestamp = InQueue._PeekTimestamp(_currentTimestamp);
                }
                Timestamp latestTimestamp = InQueue.LatestTimestamp();
                synchronizer.SetTimestampRangeForCurrentFrame(Name(), earliestTimestamp, latestTimestamp);
            }
        }
        public int GetComputeBuffer(ref ComputeBuffer computeBuffer)
        {
            const int sizeofPoint = sizeof(float) * 4;
            int nPoints = currentSize / sizeofPoint; // Because every Point is a 16bytes sized, so I need to divide the buffer size by 16 to know how many points are.
            lock (this)
            {
                if (isReady && nPoints != 0)
                {
                    unsafe
                    {
                        if (computeBuffer == null || computeBuffer.count < nPoints)
                        {
                            int dampedSize = (int)(nPoints * allocationFactor);
                            if (computeBuffer != null) computeBuffer.Release();
                            computeBuffer = new ComputeBuffer(dampedSize, sizeofPoint);
                        }
                        computeBuffer.SetData(byteArray, 0, 0, currentSize);
                    }
                    isReady = false;
                }
            }
            return nPoints;
        }

        public float GetPointSize()
        {
            if (currentCellSize > 0.0000f) return currentCellSize * cellSizeFactor;
            else return defaultCellSize * cellSizeFactor;
        }

#if VRT_WITH_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalUpdates;
            double statsDrops;
            double statsNoData;
            int statsAggregatePackets;

            public void statsUpdate(int dropCount, bool noData)
            {

                statsTotalUpdates += 1;
                statsDrops += dropCount;
                if (noData)
                {
                    statsNoData++;
                } else
                {
                    statsAggregatePackets++;
                }
                statsAggregatePackets += dropCount;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalUpdates / Interval():F2}, fps_dropped={statsDrops / Interval():F2}, fps_nodata={statsNoData / Interval():F2}, aggregate_packets={statsAggregatePackets}");
                    Clear();
                    statsTotalUpdates = 0;
                    statsDrops = 0;
                    statsNoData = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }
}
