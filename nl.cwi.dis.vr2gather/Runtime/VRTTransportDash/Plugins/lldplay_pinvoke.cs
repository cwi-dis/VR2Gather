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

    public class lldplay
    {
        public static int LogLevel = 0; // 0-Error, 1-Warn, 2-Info, 3-Debug


        public struct DashFrameMetaData
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

        private delegate IntPtr delegate_lldplay_get_version();

        protected class _API
        {

            public const string myDllName = "lldash_play.so";
            // The LLDASH_PLAYOUT_API_VERSION must match with the DLL version. Copy from lldash_play.h
            // after matching the API used here with that in the C++ code.
            const long LLDASH_PLAYOUT_API_VERSION = 0x20250722;

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate void LLDashPlayoutErrorCallbackType([MarshalAs(UnmanagedType.LPStr)] string pipeline, int level);

            // Creates a new pipeline.
            // name: a display name for log messages. Can be NULL.
            // The returned pipeline must be freed using 'lldplay_destroy'.
            [DllImport(myDllName)]
            extern static public IntPtr lldplay_create([MarshalAs(UnmanagedType.LPStr)] string pipeline, LLDashPlayoutErrorCallbackType callback, int maxLevel, long api_version = LLDASH_PLAYOUT_API_VERSION);

            // Destroys a pipeline. This frees all the resources.
            [DllImport(myDllName)]
            extern static public void lldplay_destroy(IntPtr handle);


            // Returns the number of compressed streams.
            [DllImport(myDllName)]
            extern static public int lldplay_get_stream_count(IntPtr handle);


            // Returns the 4CC of a given stream. Desc is owned by the caller.
            [DllImport(myDllName)]
            extern static public bool lldplay_get_stream_info(IntPtr handle, int streamIndex, ref DashStreamDescriptor desc);

            // Enables a quality or disables a tile. There is at most one stream enabled per tile.
            // Associations between streamIndex and tiles are given by lldplay_get_stream_info().
            // Beware that disabling all qualities from all tiles will stop the session.
            [DllImport(myDllName)]
            extern static public bool lldplay_enable_stream(IntPtr handle, int tileNumber, int quality);
            [DllImport(myDllName)]
            extern static public bool lldplay_disable_stream(IntPtr handle, int tileNumber);

            // Plays a given URL.
            [DllImport(myDllName)]
            extern static public bool lldplay_play(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string name);

            // Copy the next received compressed frame to a buffer.
            // Returns: the size of compressed data actually copied,
            // or zero, if no frame was available for this stream.
            // If 'dst' is null, the frame will not be dequeued, but its size will be returned.
            [DllImport(myDllName)]
            extern static public int lldplay_grab_frame(IntPtr handle, int streamIndex, IntPtr dst, int dstLen, ref DashFrameMetaData info);

            [DllImport(myDllName)]
            extern static public IntPtr lldplay_get_version();

        }

        /// <summary>
        /// lldplay.connection is a wrapper around a lldash_play pipeline.
        /// It is used to play a DASH stream.
        /// </summary>
        public class connection : BaseMemoryChunk
        {
            static readonly bool debugApi = false; // Could be a const but that gives warnings.
            protected IntPtr obj;
            public object errorCallback; // Hack: keep a reference to the error callback routine to work around GC issues.

            internal connection(IntPtr _pointer) : base(_pointer)
            {
                if (_pointer == IntPtr.Zero)
                {
                    throw new Exception("lldplay.connection: constructor called with null pointer");
                }
            }

            protected connection()
            {
                throw new Exception("lldplay.connection: default constructor called");
            }

            ~connection()
            {
                free();
            }

            protected override void onfree()
            {
                IntPtr tmp = _pointer;
                _pointer = IntPtr.Zero;
                if (tmp != IntPtr.Zero)
                {
                    if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: calling lldplay_destroy()");
                    _API.lldplay_destroy(tmp);
                    if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: return_from lldplay_destroy()");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("lldplay.onfree: double free");
                }
            }

            /// <summary>
            /// Returns the number of streams in the pipeline.
            /// </summary>
            /// <returns>Number of streams</returns>
            public int get_stream_count()
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("lldplay.get_stream_count: called with pointer==null");
                }
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: calling lldplay_get_stream_count()");
                var rv = _API.lldplay_get_stream_count(pointer);
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: return_from lldplay_get_stream_count()");
                return rv;
            }

            /// <summary>
            /// Returns the 4CC of a given stream.
            /// This is the same as the MP4 4CC (4 bytes binary string)
            /// </summary>
            /// <param name="stream">Stream number</param>
            /// <returns>4CC as a <c>uint</c></returns>
            public uint get_stream_4cc(int stream)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("lldplay.get_stream_4cc: called with pointer==null");
                }
                DashStreamDescriptor streamDesc = new DashStreamDescriptor();
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: calling lldplay_get_stream_info()");
                _API.lldplay_get_stream_info(pointer, stream, ref streamDesc);
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: return_from lldplay_get_stream_info()");
                return streamDesc.MP4_4CC;
            }

            /// <summary>
            /// Returns an array of <c>IncomingStreamDescription</c> objects, one for each stream.
            /// </summary>
            /// <returns>An array of <c>IncomingStreamDescription</c></returns>
            public IncomingStreamDescription[] get_streams()
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("lldplay.get_streams: called with pointer==null");
                }
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: calling lldplay_get_stream_count()");
                int nStreams = _API.lldplay_get_stream_count(pointer);
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: return_from lldplay_get_stream_count()");
                IncomingStreamDescription[] rv = new IncomingStreamDescription[nStreams];
                for (int streamIndex = 0; streamIndex < nStreams; streamIndex++)
                {
                    DashStreamDescriptor streamDesc = new DashStreamDescriptor();
                    if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: calling lldplay_get_stream_info()");
                    _API.lldplay_get_stream_info(pointer, streamIndex, ref streamDesc);
                    if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: return_from lldplay_get_stream_info()");
                    rv[streamIndex].streamIndex = streamIndex;
                    rv[streamIndex].tileNumber = (int)streamDesc.tileNumber;
                    float nx = ((float)streamDesc.nx) / 1000.0f;
                    float ny = ((float)streamDesc.ny) / 1000.0f;
                    float nz = ((float)streamDesc.nz) / 1000.0f;
                    rv[streamIndex].orientation = new Vector3(nx, ny, nz);
                }
                return rv;
            }

            /// <summary>
            /// Enable the stream for a specific quality level for a given tile.
            /// Disables all other streams for that tile.
            /// The actual switching may be delayed until the next DASH segment is started.
            /// </summary>
            /// <param name="tileNumber">Tile number for which to switch streams</param>
            /// <param name="quality">Quality level to enable</param>
            public bool enable_stream(int tileNumber, int quality)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("lldplay.enable_stream: called with pointer==null");
                }
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: calling lldplay_enable_stream()");
                var rv = _API.lldplay_enable_stream(pointer, tileNumber, quality);
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: return_from lldplay_enable_stream()");
                return rv;
            }

            /// <summary>
            /// Disable all streams for a specific tile.
            /// Note that disabling all streams for all tiles will stop the session.
            /// </summary>
            /// <param name="tileNumber">Tile number for which to disable streams</param>
            public bool disable_stream(int tileNumber)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("lldplay.disable_stream: called with pointer==null");
                }
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: calling lldplay_disable_stream()");
                var rv = _API.lldplay_disable_stream(pointer, tileNumber);
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: return_from lldplay_disable_stream()");
                return rv;
            }

            /// <summary>
            /// Play a DASH stream from a given URL.
            /// This will start the DASH session and begin receiving data.
            /// For each tile, the first available stream (quality level) will be started.
            /// </summary>
            /// <param name="url">URL of the DASH stream to play</param>
            /// <returns>True if the stream was started successfully, false otherwise</returns>
            public bool play(string url)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("lldplay.play: called with pointer==null");
                }
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: calling lldplay_play()");
                var rv = _API.lldplay_play(pointer, url);
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: return_from lldplay_play()");
                return rv;
            }

            /// <summary>
            /// Copy the next received compressed frame to a buffer, or return its size.
            /// If 'dst' is null, the frame will not be dequeued, but its size will be returned, so the
            /// caller can allocate a buffer of the right size.
            /// </summary>
            /// <param name="streamIndex">Stream index to grab the frame from</param>
            /// <param name="dst">Destination buffer to copy the frame to, or null to just get the size</param>
            /// <param name="dstLen">Length of the destination buffer</param>
            /// <param name="info">(out)Frame information, including timestamp and metadata</param>
            /// <returns>Size of the copied (or currently available) frame, or zero if no frame was available for this stream</returns>
            /// 
            public int grab_frame(int streamIndex, IntPtr dst, int dstLen, ref DashFrameMetaData info)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("lldplay.grab_frame: called with pointer==null");
                }
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: calling lldplay_grab_frame()");
                var rv = _API.lldplay_grab_frame(pointer, streamIndex, dst, dstLen, ref info);
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: return_from lldplay_grab_frame()");
                return rv;
            }
        }

        /// <summary>
        /// Creates a new lldplay connection.
        /// This will load the lldash_play dynamic library and create a new connection. The connection will
        /// not be opened until you call the <c>play</c> method.
        /// </summary>
        /// <param name="pipeline">Name of the pipeline, only for error messages and such</param>
        /// <returns>The <c>lldplay.connection</c></returns>
        public static connection create(string pipeline)
        {
            Loader.PreLoadModule(_API.myDllName);
            try
            {
                delegate_lldplay_get_version tmpDelegate = _API.lldplay_get_version;
                IntPtr tmpPtr = Marshal.GetFunctionPointerForDelegate(tmpDelegate);
            }
            catch (System.DllNotFoundException)
            {
                UnityEngine.Debug.LogError($"bin2dash: Cannot load {_API.myDllName} dynamic library");
            }
            Loader.PostLoadModule(_API.myDllName);
            _API.LLDashPlayoutErrorCallbackType errorCallback = (msg, level) =>
            {
                string _pipeline = pipeline == null ? "unknown lldplay pipeline" : string.Copy(pipeline);
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
            IntPtr obj = _API.lldplay_create(pipeline, errorCallback, LogLevel);
            if (obj == IntPtr.Zero)
                return null;
            connection rv = new connection(obj);
            rv.errorCallback = errorCallback;
            return rv;
        }

        /// <summary>
        /// Returns the version of the lldplay library.
        /// </summary>
        /// <returns>The version string</returns>
        public static string get_version()
        {
            Loader.PreLoadModule(_API.myDllName);
            try
            {
                IntPtr tmpPtr = _API.lldplay_get_version();
                if (tmpPtr == IntPtr.Zero)
                    return "unknown";
                return Marshal.PtrToStringAnsi(tmpPtr);
            }
            catch (System.DllNotFoundException)
            {
                UnityEngine.Debug.LogError($"lldplay: Cannot load {_API.myDllName} dynamic library");
                return "unknown";
            }
            finally
            {
                Loader.PostLoadModule(_API.myDllName);
            }
        }
    }
}