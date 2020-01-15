using System;
using System.Runtime.InteropServices;

public class bin2dash
{
    private class _API
    {
        const string myDllName = "bin2dash.so";
        // Creates a new packager/streamer and starts the streaming session.
        // @MP4_4CC: codec identifier. Build with VRT_4CC(). For example VRT_4CC('c','w','i','1') for "cwi1".
        // The returned pipeline must be freed using vrt_destroy().
        [DllImport(myDllName)]
        extern static public IntPtr vrt_create([MarshalAs(UnmanagedType.LPStr)]string name, UInt32 MP4_4CC, [MarshalAs(UnmanagedType.LPStr)]string publish_url = "", int seg_dur_in_ms = 10000, int timeshift_buffer_depth_in_ms = 30000);

        // Destroys a pipeline. This frees all the resources.
        [DllImport(myDllName)]
        extern static public void vrt_destroy(IntPtr h);

        // Pushes a buffer. The caller owns it ; the buffer  as it will be copied internally.
        [DllImport(myDllName)]
        extern static public bool vrt_push_buffer(IntPtr h, IntPtr buffer, uint bufferSize);

        // Gets the current media time in @timescale unit.
        [DllImport(myDllName)]
        extern static public long vrt_get_media_time(IntPtr h, int timescale);
    }

    public class connection
    {
        protected System.IntPtr obj;

        internal connection(System.IntPtr _obj)
        {
            if (_obj == System.IntPtr.Zero)
            {
                UnityEngine.Debug.LogAssertion("bin2dash.connection: constructor called with null pointer");
            }
            obj = _obj;
        }

        protected connection()
        {
            UnityEngine.Debug.LogAssertion("bin2dash.connection: default constructor called");
        }

        ~connection() {
            free();
        }

        public void free() {
            if (obj != System.IntPtr.Zero) {
                _API.vrt_destroy(obj);
                obj = System.IntPtr.Zero;
            }
        }


        public bool push_buffer(IntPtr buffer, uint bufferSize)
        {
            if (obj == System.IntPtr.Zero)
            {
                UnityEngine.Debug.LogAssertion("bin2dash.push_buffer: called with obj==null");
            }
            return _API.vrt_push_buffer(obj, buffer, bufferSize);
        }

        public long get_media_time(int timescale)
        {
            if (obj == System.IntPtr.Zero)
            {
                UnityEngine.Debug.LogAssertion("bin2dash.get_media_time: called with obj==null");
            }
            return _API.vrt_get_media_time(obj, timescale);
        }
    }

    public static connection create(string name, UInt32 MP4_4CC, string publish_url = "", int seg_dur_in_ms = 10000, int timeshift_buffer_depth_in_ms = 30000)
    {
        System.IntPtr obj;
        sub.SetMSPaths("bin2dash.so");
        obj = _API.vrt_create(name, MP4_4CC, publish_url, seg_dur_in_ms, timeshift_buffer_depth_in_ms);
        if (obj == System.IntPtr.Zero)
            return null;
        return new connection(obj);
    }

    static public uint VRT_4CC(char a, char b, char c, char d)
    {
        return (uint)((a << 24) | (b << 16) | (c << 8) | d);
    }
}