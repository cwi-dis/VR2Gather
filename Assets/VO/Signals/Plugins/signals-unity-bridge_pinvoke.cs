using System;
using System.Runtime.InteropServices;

public class signals_unity_bridge_pinvoke {

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FrameInfo {
        Int64 timestamp;
    }

    [DllImport("signals-unity-bridge")]
    extern static public IntPtr sub_create([MarshalAs(UnmanagedType.LPStr)]string pipeline);

    [DllImport("signals-unity-bridge")]
    extern static public void sub_destroy(IntPtr handle);

    [DllImport("signals-unity-bridge")]
    extern static public int sub_get_stream_count(IntPtr handle);

    // bool sub_play(sub_handle* h, const char* URL);
    [DllImport("signals-unity-bridge")]
    extern static public bool sub_play(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)]string name);

    [DllImport("signals-unity-bridge")]
    extern static public int sub_grab_frame(IntPtr handle, int streamIndex, System.IntPtr dst, int dstLen, ref FrameInfo info);
}
