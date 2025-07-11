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
        const int MAX_LLDPLAY_MESSAGE_LEVEL = 0; // 0-Error, 1-Warn, 2-Info, 3-Debug

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

        private delegate IntPtr delegate_lldplay_get_version();

        protected class _API
        {

            public const string myDllName = "lldash_play.so";
            // The LLDASH_PLAYOUT_API_VERSION must match with the DLL version. Copy from signals_unity_bridge.h
            // after matching the API used here with that in the C++ code.
            const long LLDASH_PLAYOUT_API_VERSION = 0x20250620A;

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
            extern static public int lldplay_grab_frame(IntPtr handle, int streamIndex, IntPtr dst, int dstLen, ref FrameInfo info);

            [DllImport(myDllName)]
            extern static public IntPtr lldplay_get_version();

        }

        public class connection : BaseMemoryChunk
        {
            const bool debugApi = false;
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
                } else
                {
                    UnityEngine.Debug.LogWarning("lldplay.onfree: double free");
                }
            }

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

            public bool play(string name)
            {
                if (pointer == IntPtr.Zero)
                {
                    UnityEngine.Debug.LogAssertion("lldplay.play: called with pointer==null");
                }
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: calling lldplay_play()");
                var rv = _API.lldplay_play(pointer, name);
                if (debugApi) UnityEngine.Debug.Log("signals_unity_bridge_api: return_from lldplay_play()");
                return rv;
            }

            public int grab_frame(int streamIndex, IntPtr dst, int dstLen, ref FrameInfo info)
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
            IntPtr obj = _API.lldplay_create(pipeline, errorCallback, MAX_LLDPLAY_MESSAGE_LEVEL);
            if (obj == IntPtr.Zero)
                return null;
            connection rv = new connection(obj);
            rv.errorCallback = errorCallback;
            return rv;
        }

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