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
    public struct StreamDesc
    {
        public UInt32 MP4_4CC;
        public UInt32 objectX;    // In VRTogether, for pointclouds, we use this field for tileNumber
        public UInt32 objectY;    // In VRTogether, for pointclouds, we use this field for quality
        public UInt32 objectWidth;
        public UInt32 objectHeight;
        public UInt32 totalWidth;
        public UInt32 totalHeight;
    }

    protected class _API {

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        const string myDllName = "signals-unity-bridge.dll";
#else
        const string myDllName = "signals-unity-bridge.so";
#endif
        // The SUB_API_VERSION must match with the DLL version. Copy from signals_unity_bridge.h
        // after matching the API used here with that in the C++ code.
        const System.Int64 SUB_API_VERSION = 0x20200420A;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void MessageLogCallback([MarshalAs(UnmanagedType.LPStr)]string pipeline);

        // Creates a new pipeline.
        // name: a display name for log messages. Can be NULL.
        // The returned pipeline must be freed using 'sub_destroy'.
        // SUB_EXPORT sub_handle* sub_create(const char* name, void (* onError) (const char* msg), uint64_t api_version = SUB_API_VERSION);
        [DllImport(myDllName)]
        extern static public IntPtr sub_create([MarshalAs(UnmanagedType.LPStr)]string pipeline, MessageLogCallback callback, System.Int64 api_version = SUB_API_VERSION);

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

        // Enables a quality or disables a tile. There is at most one stream enabled per tile.
        // Associations between streamIndex and tiles are given by sub_get_stream_info().
        // Beware that disabling all qualities from all tiles will stop the session.
        // SUB_EXPORT bool sub_enable_stream(sub_handle* h, int tileNumber, int quality);
        // SUB_EXPORT bool sub_disable_stream(sub_handle* h, int tileNumber);
        [DllImport(myDllName)]
        extern static public bool sub_enable_stream(IntPtr handle, int tileNumber, int quality);
        [DllImport(myDllName)]
        extern static public bool sub_disable_stream(IntPtr handle, int tileNumber);

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

    public class connection : BaseMemoryChunk
    {
        protected System.IntPtr obj;

        internal connection(System.IntPtr _pointer) : base(_pointer)
        {
            if (_pointer == System.IntPtr.Zero)
            {
                throw new System.Exception("sub.connection: constructor called with null pointer");
            }
        }

        protected connection()
        {
            throw new System.Exception("sub.connection: default constructor called");
        }

        ~connection() {
            free();
        }

        public void onFree()
        {
            UnityEngine.Debug.Log("xxxjack calling sub_destroy");
            _API.sub_destroy(pointer);
        }

        public int get_stream_count()
        {
            if (pointer == System.IntPtr.Zero)
            {
                UnityEngine.Debug.LogAssertion("sub.get_stream_count: called with pointer==null");
            }
            return _API.sub_get_stream_count(pointer);
        }

        public uint get_stream_4cc(int stream) {
            if (pointer == System.IntPtr.Zero)
            {
                UnityEngine.Debug.LogAssertion("sub.get_stream_4cc: called with pointer==null");
            }
            StreamDesc streamDesc = new StreamDesc();
            _API.sub_get_stream_info(pointer, stream, ref streamDesc);
            return streamDesc.MP4_4CC;
        }

        public bool play(string name)
        {
            if (pointer == System.IntPtr.Zero)
            {
                UnityEngine.Debug.LogAssertion("sub.play: called with pointer==null");
            }
            return _API.sub_play(pointer, name);
        }

        public int grab_frame(int streamIndex, System.IntPtr dst, int dstLen, ref FrameInfo info)
        {
            if (pointer == System.IntPtr.Zero)
            {
                UnityEngine.Debug.LogAssertion("sub.grab_frame: called with pointer==null");
            }
            return _API.sub_grab_frame(pointer, streamIndex, dst, dstLen, ref info);
        }
        
    }

    public static connection create(string pipeline)
    {
        System.IntPtr obj;
        SetMSPaths();
        obj = _API.sub_create(pipeline, (msg)=> { UnityEngine.Debug.Log($"SUB: Internal message {msg}"); });
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
            string path = Environment.GetEnvironmentVariable("SIGNALS_SMD_PATH");
            if (path == "" || path == null)
            {
                path = Config.Instance.Macintosh.SIGNALS_SMD_PATH;
            }
            if (path == "" || path == null)
            {
                UnityEngine.Debug.LogError($"Environment variable SIGNALS_SMD_PATH must be set on MacOS");
            }
            Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", path);
            UnityEngine.Debug.Log($"xxxjack: mac-specific: SIGNALS_SMD_PATH=${path}");
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