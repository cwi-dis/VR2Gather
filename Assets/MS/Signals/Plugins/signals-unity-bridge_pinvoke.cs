using System;
using System.Runtime.InteropServices;

public class signals_unity_bridge_pinvoke {

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FrameInfo {
        // presentation timestamp, in milliseconds units.
        Int64 timestamp;
    }

    // Creates a new pipeline.
    // name: a display name for log messages. Can be NULL.
    // The returned pipeline must be freed using 'sub_destroy'.
    // SUB_EXPORT sub_handle* sub_create(const char* name);
    [DllImport("signals-unity-bridge")]
    extern static public IntPtr sub_create([MarshalAs(UnmanagedType.LPStr)]string pipeline);

    // Destroys a pipeline. This frees all the resources.
    // SUB_EXPORT void sub_destroy(sub_handle* h);
    [DllImport("signals-unity-bridge")]
    extern static public void sub_destroy(IntPtr handle);

    // Returns the number of compressed streams.
    // SUB_EXPORT int sub_get_stream_count(sub_handle* h);
    [DllImport("signals-unity-bridge")]
    extern static public int sub_get_stream_count(IntPtr handle);

    // Plays a given URL.
    // SUB_EXPORT bool sub_play(sub_handle* h, const char* URL);
    [DllImport("signals-unity-bridge")]
    extern static public bool sub_play(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)]string name);

    // Copy the next received compressed frame to a buffer.
    // Returns: the size of compressed data actually copied,
    // or zero, if no frame was available for this stream.
    // If 'dst' is null, the frame will not be dequeued, but its size will be returned.
    // SUB_EXPORT size_t sub_grab_frame(sub_handle* h, int streamIndex, uint8_t* dst, size_t dstLen, FrameInfo* info);
    [DllImport("signals-unity-bridge")]
    extern static public int sub_grab_frame(IntPtr handle, int streamIndex, System.IntPtr dst, int dstLen, ref FrameInfo info);


    public static void SetPaths() {
        _setPaths();
    }

    private static void _setPaths([System.Runtime.CompilerServices.CallerFilePath]string path = "") {
        path = UnityEngine.Application.isEditor ? System.IO.Path.GetDirectoryName( path ) : UnityEngine.Application.dataPath + "/Plugins";
        //Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", path );
        //Environment.SetEnvironmentVariable("Path", path);
    }
}