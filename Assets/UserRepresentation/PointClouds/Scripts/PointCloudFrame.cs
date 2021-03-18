using System;
using System.Runtime.InteropServices;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class PointCloudFrame
    {
        cwipc.pointcloud pc;    // The pointcloud object, in internal cwipc format
        Unity.Collections.NativeArray<byte> byteArray;
        Unity.Collections.NativeArray<PointCouldVertex> vertexArray;

        public PointCloudFrame()
        {
        }

        public void SetData(cwipc.pointcloud _pc)
        {
            pc = _pc;
        }

        public void Release()
        {
            if (byteArray.Length != 0) { byteArray.Dispose(); }
            Debug.Log("PointCloudFrame.Free");
        }

        public void FreeFrameData()
        {
            pc = null;
            if (vertexArray.Length != 0) vertexArray.Dispose();
        }

        public ulong timestamp()
        {
            return pc.timestamp();
        }


        public void getByteArray()
        {
            if (pc == null)
            {
                throw new Exception("PointCloudFrame: getByteArray() called but pc==null");
            }
            unsafe
            {
                int size = pc.get_uncompressed_size();
                if (size > 0)
                {
                    if (size > byteArray.Length)
                    {
                        if (byteArray.Length != 0) byteArray.Dispose();
                        byteArray = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.Persistent);

                    }
                    IntPtr ptr = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(byteArray);
                    pc.copy_uncompressed(ptr, size);
                    //                Debug.Log("Alloc PointCloud ByteArray!!!");
                }
                else
                    Debug.LogError("Programmer error: cwipc.cwipc_get_uncompressed_size == 0");

            }
        }

        public int loadToPointbuffer(ref ComputeBuffer pointBuffer)
        {
            int size = byteArray.Length;
            if (size == 0) return 0;
            int ret = size / 16;
            unsafe
            {
                // Attempt by Jack to fix the pointbuffer allocation
                int dampedSize = (int)(ret * Config.Instance.memoryDamping);
                if (pointBuffer == null || pointBuffer.count < dampedSize)
                {
                    if (pointBuffer != null) pointBuffer.Release();
                    pointBuffer = new ComputeBuffer(dampedSize, sizeof(float) * 4);
                }
                pointBuffer.SetData(byteArray, 0, 0, size);

            }
            return ret;
        }

        [StructLayout(LayoutKind.Sequential)] // Also tried with Pack=1
        public struct PointCouldVertex
        {
            public Vector3 vertex;
            public Color32 color;
        }

        public void getVertexArray()
        {
            if (pc == null)
            {
                throw new Exception("PointCloudFrame: getVertexArray() called but pc==null");
            }

            unsafe
            {
                int size = pc.get_uncompressed_size();
                var sizeT = Marshal.SizeOf(typeof(PointCouldVertex));
                vertexArray = new Unity.Collections.NativeArray<PointCouldVertex>(size / sizeT, Unity.Collections.Allocator.Persistent);
                IntPtr ptr = (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(vertexArray);
                int ret = pc.copy_uncompressed(ptr, size);
            }
        }

        public void loadToMesh(ref Mesh mesh)
        {
            var points = new Vector3[vertexArray.Length];
            var indices = new int[vertexArray.Length];
            var colors = new Color32[vertexArray.Length];

            for (int i = 0; i < vertexArray.Length; i++)
            {
                points[i] = vertexArray[i].vertex;
                indices[i] = i;
                colors[i] = vertexArray[i].color;
            }
            mesh.Clear();
            mesh.vertices = points;
            mesh.colors32 = colors;
            mesh.SetIndices(indices, MeshTopology.Points, 0);

            vertexArray.Dispose();
        }

    }
}