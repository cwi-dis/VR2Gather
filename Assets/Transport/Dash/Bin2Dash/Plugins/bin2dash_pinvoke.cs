using System;
using System.Runtime.InteropServices;
using VRTCore;

namespace VRT.Transport.Dash
{
    public class bin2dash
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct StreamDesc
        {
            public uint MP4_4CC;
            public uint tileNumber;    // objectX, officially. In VRTogether, for pointclouds, we use this field for tileNumber
            public uint quality;    // objectY, officially. In VRTogether, for pointclouds, we use this field for quality
            public uint objectWidth;
            public uint objectHeight;
            public uint totalWidth;
            public uint totalHeight;
        }

        private class _API
        {
            const string myDllName = "bin2dash.so";

            // The BIN2DASH_API_VERSION must match with the DLL version. Copy from bin2dash.hpp
            // after matching the API used here with that in the C++ code.
            const long BIN2DASH_API_VERSION = 0x20200327A;


            // Creates a new packager/streamer and starts the streaming session.
            // @MP4_4CC: codec identifier. Build with VRT_4CC(). For example VRT_4CC('c','w','i','1') for "cwi1".
            // The returned pipeline must be freed using vrt_destroy().
            [DllImport(myDllName)]
            extern static public IntPtr vrt_create_ext([MarshalAs(UnmanagedType.LPStr)]string name, int num_streams, StreamDesc[] streams, [MarshalAs(UnmanagedType.LPStr)]string publish_url = "", int seg_dur_in_ms = 10000, int timeshift_buffer_depth_in_ms = 30000, long api_version = BIN2DASH_API_VERSION);

            // Legacy API
            [DllImport(myDllName)]
            extern static public IntPtr vrt_create([MarshalAs(UnmanagedType.LPStr)]string name, uint MP4_4CC, [MarshalAs(UnmanagedType.LPStr)]string publish_url = "", int seg_dur_in_ms = 10000, int timeshift_buffer_depth_in_ms = 30000, long api_version = BIN2DASH_API_VERSION);

            // Destroys a pipeline. This frees all the resources.
            [DllImport(myDllName)]
            extern static public void vrt_destroy(IntPtr h);

            // Pushes a buffer. The caller owns it ; the buffer  as it will be copied internally.
            [DllImport(myDllName)]
            extern static public bool vrt_push_buffer_ext(IntPtr h, int stream_index, IntPtr buffer, uint bufferSize);
            // Legacy API
            [DllImport(myDllName)]
            extern static public bool vrt_push_buffer(IntPtr h, IntPtr buffer, uint bufferSize);

            // Gets the current media time in @timescale unit.
            [DllImport(myDllName)]
            extern static public long vrt_get_media_time(IntPtr h, int timescale);
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

            public long get_media_time(int timescale)
            {
                if (pointer == IntPtr.Zero) throw new Exception($"bin2dash.get_media_time: called with pointer==null");
                return _API.vrt_get_media_time(pointer, timescale);
            }
        }

        public static connection create(string name, StreamDesc[] descriptors, string publish_url = "", int seg_dur_in_ms = 10000, int timeshift_buffer_depth_in_ms = 30000)
        {
            IntPtr obj;
            sub.SetMSPaths("bin2dash.so");
            obj = _API.vrt_create_ext(name, descriptors.Length, descriptors, publish_url, seg_dur_in_ms, timeshift_buffer_depth_in_ms);
            if (obj == IntPtr.Zero)
                return null;
            return new connection(obj);
        }

        static public uint VRT_4CC(char a, char b, char c, char d)
        {
            return (uint)(a << 24 | b << 16 | c << 8 | d);
        }
    }
}