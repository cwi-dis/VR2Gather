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

public class cwipc
{
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

public interface cwipc_source
{
    void free();
    bool eof();
    bool available(bool wait);
    cwipc get();
}

internal class cwipc_source_impl : cwipc_source
{
    System.IntPtr obj;

    internal cwipc_source_impl(System.IntPtr _obj)
    {
        obj = _obj;
    }

    public bool eof()
    {
        if (obj == System.IntPtr.Zero) return true;
        return API_cwipc_util.cwipc_source_eof(obj);
    }

    public bool available(bool wait)
    {
        if (obj == System.IntPtr.Zero) return false;
        return API_cwipc_util.cwipc_source_available(obj, wait);
    }

    public void free()
    {
        if (obj == System.IntPtr.Zero) return;
        API_cwipc_util.cwipc_source_free(obj);
        obj = System.IntPtr.Zero;
    }

    public cwipc get()
    {
        if (obj == System.IntPtr.Zero)
        {
            Debug.LogError("cwipc.obj == NULL");
            return null;
        }
        var rv = API_cwipc_util.cwipc_source_get(obj);
        if (rv == System.IntPtr.Zero) return null;
        return new cwipc(rv);
    }
}

internal class source_from_cwicpc_dir : cwipc_source
{
    string[]    allFilenames;
    int         currentFile;
    IntPtr      decoder;

    internal source_from_cwicpc_dir(string dirname)
    {
        currentFile = 0;
        allFilenames = System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/" + dirname);

        decoder = API_cwipc_codec.cwipc_new_decoder();
        if (decoder == IntPtr.Zero)
        {
            Debug.LogError("Cannot create cwipc_new_decoder");
        }
    }

    public void free()
    {
    }

    public bool eof()
    {
        return allFilenames.Length == 0;
    }

    public bool available(bool wait)
    {
        return allFilenames.Length != 0;
    }

    public cwipc get()
    {
        if (decoder == IntPtr.Zero) {
            Debug.LogError("cwipc_decoder: no decoder available");
            return null;
        }

        string filename = allFilenames[currentFile];
        currentFile = (currentFile + 1) % allFilenames.Length;

        var bytes = System.IO.File.ReadAllBytes(filename);
        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
        API_cwipc_codec.cwipc_decoder_feed(decoder, ptr, bytes.Length);
        bool ok = API_cwipc_util.cwipc_source_available(decoder, true);
        if (!ok) {
            Debug.LogError("cwipc_decoder: no pointcloud available");
            return null;
        }
        var pc = API_cwipc_util.cwipc_source_get(decoder);
        if (pc == null) {
            Debug.LogError("cwipc_decoder: did not return a pointcloud");
            return null;
        }


        return new cwipc(pc);

    }
}

internal class source_from_ply_dir : cwipc_source
{
    Queue<string> allFilenames;

    internal source_from_ply_dir(string dirname)
    {
        allFilenames = new Queue<string>(System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/" + dirname));
    }

    public void free()
    {
    }

    public bool eof()
    {
        return allFilenames.Count == 0;
    }

    public bool available(bool wait)
    {
        return allFilenames.Count != 0;
    }


    public cwipc get()
    {
        if (allFilenames.Count == 0) return null;
        string filename = allFilenames.Dequeue();
        Debug.Log("xxxjack source_from_ply_dir now reading " + filename);
        System.IntPtr errorPtr = System.IntPtr.Zero;
        var rv = API_cwipc_util.cwipc_read(filename, 0, ref errorPtr);
        if (errorPtr != System.IntPtr.Zero)
        {
            string errorMessage = Marshal.PtrToStringAuto(errorPtr);
            Debug.LogError("cwipc_read returned error: " + errorMessage);
        }
        return new cwipc(rv);
    }
}

internal class source_from_cwicpc_socket : cwipc_source
{
    string hostname;
    int port;
    bool failed;
    IntPtr decoder;

    internal source_from_cwicpc_socket(string _hostname, int _port)
    {
        hostname = _hostname;
        port = _port;
        failed = false;
        decoder = API_cwipc_codec.cwipc_new_decoder();
        if (decoder == IntPtr.Zero)
        {
            Debug.LogError("Cannot create cwipc_new_decoder");
        }

    }

