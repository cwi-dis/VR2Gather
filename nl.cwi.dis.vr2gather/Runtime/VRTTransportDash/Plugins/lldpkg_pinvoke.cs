using System;
using System.Runtime.InteropServices;
using VRT.Core;
using Cwipc;

namespace VRT.Transport.Dash
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class lldpkg
    {

        public static int LogLevel = 0; // 0-Error, 1-Warn, 2-Info, 3-Debug
        // Delegate types to allow loading bin2dash before actually calling it (so we can get its pathname,
        // so we can tell it where its plugins are).
        private delegate IntPtr delegate_lldpkg_get_version();

        private class _API
        {
            public const string myDllName = "lldash_packager.so";

            // The BIN2DASH_API_VERSION must match with the DLL version. Copy from lldash_packager.hpp
            // after matching the API used here with that in the C++ code.
            const long LLDASH_PACKAGER_API_VERSION = 0x20250724;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate void LLDashPackagerErrorCallbackType([MarshalAs(UnmanagedType.LPStr)] string pipeline, int level);


            // Creates a new packager/streamer and starts the streaming session.
            // @MP4_4CC: codec identifier. Build with VRT_4CC(). For example VRT_4CC('c','w','i','1') for "cwi1".
            // The returned pipeline must be freed using vrt_destroy().
            [DllImport(myDllName)]
            extern static public IntPtr lldpkg_create([MarshalAs(UnmanagedType.LPStr)] string name, LLDashPackagerErrorCallbackType callback, int logLevel, int num_streams, DashStreamDescriptor[] streams, [MarshalAs(UnmanagedType.LPStr)] string publish_url = "", int seg_dur_in_ms = 10000, int timeshift_buffer_depth_in_ms = 30000, long api_version = LLDASH_PACKAGER_API_VERSION);


            // Destroys a pipeline. This frees all the resources.
            [DllImport(myDllName)]
            extern static public void lldpkg_destroy(IntPtr h, bool flush);

            // Pushes a buffer. The caller owns it ; the buffer  as it will be copied internally.
            [DllImport(myDllName)]
            extern static public bool lldpkg_push_buffer(IntPtr h, int stream_index, IntPtr buffer, uint bufferSize);

            // Gets the current media time in @timescale unit.
            [DllImport(myDllName)]
            extern static public Timestamp lldpkg_get_media_time(IntPtr h, int stream_index, int timescale);

            [DllImport(myDllName)]
            extern static public IntPtr lldpkg_get_version();
        }

        /// <summary>
        /// This class represents a connection to the lldash_packager library.
        /// It is used to ingest streams into lldash relay server.
        /// </summary> 
        public class connection : BaseMemoryChunk
        {

            static public readonly bool debugApi = true; // Could be a const but that gives warnings.
            public object errorCallback; // Hack: keep a reference to the error callback routine to work around GC issues.
            
            internal connection(IntPtr _pointer) : base(_pointer)
            {
                if (_pointer == IntPtr.Zero) throw new Exception("lldpkg.connection: constructor called with null pointer");
            }

            protected connection()
            {
                throw new Exception("lldpkg.connection: default constructor called");
            }

            ~connection()
            {
                if (_pointer != IntPtr.Zero)
                {
                    onfree();
                }
            }

            protected override void onfree()
            {
                IntPtr ptr = _pointer;
                _pointer = IntPtr.Zero;
                if (debugApi) UnityEngine.Debug.Log($"lldpkg_pinvoke: calling lldpkg_destroy()");
                _API.lldpkg_destroy(ptr, false);
                if (debugApi) UnityEngine.Debug.Log($"lldpkg_pinvoke: return from lldpkg_destroy()");
            }

            /// <summary>
            /// Pushes a buffer to the packager.
            /// The buffer must be allocated using Marshal.AllocHGlobal or similar, and will be copied internally.
            /// The caller owns the buffer and must free it after this call.
            /// </summary>
            /// <param name="stream_index">Index of the stream to which the buffer should be pushed</param>
            /// <param name="buffer">Pointer to the buffer to be pushed</param>
            /// <param name="bufferSize">Size of the buffer in bytes</param>
            /// <returns>True if the buffer was successfully pushed, false otherwise</returns>
            public bool push_buffer(int stream_index, IntPtr buffer, uint bufferSize)
            {
                if (pointer == IntPtr.Zero) throw new Exception($"lldpkg.push_buffer: called with pointer==null");
                if (debugApi) UnityEngine.Debug.Log($"lldpkg_pinvoke: calling lldpkg_push_buffer()");
                bool rv = _API.lldpkg_push_buffer(pointer, stream_index, buffer, bufferSize);
                if (debugApi) UnityEngine.Debug.Log($"lldpkg_pinvoke: return from lldpkg_push_buffer()");
                return rv;
            }

            /// <summary>
            /// Gets the current media time for a specific stream in the specified timescale.
            /// </summary>
            /// <param name="stream_index">Index of the stream for which to get the media time</param>
            /// <param name="timescale">Timescale in which the media time should be returned</param>
            /// <returns>The current media time in the specified timescale</returns>
            public Timestamp get_media_time(int stream_index, int timescale)
            {
                if (pointer == IntPtr.Zero) throw new Exception($"lldpkg.get_media_time: called with pointer==null");
                if (debugApi) UnityEngine.Debug.Log($"lldpkg_pinvoke: calling lldpkg_get_media_time()");
                Timestamp rv = _API.lldpkg_get_media_time(pointer, stream_index, timescale);
                if (debugApi) UnityEngine.Debug.Log($"lldpkg_pinvoke: return from lldpkg_get_media_time()");
                return rv;
            }
        }

        
        static void MessageCallback(string msg, int level)
        {
            string _msg = string.Copy(msg);
            if (level == 0)
            {
                UnityEngine.Debug.LogError($"lldpkg: asynchronous error: {_msg}. Attempting to continue.");
            }
            else if (level == 1)
            {
                UnityEngine.Debug.LogWarning($"lldpkg: asynchronous warning: {_msg}.");
            }
            else
            {
                UnityEngine.Debug.Log($"lldpkg: asynchronous message: {_msg}.");
            }
        }
        /// <summary>
        /// Creates a new lldpkg connection.
        /// This will load the lldash_packager dynamic library and create a new connection.
        /// The connection to the relay server is opened when you create it.
        /// </summary>
        /// <param name="pipeline">Name of the connection, used for error messages and such</param>
        /// <param name="descriptors">Array of stream descriptors, one for each stream</param>
        /// <param name="publish_url">URL to which the streams should be published.</param>
        /// <param name="seg_dur_in_ms">Duration of each segment in milliseconds</param>
        /// <param name="timeshift_buffer_depth_in_ms">Duration of the timeshift buffer. This is
        /// communicated to the relay server, and it determines how long the server will keep the 
        /// segments available for clients to play.</param>
        /// /// <returns>A new lldpkg.connection object</returns>
        public static connection create(string pipeline, DashStreamDescriptor[] descriptors, string publish_url = "", int seg_dur_in_ms = 10000, int timeshift_buffer_depth_in_ms = 30000)
        {
            Loader.PreLoadModule(_API.myDllName);
            try
            {
                delegate_lldpkg_get_version tmpDelegate = _API.lldpkg_get_version;
                IntPtr tmpPtr = Marshal.GetFunctionPointerForDelegate(tmpDelegate);
            }
            catch (System.DllNotFoundException)
            {
                UnityEngine.Debug.LogError($"lldpkg: Cannot load {_API.myDllName} dynamic library");
            }
            Loader.PostLoadModule(_API.myDllName);
            
            _API.LLDashPackagerErrorCallbackType errorCallback = MessageCallback;
            if (connection.debugApi) UnityEngine.Debug.Log($"lldpkg_pinvoke: calling lldpkg_create()");
            IntPtr obj = _API.lldpkg_create(pipeline, errorCallback, LogLevel, descriptors.Length, descriptors, publish_url, seg_dur_in_ms, timeshift_buffer_depth_in_ms);
            if (connection.debugApi) UnityEngine.Debug.Log($"lldpkg_pinvoke: return from lldpkg_create()");
            if (obj == IntPtr.Zero)
                return null;
            connection rv = new connection(obj);
            rv.errorCallback = errorCallback;
            return rv;
        }

        /// <summary>
        /// Returns the version of the lldpkg library.
        ///  </summary>
        public static string get_version()
        {
            Loader.PreLoadModule(_API.myDllName);
            try
            {
                IntPtr tmpPtr = _API.lldpkg_get_version();
                if (tmpPtr == IntPtr.Zero)
                    return "unknown";
                return Marshal.PtrToStringAnsi(tmpPtr);
            }
            catch (System.DllNotFoundException)
            {
                UnityEngine.Debug.LogError($"lldpkg: Cannot load {_API.myDllName} dynamic library");
                return "unknown";
            }
            finally
            {
                Loader.PostLoadModule(_API.myDllName);
            }
        }
    }
}