using System;
using System.Runtime.InteropServices;
using VRT.Core;
using Cwipc;

namespace VRT.Transport.Dash
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class bin2dash
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct StreamDesc
        {
            public uint MP4_4CC;
            public uint tileNumber;    // objectX. In VRTogether, for pointclouds, we use this field for tileNumber
            public int nx;    // objectY. In VRTogether, for pointclouds, we use this field for nx
            public int ny;    // objectWidth. In VRTogether, for pointclouds, we use this field for ny
            public int nz;    // objectHeight. In VRTogether, for pointclouds, we use this field for nz
            public uint totalWidth;
            public uint totalHeight;
        }

        // Delegate types to allow loading bin2dash before actually calling it (so we can get its pathname,
        // so we can tell it where its plugins are).
        private delegate IntPtr delegate_vrt_get_version();

        private class _API
        {
            public const string myDllName = "bin2dash.so";

            // The BIN2DASH_API_VERSION must match with the DLL version. Copy from bin2dash.hpp
            // after matching the API used here with that in the C++ code.
            const long BIN2DASH_API_VERSION = 0x20250620B;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate void MessageLogCallback([MarshalAs(UnmanagedType.LPStr)] string pipeline, int level);


            // Creates a new packager/streamer and starts the streaming session.
            // @MP4_4CC: codec identifier. Build with VRT_4CC(). For example VRT_4CC('c','w','i','1') for "cwi1".
            // The returned pipeline must be freed using vrt_destroy().
            [DllImport(myDllName)]
            extern static public IntPtr vrt_create_ext2([MarshalAs(UnmanagedType.LPStr)] string name, MessageLogCallback callback, int num_streams, StreamDesc[] streams, [MarshalAs(UnmanagedType.LPStr)] string publish_url = "", int seg_dur_in_ms = 10000, int timeshift_buffer_depth_in_ms = 30000, long api_version = BIN2DASH_API_VERSION);


            // Destroys a pipeline. This frees all the resources.
            [DllImport(myDllName)]
            extern static public void vrt_destroy(IntPtr h);

            // Pushes a buffer. The caller owns it ; the buffer  as it will be copied internally.
            [DllImport(myDllName)]
            extern static public bool vrt_push_buffer_ext(IntPtr h, int stream_index, IntPtr buffer, uint bufferSize);

            // Gets the current media time in @timescale unit.
            [DllImport(myDllName)]
            extern static public Timestamp vrt_get_media_time_ext(IntPtr h, int stream_index, int timescale);

            [DllImport(myDllName)]
            extern static public IntPtr vrt_get_version();
        }

        public class connection : BaseMemoryChunk
        {

            internal connection(IntPtr _pointer) : base(_pointer)
            {
                if (_pointer == IntPtr.Zero) throw new Exception("bin2dash.connection: constructor called with null pointer");
            }

            protected connection()
            {
                throw new Exception("bin2dash.connection: default constructor called");
            }

            ~connection()
            {
                free();
            }

            protected override void onfree()
            {
                _API.vrt_destroy(pointer);
            }

            public bool push_buffer(int stream_index, IntPtr buffer, uint bufferSize)
            {
                if (pointer == IntPtr.Zero) throw new Exception($"bin2dash.push_buffer: called with pointer==null");
                return _API.vrt_push_buffer_ext(pointer, stream_index, buffer, bufferSize);
            }

            public Timestamp get_media_time(int stream_index, int timescale)
            {
                if (pointer == IntPtr.Zero) throw new Exception($"bin2dash.get_media_time: called with pointer==null");
                return _API.vrt_get_media_time_ext(pointer, stream_index, timescale);
            }
        }

        public static connection create(string name, StreamDesc[] descriptors, string publish_url = "", int seg_dur_in_ms = 10000, int timeshift_buffer_depth_in_ms = 30000)
        {
            Loader.PreLoadModule(_API.myDllName);
            try
            {
                delegate_vrt_get_version tmpDelegate = _API.vrt_get_version;
                IntPtr tmpPtr = Marshal.GetFunctionPointerForDelegate(tmpDelegate);
            }
            catch (System.DllNotFoundException)
            {
                UnityEngine.Debug.LogError($"bin2dash: Cannot load {_API.myDllName} dynamic library");
            }
            Loader.PostLoadModule(_API.myDllName);

            _API.MessageLogCallback errorCallback = (msg, level) =>
            {
                string _msg = string.Copy(msg);
                if (level == 0)
                {
                    UnityEngine.Debug.LogError($"bin2dash: asynchronous error: {_msg}. Attempting to continue.");
                }
                else
                if (level == 1)
                {
                    UnityEngine.Debug.LogWarning($"bin2dash: asynchronous warning: {_msg}.");
                }
                else
                {
                    UnityEngine.Debug.Log($"bin2dash: asynchronous message: {_msg}.");
                }
            };
            IntPtr obj = _API.vrt_create_ext2(name, errorCallback, descriptors.Length, descriptors, publish_url, seg_dur_in_ms, timeshift_buffer_depth_in_ms);
            if (obj == IntPtr.Zero)
                return null;
            return new connection(obj);
        }

        public static string get_version()
        {
            Loader.PreLoadModule(_API.myDllName);
            try
            {
                IntPtr tmpPtr = _API.vrt_get_version();
                if (tmpPtr == IntPtr.Zero)
                    return "unknown";
                return Marshal.PtrToStringAnsi(tmpPtr);
            }
            catch (System.DllNotFoundException)
            {
                UnityEngine.Debug.LogError($"sub: Cannot load {_API.myDllName} dynamic library");
                return "unknown";
            }
            finally
            {
                Loader.PostLoadModule(_API.myDllName);
            }
        }
    }
}