    public void free()
    {
    }

    public bool eof()
    {
        return failed;
    }

    public bool available(bool wait)
    {
        return !failed;
    }

    public cwipc get()
    {
        if (failed) return null;
        TcpClient clt = null;
        try
        {
            clt = new TcpClient(hostname, port);
        }
        catch (SocketException)
        {
            Debug.LogError("connection error");
        }
        if (clt == null)
        {
            failed = true;
            return null;
        }
        List<byte> allData = new List<byte>();
        using (NetworkStream stream = clt.GetStream())
        {
            Byte[] data = new Byte[1024];

            do
            {
                int numBytesRead = stream.Read(data, 0, data.Length);

                if (numBytesRead == data.Length)
                {
                    allData.AddRange(data);
                }
                else if (numBytesRead > 0)
                {
                    allData.AddRange(data.Take(numBytesRead));
                }
            } while (stream.DataAvailable);
        }
        byte[] bytes = allData.ToArray();
        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);

        API_cwipc_codec.cwipc_decoder_feed(decoder, ptr, bytes.Length);
        bool ok = API_cwipc_util.cwipc_source_available(decoder, true);
        if (!ok)
        {
            Debug.LogError("cwipc_decoder: no pointcloud available");
            return null;
        }
        var pc = API_cwipc_util.cwipc_source_get(decoder);
        if (pc == null)
        {
            Debug.LogError("cwipc_decoder: did not return a pointcloud");
            return null;
        }


        return new cwipc(pc);

    }
}

internal class source_from_sub : cwipc_source
{
    string url;
    int streamNumber;
    bool failed;
    IntPtr subHandle;
    IntPtr decoder;
    byte[] currentBuffer;
    IntPtr currentBufferPtr;

    internal source_from_sub(string _url, int _streamNumber)
    {
        failed = true;
        url = _url;
        streamNumber = _streamNumber;


        bool ok = setup_sub_environment();
        if (!ok)
        {
            Debug.LogError("setup_sub_environment failed");
            return;
        }

        subHandle = signals_unity_bridge_pinvoke.sub_create("source_from_sub");
        if (subHandle == IntPtr.Zero)
        {
            Debug.LogError("sub_create failed");
            return;
        }

        ok = signals_unity_bridge_pinvoke.sub_play(subHandle, url);
        if (!ok)
        {
            Debug.LogError("sub_play failed for " + url);
            return;
        }
        decoder = API_cwipc_codec.cwipc_new_decoder();
        if (decoder == IntPtr.Zero)
        {
            Debug.LogError("Cannot create cwipc_new_decoder");
            return;
        }

        failed = false;
    }

    internal bool setup_sub_environment()
    {
        signals_unity_bridge_pinvoke.SetPaths();

        IntPtr hMod = API_kernel.GetModuleHandle("signals-unity-bridge");
        if (hMod == IntPtr.Zero)
        {
            Debug.LogError("Cannot get handle on signals-unity-bridge, GetModuleHandle returned NULL.");
            return false;
        }
        StringBuilder modPath = new StringBuilder(255);
        int rv = API_kernel.GetModuleFileName(hMod, modPath, 255);
        if (rv < 0)
        {
            Debug.LogError("Cannot get filename for signals-unity-bridge, GetModuleFileName returned " + rv);
            //return false;
        }
        string dirName = Path.GetDirectoryName(modPath.ToString());
        Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", dirName);
        return true;
    }

    public void free()
    {
        if (subHandle != IntPtr.Zero)
        {
            signals_unity_bridge_pinvoke.sub_destroy(subHandle);
            subHandle = IntPtr.Zero;
            failed = true; // Not really failed, but reacts the same (nothing will work anymore)
        }
    }

    public bool eof()
    {
        return failed;
    }

    public bool available(bool wait)
    {
        return !failed;
    }

    public cwipc get()
    {
        signals_unity_bridge_pinvoke.FrameInfo info = new signals_unity_bridge_pinvoke.FrameInfo();
        if (failed) return null;
        int bytesNeeded = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, streamNumber, IntPtr.Zero, 0, ref info);
        if (bytesNeeded == 0)
        {
            return null;
        }

