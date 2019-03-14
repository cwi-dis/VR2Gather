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
    [DllImport("signals-unity-bridge")]
    extern static public IntPtr sub_create([MarshalAs(UnmanagedType.LPStr)]string pipeline);

    // Destroys a pipeline. This frees all the resources.
    [DllImport("signals-unity-bridge")]
    extern static public void sub_destroy(IntPtr handle);

    // Returns the number of compressed streams.
    [DllImport("signals-unity-bridge")]
    extern static public int sub_get_stream_count(IntPtr handle);

    // Plays a given URL.
    [DllImport("signals-unity-bridge")]
    extern static public bool sub_play(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)]string name);

    // Copy the next received compressed frame to a buffer.
    // Returns: the size of compressed data actually copied,
    // or zero, if no frame was available for this stream.
    // If 'dst' is null, the frame will not be dequeued, but its size will be returned.
    [DllImport("signals-unity-bridge")]
    extern static public int sub_grab_frame(IntPtr handle, int streamIndex, System.IntPtr dst, int dstLen, ref FrameInfo info);
}
