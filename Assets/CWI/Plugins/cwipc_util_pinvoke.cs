using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

internal class API_cwipc_util
{
    [DllImport("cwipc_util")]
    internal extern static IntPtr cwipc_read([MarshalAs(UnmanagedType.LPStr)]string filename, System.UInt64 timestamp, ref System.IntPtr errorMessage);
    [DllImport("cwipc_util")]
    internal extern static void cwipc_free(IntPtr pc);
    [DllImport("cwipc_util")]
    internal extern static UInt64 cwipc_timestamp(IntPtr pc);
    [DllImport("cwipc_util")]
    internal extern static System.IntPtr cwipc_get_uncompressed_size(IntPtr pc, uint dataVersion = 0x20190209);
    [DllImport("cwipc_util")]
    internal extern static int cwipc_copy_uncompressed(IntPtr pc, IntPtr data, System.IntPtr size);

    [DllImport("cwipc_util")]
    internal extern static System.IntPtr cwipc_source_get(IntPtr src);
    [DllImport("cwipc_util")]
    internal extern static bool cwipc_source_eof(IntPtr src);
    [DllImport("cwipc_util")]
    internal extern static bool cwipc_source_available(IntPtr src, bool available);
    [DllImport("cwipc_util")]
    internal extern static void cwipc_source_free(IntPtr src);

    [DllImport("cwipc_util")]
    internal extern static IntPtr cwipc_synthetic();
}

internal class API_cwipc_realsense2
{
    [DllImport("cwipc_realsense2")]
    internal extern static IntPtr cwipc_realsense2(ref System.IntPtr errorMessage);
}


internal class API_cwipc_codec
{
    [DllImport("cwipc_codec")]
    internal extern static IntPtr cwipc_new_decoder();

    [DllImport("cwipc_codec")]
    internal extern static void cwipc_decoder_feed(IntPtr dec, IntPtr compFrame, int len);

}

internal class API_kernel
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern int GetModuleFileName(IntPtr hModule, StringBuilder modulePath, int nSize);
}

public class cwipc {
    System.IntPtr obj;
    Unity.Collections.NativeArray<byte> byteArray;
    Unity.Collections.NativeArray<PointCouldVertex> vertexArray;

    internal cwipc(System.IntPtr _obj)
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

    public void getByteArray() {
        if (obj == System.IntPtr.Zero) {
            Debug.Log("cwipc.obj == NULL");
            return;
        }
        
        unsafe {
            int size = (int)API_cwipc_util.cwipc_get_uncompressed_size(obj);
            if (byteArray.Length != 0) byteArray.Dispose();
            byteArray = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.TempJob);
            System.IntPtr ptr = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(byteArray);
            API_cwipc_util.cwipc_copy_uncompressed(obj, ptr, (System.IntPtr)size);
        }
    }

    public int load_to_pointbuffer(ref ComputeBuffer pointBuffer) {
        int size = byteArray.Length;
        if (size == 0) return 0;
        int ret = size / 16;
        unsafe {
            // Attempt by Jack to fix the pointbuffer allocation
            if (pointBuffer == null || pointBuffer.count < size) {
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
        if (obj == System.IntPtr.Zero) {
            Debug.Log("cwipc.obj == NULL");
            return;
        }

        unsafe
        {
            int size = (int)API_cwipc_util.cwipc_get_uncompressed_size(obj);
            var sizeT = Marshal.SizeOf(typeof(PointCouldVertex));

            vertexArray = new Unity.Collections.NativeArray<PointCouldVertex>( size / sizeT, Unity.Collections.Allocator.TempJob);

            System.IntPtr ptr = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(vertexArray);
            int ret = API_cwipc_util.cwipc_copy_uncompressed(obj, ptr, (System.IntPtr)size);
        }
    }

    public void load_to_mesh(ref Mesh mesh)
    {
        var points = new Vector3[vertexArray.Length];
        var indices = new int[vertexArray.Length];
        var colors = new Color32[vertexArray.Length];

        for (int i = 0; i < vertexArray.Length; i++) {
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