        if (currentBuffer == null || bytesNeeded > currentBuffer.Length) {
            Debug.Log("Needs more memory!!");
            currentBuffer = new byte[(int)(bytesNeeded * 1.3f)]; // Reserves 30% more.
            currentBufferPtr = Marshal.UnsafeAddrOfPinnedArrayElement(currentBuffer, 0);
        }

        int bytesRead = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, streamNumber, currentBufferPtr, bytesNeeded, ref info);
        if (bytesRead != bytesNeeded)
        {
            Debug.LogError("sub_grab_frame returned " + bytesRead + " bytes after promising " + bytesNeeded);
            return null;
        }
        

        API_cwipc_codec.cwipc_decoder_feed(decoder, currentBufferPtr, bytesNeeded);
        bool ok = API_cwipc_util.cwipc_source_available(decoder, true);
        if (!ok)
        {
            Debug.LogError("cwipc_decoder: no pointcloud available");
            return null;
        }
        var pc = API_cwipc_util.cwipc_source_get(decoder);
        if (pc == null)
        {
            Debug.LogError("cwipc_decoder: did not return a pointcloud");
            return null;
        }


        return new cwipc(pc);

    }
}


public class cwipc_util_pinvoke
{
        public static cwipc getOnePointCloudFromPly(string filename) {

//        System.IntPtr src = cwipc_synthetic();
//        return cwipc_source_get(src);
        System.IntPtr errorPtr = System.IntPtr.Zero;
        var rv = API_cwipc_util.cwipc_read(Application.streamingAssetsPath + "/" + filename, 0, ref errorPtr);
        if (errorPtr != System.IntPtr.Zero)
        {
            string errorMessage = Marshal.PtrToStringAuto(errorPtr);
            Debug.LogError("cwipc_read returned error: " + errorMessage);
        }
        return new cwipc(rv);
    }

    public static cwipc getOnePointCloudFromCWICPC(string filename)
    {
        var bytes = System.IO.File.ReadAllBytes(Application.streamingAssetsPath + "/"+ filename);
        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
        IntPtr decoder = API_cwipc_codec.cwipc_new_decoder();
        if (decoder == IntPtr.Zero)
        {
            Debug.LogError("Cannot create cwipc_new_decoder");
        }

        API_cwipc_codec.cwipc_decoder_feed(decoder, ptr, bytes.Length);
        bool ok = API_cwipc_util.cwipc_source_available(decoder, true);
        if (!ok)
        {
            Debug.LogError("cwipc_decoder: no pointcloud available");
            return null;
        }
        var pc = API_cwipc_util.cwipc_source_get(decoder);
        if (pc == null)
        {
            Debug.LogError("cwipc_decoder: did not return a pointcloud");
            return null;
        }
        

        return new cwipc(pc);
    }

    public static cwipc_source sourceFromSynthetic()
    {
        var rv = API_cwipc_util.cwipc_synthetic();
        if (rv == System.IntPtr.Zero) return null;
        return new cwipc_source_impl(rv);
    }

    public static cwipc_source sourceFromRealsense2()
    {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        var rv = API_cwipc_realsense2.cwipc_realsense2(ref errorPtr);
        if (errorPtr != System.IntPtr.Zero)
        {
            string errorMessage = Marshal.PtrToStringAuto(errorPtr);
            Debug.LogError("cwipc_realsense2 returned error: " + errorMessage);
        }
        if (rv == System.IntPtr.Zero) return null;
        return new cwipc_source_impl(rv);
    }

    public static cwipc_source sourceFromCompressedDir(string dirname)
    {
        return new source_from_cwicpc_dir(dirname);
    }

    public static cwipc_source sourceFromNetwork(string hostname, int port)
    {
        return new source_from_cwicpc_socket(hostname, port);
    }
    public static cwipc_source sourceFromSUB(string url, int streamNumber)
    {
        return new source_from_sub(url, streamNumber);
    }

    public static cwipc_source sourceFromPlyDir(string dirname)
    {
        return new source_from_ply_dir(dirname);
    }
}
