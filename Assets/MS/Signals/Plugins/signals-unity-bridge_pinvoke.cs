using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public class sub
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FrameInfo {
        // presentation timestamp, in milliseconds units.
        public Int64 timestamp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] dsi;
        public int dsi_size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StreamDesc {
        public uint MP4_4CC;
        public uint tileNumber;
        public uint quality;
    }
    
    protected class _API
    {
        const string myDllName = "signals-unity-bridge.so";

        // Creates a new pipeline.
        // name: a display name for log messages. Can be NULL.
        // The returned pipeline must be freed using 'sub_destroy'.
        // SUB_EXPORT sub_handle* sub_create(const char* name);
        [DllImport(myDllName)]
        extern static public IntPtr sub_create([MarshalAs(UnmanagedType.LPStr)]string pipeline);

        // Destroys a pipeline. This frees all the resources.
        // SUB_EXPORT void sub_destroy(sub_handle* h);
        [DllImport(myDllName)]
        extern static public void sub_destroy(IntPtr handle);


        // Returns the number of compressed streams.
        // SUB_EXPORT int sub_get_stream_count(sub_handle* h);
        [DllImport(myDllName)]
        extern static public int sub_get_stream_count(IntPtr handle);


        // Returns the 4CC of a given stream. Desc is owned by the caller.
        // SUB_EXPORT bool sub_get_stream_info(sub_handle* h, int streamIndex, struct streamDesc *desc);;
        [DllImport(myDllName)]
        extern static public bool sub_get_stream_info(IntPtr handle, int streamIndex, ref StreamDesc desc);

        // Plays a given URL.
        // SUB_EXPORT bool sub_play(sub_handle* h, const char* URL);
        [DllImport(myDllName)]
        extern static public bool sub_play(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)]string name);

        // Copy the next received compressed frame to a buffer.
        // Returns: the size of compressed data actually copied,
        // or zero, if no frame was available for this stream.
        // If 'dst' is null, the frame will not be dequeued, but its size will be returned.
        // SUB_EXPORT size_t sub_grab_frame(sub_handle* h, int streamIndex, uint8_t* dst, size_t dstLen, FrameInfo* info);
        [DllImport(myDllName)]
        extern static public int sub_grab_frame(IntPtr handle, int streamIndex, System.IntPtr dst, int dstLen, ref FrameInfo info);


    }

    public class connection
    {
        protected System.IntPtr obj;

        internal connection(System.IntPtr _obj)
        {
            if (_obj == System.IntPtr.Zero)
            {
                throw new System.Exception("sub.connection: constructor called with null pointer");
            }
            obj = _obj;
        }

        protected connection()
        {
            throw new System.Exception("sub.connection: default constructor called");
        }

        ~connection() {
            free();
        }

        public void free() {
            if (obj != System.IntPtr.Zero) {
                _API.sub_destroy(obj);
                obj = System.IntPtr.Zero;
            }
        }

        public int get_stream_count()
        {
            return _API.sub_get_stream_count(obj);
        }

        public uint get_stream_4cc(int stream) {
            StreamDesc streamDesc = new StreamDesc();
            _API.sub_get_stream_info(obj, stream, ref streamDesc);
            return streamDesc.MP4_4CC;
        }

        public bool play(string name)
        {
            return _API.sub_play(obj, name);
        }

        public int grab_frame(int streamIndex, System.IntPtr dst, int dstLen, ref FrameInfo info)
        {
            return _API.sub_grab_frame(obj, streamIndex, dst, dstLen, ref info);
        }
        
    }

    public static connection create(string pipeline)
    {
        System.IntPtr obj;
        SetMSPaths();
        obj = _API.sub_create(pipeline);
        if (obj == System.IntPtr.Zero)
            return null;
        return new connection(obj);
    }

    private static string lastMSpathInstalled = "";

    // This could be either here or in bin2dash_pinvoke. 
    public static void SetMSPaths(string module_base = "signals-unity-bridge")
    {
        if (UnityEngine.Application.platform == UnityEngine.RuntimePlatform.OSXEditor)
        {
            // xxxjack should we use another way to find the path?
            return;
        }
        if (lastMSpathInstalled == module_base) return;

        IntPtr hMod = API_kernel.GetModuleHandle(module_base);
        if (hMod == IntPtr.Zero)
        {
            UnityEngine.Debug.LogError($"sub.SetMSPaths: Cannot get handle on {module_base}, GetModuleHandle returned NULL. PATH={Environment.GetEnvironmentVariable("PATH")}, SIGNALS_SMD_PATH={Environment.GetEnvironmentVariable("SIGNALS_SMD_PATH")} ");
            return;
        }
        StringBuilder modPath = new StringBuilder(255);
        int rv = API_kernel.GetModuleFileName(hMod, modPath, 255);
        if (rv < 0)
        {
            UnityEngine.Debug.LogError($"sub.SetMSPaths: Cannot get filename for {module_base}, handle={hMod}, GetModuleFileName returned " + rv);
            //return false;
        }
        string dirName = Path.GetDirectoryName(modPath.ToString());
        //UnityEngine.Debug.Log($"sub.SetMSPaths: SIGNALS_SMD_PATH={dirName}");
        Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", dirName);
        lastMSpathInstalled = module_base;
    }
}