using System;
using System.Runtime.InteropServices;
using UnityEngine;

internal class API_cwipc_util
{
    [DllImport("cwipc_util")]
    internal extern static IntPtr cwipc_read([MarshalAs(UnmanagedType.LPStr)]string filename, System.UInt64 timestamp, ref System.IntPtr errorMessage);
    [DllImport("cwipc_util")]
    internal extern static void cwipc_free(IntPtr pc);
    [DllImport("cwipc_util")]
    internal extern static uint cwipc_timestamp(IntPtr pc);
    [DllImport("cwipc_util")]
    internal extern static Int32 cwipc_get_uncompressed_size(IntPtr pc, uint dataVersion = 0x20190209);
    [DllImport("cwipc_util")]
    internal extern static int cwipc_copy_uncompressed(IntPtr pc, IntPtr data, Int32 size);
    [DllImport("cwipc_util")]
    internal extern static System.IntPtr cwipc_source_get(IntPtr src);
    [DllImport("cwipc_util")]
    internal extern static void cwipc_source_free(IntPtr src);
    [DllImport("cwipc_util")]
    internal extern static IntPtr cwipc_synthetic();
}

public class cwipc_util_pinvoke
{
        public static System.IntPtr GetPointCloudFromPly(string filename) {

//        System.IntPtr src = cwipc_synthetic();
//        return cwipc_source_get(src);
        System.IntPtr ptr = System.IntPtr.Zero;
        var rv = API_cwipc_util.cwipc_read(Application.streamingAssetsPath + "/" + filename, 0, ref ptr);
        Debug.Log("xxxjack cwipc_read returned " + rv + ",errorptr " + ptr);
        if (ptr != System.IntPtr.Zero)
        {
            string errorMessage = Marshal.PtrToStringAuto(ptr);
            Debug.LogError("cwipc_read returned error: " + errorMessage);
        }
        return rv;
    }

    public static System.IntPtr GetPointCloudFromCWICPC(string filename)
    {
        float init = Time.realtimeSinceStartup;
        var bytes = System.IO.File.ReadAllBytes(Application.streamingAssetsPath + "/"+ filename);
        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
        float read = Time.realtimeSinceStartup;

        var pc = cwipc_codec_pinvoke.cwipc_decompress(ptr, bytes.Length);
        float decom1 = Time.realtimeSinceStartup;
        pc = cwipc_codec_pinvoke.cwipc_decompress(ptr, bytes.Length);
        float decom2 = Time.realtimeSinceStartup;

        Debug.Log(">>> read " + (read - init) + " decom " + (decom1 - read) + " decom2 " + (decom2 - decom1));
        

        return pc;
    }

    public static System.IntPtr getSynthetic()
    {
        return API_cwipc_util.cwipc_synthetic();
    }

    public static System.IntPtr getPointCloudFromSource(System.IntPtr pcsrc)
    {
        return API_cwipc_util.cwipc_source_get(pcsrc);
    }

    public static void UpdatePointBuffer(System.IntPtr pc, ref ComputeBuffer pointBuffer)
    {
        uint ts = API_cwipc_util.cwipc_timestamp(pc);
        System.Int32 size = API_cwipc_util.cwipc_get_uncompressed_size(pc);
        unsafe
        {
            var array = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.Temp);

            System.IntPtr ptr = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(array);
            int ret = API_cwipc_util.cwipc_copy_uncompressed(pc, ptr, size);

            if (pointBuffer == null) pointBuffer = new ComputeBuffer(ret, sizeof(float) * 4);
            pointBuffer.SetData<byte>(array, 0, 0, size);

            array.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential)] // Also tried with Pack=1
    public struct PointCouldVertex
    {
        public Vector3 vertex;
        public Color32 color;
    }

    public static void UpdatePointBuffer(System.IntPtr pc, ref Mesh mesh)
    {
        uint ts = API_cwipc_util.cwipc_timestamp(pc);
        System.Int32 size = API_cwipc_util.cwipc_get_uncompressed_size(pc);
        unsafe
        {
            var sizeT = Marshal.SizeOf(typeof(PointCouldVertex));
            var array = new Unity.Collections.NativeArray<PointCouldVertex>(size / sizeT, Unity.Collections.Allocator.Temp);
            System.IntPtr ptr = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(array);
            int ret = API_cwipc_util.cwipc_copy_uncompressed(pc, ptr, size);

            var points = new Vector3[array.Length];
            var indices = new int[array.Length];
            var colors = new Color32[array.Length];

            for (int i = 0; i < array.Length; i++) {
                points[i] = array[i].vertex;
                indices[i] = i;
                colors[i] = array[i].color;
            }

            mesh.vertices = points;
            mesh.colors32 = colors;
            mesh.SetIndices(indices, MeshTopology.Points, 0);

            array.Dispose();
        }
    }

}
