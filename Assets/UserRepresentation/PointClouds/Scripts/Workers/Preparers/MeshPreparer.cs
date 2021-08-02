using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class MeshPreparer : BasePreparer
    {
        bool isReady = false;
        Unity.Collections.NativeArray<PointCouldVertex> vertexArray;
        System.IntPtr currentBuffer;
        int PointCouldVertexSize;
        public ulong currentTimestamp;
        Vector3[] points;
        int[] indices;
        Color32[] colors;
        float currentCellSize = 0.008f;
        float defaultCellSize;
        float cellSizeFactor;
        QueueThreadSafe InQueue;

        public MeshPreparer(QueueThreadSafe _InQueue, float _defaultCellSize = 0, float _cellSizeFactor = 0) : base(WorkerType.End)
        {
            defaultCellSize = _defaultCellSize != 0 ? _defaultCellSize : 0.008f;
            cellSizeFactor = _cellSizeFactor != 0 ? _cellSizeFactor : 0.71f;
            if (_InQueue == null)
            {
                throw new System.Exception("MeshPreparer: InQueue is null");
            }
            InQueue = _InQueue;
            PointCouldVertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(PointCouldVertex));
            Start();
        }

        public override void OnStop()
        {
            base.OnStop();
            if (InQueue != null && !InQueue.IsClosed()) InQueue.Close();
            if (vertexArray.Length != 0) vertexArray.Dispose();
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] // Also tried with Pack=1
        public struct PointCouldVertex
        {
            public Vector3 vertex;
            public Color32 color;
        }

        public override bool LatchFrame()
        {
            lock (this)
            {
                if (synchronizer != null)
                {
                    ulong bestTimestamp = synchronizer.GetBestTimestampForCurrentFrame();
                    if (bestTimestamp != 0 && bestTimestamp <= currentTimestamp)
                    {
                        //Debug.Log($"{Name()}: xxxjack not getting frame {UnityEngine.Time.frameCount} {currentTimestamp}");
                        return false;
                    }
                    //Debug.Log($"{Name()}: xxxjack getting frame {UnityEngine.Time.frameCount} {bestTimestamp}");

                }              // xxxjack Note: we are holding the lock during TryDequeue. Is this a good idea?
                // xxxjack Also: the 0 timeout to TryDecode may need thought.
                if (InQueue.IsClosed()) return false; // We are shutting down
                cwipc.pointcloud pc = (cwipc.pointcloud)InQueue.TryDequeue(0);
                if (pc == null) return false;
                unsafe
                {
                    int bufferSize = pc.get_uncompressed_size();
                    currentTimestamp = pc.timestamp();
                    currentCellSize = pc.cellsize();

                    // xxxjack if currentCellsize is != 0 it is the size at which the points should be displayed
                    int size = bufferSize / PointCouldVertexSize;
                    int dampedSize = (int)(size * Config.Instance.memoryDamping);
                    if (vertexArray.Length < dampedSize)
                    {
                        vertexArray = new Unity.Collections.NativeArray<PointCouldVertex>(dampedSize, Unity.Collections.Allocator.Persistent);
                        currentBuffer = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(vertexArray);
                    }
                    int ret = pc.copy_uncompressed(currentBuffer, bufferSize);
                    pc.free();
                    // Check that sizes make sense. Note that copy_uncompressed returns the number of points
                    if (ret != size)
                    {
                        Debug.Log($"MeshPreparer: decoding problem: copy_uncompressed() size={ret}, get_uncompressed_size()={bufferSize}, vertexSize={size}");
                        Debug.LogError("Programmer error while rendering a participant.");
                    }

                    points = new Vector3[size];
                    indices = new int[size];
                    colors = new Color32[size];
                    for (int i = 0; i < size; i++)
                    {
                        points[i] = vertexArray[i].vertex;
                        indices[i] = i;
                        colors[i] = vertexArray[i].color;
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
                ulong nextTimestamp = InQueue._PeekTimestamp(currentTimestamp + 1);
                while (nextTimestamp != 0 && nextTimestamp <= currentTimestamp)
                {
                    // This can happen when DASH switches streams: the newly selected stream produces
                    // a packet from earlier than the last packet of the previous stream.
                    // This looks very ugly, so we drop it.
                    var frameToDrop = InQueue.TryDequeue(0);
                    if (true) Debug.LogWarning($"{Name()}: Drop frame {nextTimestamp} <= previous {currentTimestamp}, {currentTimestamp - nextTimestamp}ms too late");
                    frameToDrop.free();
                    nextTimestamp = InQueue._PeekTimestamp(currentTimestamp + 1);
                }
                ulong latestTimestamp = InQueue.LatestTimestamp();
                synchronizer.SetTimestampRangeForCurrentFrame(Name(), currentTimestamp, nextTimestamp, latestTimestamp);
            }
        }

        public bool GetMesh(ref Mesh mesh)
        {
            lock (this)
            {
                if (isReady)
                {
                    mesh.Clear();
                    mesh.vertices = points;
                    mesh.colors32 = colors;
                    // mesh.SetIndices(indices, 0, indices.Length, MeshTopology.Points, 0);
                    mesh.SetIndices(indices, MeshTopology.Points, 0);
                    isReady = false;
                    return true;
                }
            }
            return false;
        }

        public float GetPointSize()
        {
            if (currentCellSize > 0.0000f) return currentCellSize * cellSizeFactor;
            else return defaultCellSize * cellSizeFactor;
        }

        public int getQueueSize()
        {
            return InQueue._Count;
        }
    }
}
