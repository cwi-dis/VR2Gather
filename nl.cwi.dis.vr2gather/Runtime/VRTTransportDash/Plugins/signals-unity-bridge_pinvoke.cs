using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.Transport.Dash
{
    using Timestamp = System.Int64;
    using IncomingStreamDescription = Cwipc.StreamSupport.IncomingStreamDescription;

    public class sub
    {
        const int MAX_SUB_MESSAGE_LEVEL = 0; // 0-Error, 1-Warn, 2-Info, 3-Debug

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DashStreamDescriptor
        {
            public uint MP4_4CC;
            public uint tileNumber;    // objectX. In VRTogether, for pointclouds, we use this field for tileNumber
            public int nx;    // objectY. In VRTogether, for pointclouds, we use this field for nx
            public int ny;    // objectWidth. In VRTogether, for pointclouds, we use this field for ny
            public int nz;    // objectHeight. In VRTogether, for pointclouds, we use this field for nz
            public uint totalWidth;
            public uint totalHeight;
        }

        public struct FrameInfo
        {
            /// <summary>
            /// Presentation timestamp (milliseconds).
            /// </summary>
            public Timestamp timestamp;
            /// <summary>
            /// Per-frame metadata carried by Dash packets.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] dsi;
            /// <summary>
            /// Length of dsi.
            /// </summary>
            public int dsi_size;
        }

        private delegate IntPtr delegate_sub_create(string pipeline, _API.MessageLogCallback callback, int maxLevel, long api_version);

        protected class _API
        {

            public const string myDllName = "signals-unity-bridge.so";
            // The SUB_API_VERSION must match with the DLL version. Copy from signals_unity_bridge.h
            // after matching the API used here with that in the C++ code.
            const long SUB_API_VERSION = 0x20210729A;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate void MessageLogCallback([MarshalAs(UnmanagedType.LPStr)]string pipeline, int level);

            // Creates a new pipeline.
            // name: a display name for log messages. Can be NULL.
            // The returned pipeline must be freed using 'sub_destroy'.
            // SUB_EXPORT sub_handle* sub_create(const char* name, void (* onError) (const char* msg), uint64_t api_version = SUB_API_VERSION);
            [DllImport(myDllName)]
            extern static public IntPtr sub_create([MarshalAs(UnmanagedType.LPStr)]string pipeline, MessageLogCallback callback, int maxLevel, long api_version = SUB_API_VERSION);

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
            extern static public bool sub_get_stream_info(IntPtr handle, int streamIndex, ref DashStreamDescriptor desc);

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
            extern static public int sub_grab_frame(IntPtr handle, int streamIndex, IntPtr dst, int dstLen, ref FrameInfo info);


        }

        public class connection : BaseMemoryChunk
        {
            protected IntPtr obj;
            public object errorCallback; // Hack: keep a reference to the error callback routine to work around GC issues.

            internal connection(IntPtr _pointer) : base(_pointer)
            {
                if (_pointer == IntPtr.Zero)
                {
                    throw new Exception("sub.connection: constructor called with null pointer");
                }
            }

            protected connection()
            {
                throw new Exception("sub.connection: default constructor called");
            }

            ~connection()
            {
                free();
            }

            protected override void onfree()
            {
                _API.sub_destroy(pointer);
            }

            public int get_stream_count()
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("sub.get_stream_count: called with pointer==null");
                }
                return _API.sub_get_stream_count(pointer);
            }

            public uint get_stream_4cc(int stream)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("sub.get_stream_4cc: called with pointer==null");
                }
                DashStreamDescriptor streamDesc = new DashStreamDescriptor();
                _API.sub_get_stream_info(pointer, stream, ref streamDesc);
                return streamDesc.MP4_4CC;
            }

            public IncomingStreamDescription[] get_streams()
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("sub.get_streams: called with pointer==null");
                }
                int nStreams = _API.sub_get_stream_count(pointer);
                IncomingStreamDescription[] rv = new IncomingStreamDescription[nStreams];
                for (int streamIndex = 0; streamIndex < nStreams; streamIndex++)
                {
                    DashStreamDescriptor streamDesc = new DashStreamDescriptor();
                    _API.sub_get_stream_info(pointer, streamIndex, ref streamDesc);
                    rv[streamIndex].streamIndex = streamIndex;
                    rv[streamIndex].tileNumber = (int)streamDesc.tileNumber;
                    float nx = ((float)streamDesc.nx) / 1000.0f;
                    float ny = ((float)streamDesc.ny) / 1000.0f;
                    float nz = ((float)streamDesc.nz) / 1000.0f;
                    rv[streamIndex].orientation = new Vector3(nx, ny, nz);
                }
                return rv;
            }

            public bool enable_stream(int tileNumber, int quality)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("sub.enable_stream: called with pointer==null");
                }
                return _API.sub_enable_stream(pointer, tileNumber, quality);
            }

            public bool disable_stream(int tileNumber)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("sub.disable_stream: called with pointer==null");
                }
                return _API.sub_disable_stream(pointer, tileNumber);
            }

            public bool play(string name)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("sub.play: called with pointer==null");
                }
                return _API.sub_play(pointer, name);
            }

            public int grab_frame(int streamIndex, IntPtr dst, int dstLen, ref FrameInfo info)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("sub.grab_frame: called with pointer==null");
                }
                return _API.sub_grab_frame(pointer, streamIndex, dst, dstLen, ref info);
            }
        }

        public static connection create(string pipeline)
        {
            try
            {
                delegate_sub_create tmpDelegate = _API.sub_create;
                IntPtr tmpPtr = Marshal.GetFunctionPointerForDelegate(tmpDelegate);
            }
            catch (System.DllNotFoundException)
            {
                UnityEngine.Debug.LogError($"bin2dash: Cannot load {_API.myDllName} dynamic library");
            }
            SetMSPaths(_API.myDllName);
            _API.MessageLogCallback errorCallback = (msg, level) =>
            {
                string _pipeline = pipeline == null ? "unknown pipeline" : string.Copy(pipeline);
                string _msg = string.Copy(msg);
                if (level == 0)
                {
                    UnityEngine.Debug.LogError($"{_pipeline}: asynchronous error: {_msg}. Attempting to continue.");
                }
                else
                if (level == 1)
                {
                    UnityEngine.Debug.LogWarning($"{_pipeline}: asynchronous warning: {_msg}.");
                }
                else
                {
                    UnityEngine.Debug.Log($"{_pipeline}: asynchronous message: {_msg}.");
                }
            };
            IntPtr obj = _API.sub_create(pipeline, errorCallback, MAX_SUB_MESSAGE_LEVEL);
            if (obj == IntPtr.Zero)
                return null;
            connection rv = new connection(obj);
            rv.errorCallback = errorCallback;
            return rv;
        }

        private static bool didSetMSPaths = false;

        // This could be either here or in bin2dash_pinvoke. 
        public static void SetMSPaths(string module_base)
        {
            if (didSetMSPaths) return;

            IntPtr hMod = API_kernel.GetModuleHandle(module_base);
            if (hMod == IntPtr.Zero)
            {
                UnityEngine.Debug.Log($"sub.SetMSPaths: Cannot get handle for {module_base}, GetModuleHandle returned NULL. PATH={Environment.GetEnvironmentVariable("PATH")}, SIGNALS_SMD_PATH={Environment.GetEnvironmentVariable("SIGNALS_SMD_PATH")} ");
                UnityEngine.Debug.LogError($"Cannot GetModuleHandle({module_base}). Try re-installing the application");
                return;
            }
            StringBuilder modPath = new StringBuilder(255);
            int rv = API_kernel.GetModuleFileName(hMod, modPath, 255);
            if (rv < 0)
            {
                UnityEngine.Debug.Log($"sub.SetMSPaths: Cannot get filename for {module_base}, handle={hMod}, GetModuleFileName returned " + rv);
                UnityEngine.Debug.LogError($"Cannot get filename for {module_base} from handle. Try re-installing the application");
                return;
            }
            string dirName = Path.GetDirectoryName(modPath.ToString());
            dirName = dirName.Replace("\\", "/");

            UnityEngine.Debug.Log($"sub.SetMSPaths: SIGNALS_SMD_PATH={dirName}");
            Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", dirName);
            didSetMSPaths = true;
        }
    }
}