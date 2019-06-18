using System;
using System.Runtime.InteropServices;

public class bin2dash_pinvoke
{

    static public uint VRT_4CC(char a, char b, char c, char d) { return (uint)((a << 24) | (b << 16) | (c << 8) | d); }
    // Creates a new packager/streamer and starts the streaming session.
    // @MP4_4CC: codec identifier. Build with VRT_4CC(). For example VRT_4CC('c','w','i','1') for "cwi1".
    // The returned pipeline must be freed using vrt_destroy().
    [DllImport("bin2dash")]
    extern static public IntPtr vrt_create([MarshalAs(UnmanagedType.LPStr)]string name, UInt32 MP4_4CC, [MarshalAs(UnmanagedType.LPStr)]string publish_url = "", int seg_dur_in_ms = 10000, int timeshift_buffer_depth_in_ms = 30000);

    // Destroys a pipeline. This frees all the resources.
    [DllImport("bin2dash")]
    extern static public void vrt_destroy(IntPtr h);

    // Pushes a buffer. The caller owns it ; the buffer  as it will be copied internally.
    [DllImport("bin2dash")]
    extern static public bool vrt_push_buffer(IntPtr h, IntPtr buffer, uint bufferSize);

    // Gets the current media time in @timescale unit.
    [DllImport("bin2dash")]
    extern static public long vrt_get_media_time(IntPtr h, int timescale);

    public static void SetPaths()
    {
        _setPaths();
    }

    private static void _setPaths([System.Runtime.CompilerServices.CallerFilePath]string path = "")
    {
        path = UnityEngine.Application.isEditor ? System.IO.Path.GetDirectoryName(path) : UnityEngine.Application.dataPath + "/Plugins";
        //        Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", path );
        //        Environment.SetEnvironmentVariable("PATH", path );

    }
}

