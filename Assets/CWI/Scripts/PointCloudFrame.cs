using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class PointCloudFrame
{
    System.IntPtr obj;
    Unity.Collections.NativeArray<byte> byteArray;
    Unity.Collections.NativeArray<PointCouldVertex> vertexArray;

    public PointCloudFrame(System.IntPtr _obj)
    {
        obj = _obj;
    }

    public void free()
    {
        if (obj == System.IntPtr.Zero) return;
        API_cwipc_util.cwipc_free(obj);
        if (byteArray.Length != 0) byteArray.Dispose();
        if (vertexArray.Length != 0) vertexArray.Dispose();
        obj = System.IntPtr.Zero;
    }

    public UInt64 timestamp()
    {
        if (obj == System.IntPtr.Zero)
        {
            Debug.LogError("cwipc.obj == NULL");
            return 0;
        }
        return API_cwipc_util.cwipc_timestamp(obj);
    }

    public void getByteArray()
    {
        if (obj == System.IntPtr.Zero)
        {
            Debug.Log("cwipc.obj == NULL");
            return;
        }

        unsafe
        {
            int size = (int)API_cwipc_util.cwipc_get_uncompressed_size(obj);
            if (size != 0)
            {
                if (byteArray.Length != 0) byteArray.Dispose();
                byteArray = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.TempJob);
                System.IntPtr ptr = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(byteArray);
                API_cwipc_util.cwipc_copy_uncompressed(obj, ptr, (System.IntPtr)size);
            }
            else
                Debug.LogError("cwipc.cwipc_get_uncompressed_size == 0");

        }
    }

    public int load_to_pointbuffer(ref ComputeBuffer pointBuffer)
    {
        int size = byteArray.Length;
        if (size == 0) return 0;
        int ret = size / 16;
        unsafe
        {
            // Attempt by Jack to fix the pointbuffer allocation
            if (pointBuffer == null || pointBuffer.count < size)
            {
                if (pointBuffer != null) pointBuffer.Release();
                pointBuffer = new ComputeBuffer((int)(ret * 1.4f), sizeof(float) * 4);
            }
            pointBuffer.SetData<byte>(byteArray, 0, 0, size);
            byteArray.Dispose();

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
        if (obj == System.IntPtr.Zero)
        {
            Debug.Log("cwipc.obj == NULL");
            return;
        }

        unsafe
        {
            int size = (int)API_cwipc_util.cwipc_get_uncompressed_size(obj);
            var sizeT = Marshal.SizeOf(typeof(PointCouldVertex));

            vertexArray = new Unity.Collections.NativeArray<PointCouldVertex>(size / sizeT, Unity.Collections.Allocator.TempJob);

            System.IntPtr ptr = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(vertexArray);
            int ret = API_cwipc_util.cwipc_copy_uncompressed(obj, ptr, (System.IntPtr)size);
        }
    }

    public void load_to_mesh(ref Mesh mesh)
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
