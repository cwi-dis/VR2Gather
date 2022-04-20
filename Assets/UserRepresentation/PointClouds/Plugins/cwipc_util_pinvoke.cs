﻿using System;
using System.Runtime.InteropServices;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class cwipc
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct encoder_params
        {
            public bool do_inter_frame;    /**< (unused in this version, must be false) do inter-frame compression */
            public int gop_size;           /**< (unused in this version, ignored) spacing of I frames for inter-frame compression */
            public float exp_factor;       /**< (unused in this version, ignored). Bounding box expansion factor for inter-frame coding */
            public int octree_bits;        /**< Octree depth: a fully populated octree will have 8**octree_bits points */
            public int jpeg_quality;       /**< JPEG encoding quality */
            public int macroblock_size;    /**< (unused in this version, ignored) macroblock size for inter-frame prediction */
            public int tilenumber;         /**< 0 for encoding full pointclouds, > 0 for selecting a single tile to encode */
            public float voxelsize;        /**< If non-zero run voxelizer with this cell size to get better tiled pointcloud */
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct vector
        {
            public double x;
            public double y;
            public double z;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct point
        {
            public float x;
            public float y;
            public float z;
            public byte r;
            public byte g;
            public byte b;
            public byte tile;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct tileinfo
        {
            public vector normal;
            public IntPtr cameraName;
            public byte ncamera;
            public byte cameraMask;
        };

        private class _API_cwipc_util
        {
            const string myDllName = "cwipc_util";
            public const ulong CWIPC_API_VERSION = 0x20220126;

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_read([MarshalAs(UnmanagedType.LPStr)]string filename, Timestamp timestamp, ref IntPtr errorMessage, ulong apiVersion = CWIPC_API_VERSION);
			[DllImport(myDllName)]
			internal extern static IntPtr cwipc_read_debugdump([MarshalAs(UnmanagedType.LPStr)]string filename, ref System.IntPtr errorMessage, System.UInt64 apiVersion = CWIPC_API_VERSION);
            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_from_packet(IntPtr packet, IntPtr size, ref System.IntPtr errorMessage, System.UInt64 apiVersion = CWIPC_API_VERSION);
            [DllImport(myDllName)]
            internal extern static void cwipc_free(IntPtr pc);
            [DllImport(myDllName)]
            internal extern static Timestamp cwipc_timestamp(IntPtr pc);
            [DllImport(myDllName)]
            internal extern static int cwipc_count(IntPtr pc);
            [DllImport(myDllName)]
            internal extern static float cwipc_cellsize(IntPtr pc);
            [DllImport(myDllName)]
            internal extern static void cwipc__set_cellsize(IntPtr pc, float cellsize);
            [DllImport(myDllName)]
            internal extern static void cwipc__set_timestamp(IntPtr pc, Timestamp timestamp);
            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_get_uncompressed_size(IntPtr pc);
            [DllImport(myDllName)]
            internal extern static int cwipc_copy_uncompressed(IntPtr pc, IntPtr data, IntPtr size);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_copy_packet(IntPtr pc, IntPtr packet, IntPtr size);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_source_get(IntPtr src);
            [DllImport(myDllName)]
            internal extern static bool cwipc_source_eof(IntPtr src);
            [DllImport(myDllName)]
            internal extern static bool cwipc_source_available(IntPtr src, bool available);
            [DllImport(myDllName)]
            internal extern static void cwipc_source_free(IntPtr src);

            [DllImport(myDllName)]
            internal extern static int cwipc_tiledsource_maxtile(IntPtr src);
            [DllImport(myDllName)]
            internal extern static bool cwipc_tiledsource_get_tileinfo(IntPtr src, int tileNum, [Out] out tileinfo _tileinfo);

            [DllImport(myDllName)]
            internal extern static int cwipc_sink_free(IntPtr sink);
            [DllImport(myDllName)]
            internal extern static int cwipc_sink_feed(IntPtr sink, IntPtr pc, bool clear);
            [DllImport(myDllName)]
            internal extern static int cwipc_sink_caption(IntPtr sink, [MarshalAs(UnmanagedType.LPStr)]string caption);
            [DllImport(myDllName)]
            internal extern static int cwipc_sink_interact(IntPtr sink, [MarshalAs(UnmanagedType.LPStr)]string prompt, [MarshalAs(UnmanagedType.LPStr)]string responses, int millis);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_synthetic(int fps, int npoints, ref IntPtr errorMessage, ulong apiVersion = CWIPC_API_VERSION);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_from_certh(IntPtr certhPC, float[] origin, float[] bbox, Timestamp timestamp, ref IntPtr errorMessage, ulong apiVersion = CWIPC_API_VERSION);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_proxy([MarshalAs(UnmanagedType.LPStr)]string ip, int port, ref IntPtr errorMessage, ulong apiVersion = CWIPC_API_VERSION);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_downsample(IntPtr pc, float voxelSize);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_tilefilter(IntPtr pc, int tilenum);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_tilemap(IntPtr pc, byte[] map);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_colormap(IntPtr pc, UInt32 clearBits, UInt32 setBits);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_crop(IntPtr pc, float[] bbox);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_join(IntPtr pc1, IntPtr pc2);

        }
        private class _API_cwipc_realsense2
        {
            const string myDllName = "cwipc_realsense2";

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_realsense2([MarshalAs(UnmanagedType.LPStr)]string filename, ref IntPtr errorMessage, ulong apiVersion = _API_cwipc_util.CWIPC_API_VERSION);
        }
        private class _API_cwipc_kinect
        {
            const string myDllName = "cwipc_kinect";

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_kinect([MarshalAs(UnmanagedType.LPStr)]string filename, ref IntPtr errorMessage, ulong apiVersion = _API_cwipc_util.CWIPC_API_VERSION);
        }
        private class _API_cwipc_codec
        {
            const string myDllName = "cwipc_codec";
            public const int CWIPC_ENCODER_PARAM_VERSION = 0x20190506;

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_new_decoder(ref IntPtr errorMessage, ulong apiVersion = _API_cwipc_util.CWIPC_API_VERSION);

            [DllImport(myDllName)]
            internal extern static void cwipc_decoder_feed(IntPtr dec, IntPtr compFrame, int len);

            [DllImport(myDllName)]
            internal extern static void cwipc_decoder_close(IntPtr dec);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_new_encoder(int paramVersion, ref encoder_params encParams, ref IntPtr errorMessage, ulong apiVersion = _API_cwipc_util.CWIPC_API_VERSION);

            [DllImport(myDllName)]
            internal extern static void cwipc_encoder_free(IntPtr enc);

            [DllImport(myDllName)]
            internal extern static void cwipc_encoder_close(IntPtr enc);

            [DllImport(myDllName)]
            internal extern static void cwipc_encoder_feed(IntPtr enc, IntPtr pc);

            [DllImport(myDllName)]
            internal extern static bool cwipc_encoder_available(IntPtr enc, bool wait);

            [DllImport(myDllName)]
            internal extern static bool cwipc_encoder_eof(IntPtr enc);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_encoder_get_encoded_size(IntPtr enc);

            [DllImport(myDllName)]
            internal extern static bool cwipc_encoder_copy_data(IntPtr enc, IntPtr data, IntPtr size);

            [DllImport(myDllName)]
            internal extern static bool cwipc_encoder_at_gop_boundary(IntPtr enc);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_new_encodergroup(ref IntPtr errorMessage, ulong apiVersion = _API_cwipc_util.CWIPC_API_VERSION);

            [DllImport(myDllName)]
            internal extern static void cwipc_encodergroup_free(IntPtr enc);

            [DllImport(myDllName)]
            internal extern static void cwipc_encodergroup_close(IntPtr enc);

            [DllImport(myDllName)]
            internal extern static IntPtr cwipc_encodergroup_addencoder(IntPtr enc, int paramVersion, ref encoder_params encParams, ref IntPtr errorMessage);

            [DllImport(myDllName)]
            internal extern static void cwipc_encodergroup_feed(IntPtr enc, IntPtr pc);

        }

        public class pointcloud : BaseMemoryChunk
        {
            internal pointcloud(IntPtr _pointer) : base(_pointer)
            {
                if (_pointer == IntPtr.Zero)
                    throw new Exception("cwipc.pointcloud called with NULL pointer argument");
                // This is a hack. We copy the timestamp from the cwipc data to our info structure.
                info.timestamp = timestamp();
            }

            ~pointcloud()
            {
                free();
            }

            protected override void onfree()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.pointcloud.onfree called with NULL pointer");
                _API_cwipc_util.cwipc_free(pointer);
            }

            public Timestamp timestamp()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.pointcloud.timestamp called with NULL pointer");
                return _API_cwipc_util.cwipc_timestamp(pointer);
            }

            public void _set_timestamp(Timestamp timestamp)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.pointcloud._set_timestamp called with NULL pointer");
                _API_cwipc_util.cwipc__set_timestamp(pointer, timestamp);
                info.timestamp = timestamp;
            }

            public int count()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.pointcloud.count called with NULL pointer");
                return _API_cwipc_util.cwipc_count(pointer);

            }

            public float cellsize()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.pointcloud.cellsize called with NULL pointer");
                return _API_cwipc_util.cwipc_cellsize(pointer);
            }
            public void _set_cellsize(float cellsize)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.pointcloud._set_cellsize called with NULL pointer");
                _API_cwipc_util.cwipc__set_cellsize(pointer, cellsize);
            }

            public int get_uncompressed_size()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.pointcloud.get_uncompressed_size called with NULL pointer");
                return (int)_API_cwipc_util.cwipc_get_uncompressed_size(pointer);
            }

            public int copy_uncompressed(IntPtr data, int size)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.pointcloud.copy_uncompressed called with NULL pointer");
                return _API_cwipc_util.cwipc_copy_uncompressed(pointer, data, (IntPtr)size);
            }

            public int copy_packet(IntPtr data, int size)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.pointcloud.copy_uncompressed called with NULL pointer");
                return (int)_API_cwipc_util.cwipc_copy_packet(pointer, data, (IntPtr)size);
            }

            public byte[] get_packet()
            {
                int size = copy_packet(IntPtr.Zero, 0);
                byte[] rv = new byte[size];
                int actualSize = 0;
                unsafe
                {
                    fixed (byte* rvPtr = rv)
                    {
                        actualSize = copy_packet((IntPtr)rvPtr, size);
                    }
                }
                if (actualSize != size)
                {
                    throw new System.Exception($"cwipc.get_packet: size={actualSize} after promising {size}");
                }
                return rv;
            }

            internal IntPtr _intptr()
            {
                return pointer;
            }
        }

        public class source : BaseMemoryChunk
        {
            internal source(IntPtr _pointer) : base(_pointer)
            {
                if (_pointer == IntPtr.Zero) throw new Exception("cwipc.source called with NULL pointer argument");
            }

            protected override void onfree()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.source.onfree called with NULL pointer");
                _API_cwipc_util.cwipc_source_free(pointer);
            }

            /* xxxjack need to check how this works with BaseMemoryChunk
            ~source() {
                free();
            }
            */
            public pointcloud get()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.source.get called with NULL pointer");
                IntPtr pc = _API_cwipc_util.cwipc_source_get(pointer);
                if (pc == IntPtr.Zero) return null;
                return new pointcloud(pc);
            }

            public bool eof()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.source.eof called with NULL pointer");
                return _API_cwipc_util.cwipc_source_eof(pointer);
            }

            public bool available(bool wait)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.source.available called with NULL pointer");
                return _API_cwipc_util.cwipc_source_available(pointer, wait);
            }

            public tileinfo[] get_tileinfo()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.source.get_tileinfo called with NULL pointer");
                int maxTile = _API_cwipc_util.cwipc_tiledsource_maxtile(pointer);
                if (maxTile == 0) return null;
                tileinfo[] rv = new tileinfo[maxTile];
                for (int i = 0; i < maxTile; i++)
                {
                    bool ok = _API_cwipc_util.cwipc_tiledsource_get_tileinfo(pointer, i, out rv[i]);
                }
                return rv;
            }
        }

        public class decoder : source
        {
            internal decoder(IntPtr _obj) : base(_obj)
            {
                if (_obj == IntPtr.Zero) throw new Exception("cwipc.decoder: constructor called with null pointer");
            }

            public void feed(IntPtr compFrame, int len)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.decoder.feed called with NULL pointer");
                _API_cwipc_codec.cwipc_decoder_feed(pointer, compFrame, len);
            }

            public void close()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.decoder.close called with NULL pointer");
                _API_cwipc_codec.cwipc_decoder_close(pointer);
            }

        }


        public class encoder : source
        {
            internal encoder(IntPtr _obj) : base(_obj)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encoder called with NULL pointer argument");
            }

            /* xxxjack need to check how this works with BaseMemoryChunk
                    ~encoder() {
                        free();
                    }
            */
            public void feed(pointcloud pc)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encoder.feed called with NULL pointer argument");
                _API_cwipc_codec.cwipc_encoder_feed(pointer, pc.pointer);
            }

            public void close()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encoder.close called with NULL pointer argument");
                _API_cwipc_codec.cwipc_encoder_close(pointer);
            }

            public new bool eof()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encoder.eof called with NULL pointer argument");
                return _API_cwipc_codec.cwipc_encoder_eof(pointer);
            }

            new public bool available(bool wait)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encoder.available called with NULL pointer argument");
                return _API_cwipc_codec.cwipc_encoder_available(pointer, wait);
            }

            public int get_encoded_size()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encoder.get_encoded_size called with NULL pointer argument");
                return (int)_API_cwipc_codec.cwipc_encoder_get_encoded_size(pointer);
            }

            public bool copy_data(IntPtr data, int size)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encoder.copy_data called with NULL pointer argument");
                return _API_cwipc_codec.cwipc_encoder_copy_data(pointer, data, (IntPtr)size);
            }

            public bool at_gop_boundary()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encoder.at_gop_boundary called with NULL pointer argument");
                return _API_cwipc_codec.cwipc_encoder_at_gop_boundary(pointer);
            }

        }

        public class encodergroup : BaseMemoryChunk
        {
            internal encodergroup(IntPtr _obj) : base(_obj)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encodergroup called with NULL pointer argument");
            }

            /* xxxjack need to check how this works with BaseMemoryChunk
                    ~encodergroup() {
                        free();
                    }
            */
            public void feed(pointcloud pc)
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encodergroup.feed called with NULL pointer argument");
                _API_cwipc_codec.cwipc_encodergroup_feed(pointer, pc.pointer);
            }

            public void close()
            {
                if (pointer == IntPtr.Zero) throw new Exception("cwipc.encodergroup.close called with NULL pointer argument");
                _API_cwipc_codec.cwipc_encodergroup_close(pointer);
            }

            public encoder addencoder(encoder_params par)
            {
                IntPtr errorPtr = IntPtr.Zero;
                IntPtr enc = _API_cwipc_codec.cwipc_encodergroup_addencoder(pointer, _API_cwipc_codec.CWIPC_ENCODER_PARAM_VERSION, ref par, ref errorPtr);
                if (enc == IntPtr.Zero)
                {
                    if (errorPtr == IntPtr.Zero)
                    {
                        throw new Exception("cwipc.encodergroup.addencoder: returned null without setting error message");
                    }
                    throw new Exception($"cwipc_encoder_addencoder: {Marshal.PtrToStringAnsi(errorPtr)} ");
                }
                if (errorPtr != IntPtr.Zero)
                {
                    UnityEngine.Debug.LogError($"cwipc_encoder_addencoder: {Marshal.PtrToStringAnsi(errorPtr)}. Attempting to continue.");
                }
                return new encoder(enc);

            }
        }

        public static source synthetic(int fps = 0, int npoints = 0)
        {
            IntPtr errorPtr = IntPtr.Zero;
            IntPtr rdr = _API_cwipc_util.cwipc_synthetic(fps, npoints, ref errorPtr);
            if (rdr == IntPtr.Zero)
            {
                if (errorPtr == IntPtr.Zero)
                {
                    throw new Exception("cwipc.synthetic: returned null without setting error message");
                }
                throw new Exception($"cwipc.synthetic: {Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            if (errorPtr != IntPtr.Zero)
            {
                UnityEngine.Debug.LogError($"cwipc.synthetic: {Marshal.PtrToStringAnsi(errorPtr)}. Attempting to continue.");
            }
            return new source(rdr);
        }

        public static source proxy(string ip, int port)
        {
            IntPtr errorPtr = IntPtr.Zero;
            IntPtr rdr = _API_cwipc_util.cwipc_proxy(ip, port, ref errorPtr);
            if (rdr == IntPtr.Zero)
            {
                if (errorPtr == IntPtr.Zero)
                {
                    throw new Exception("cwipc.proxy: returned null without setting error message");
                }
                throw new Exception($"cwipc.proxy: {Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            if (errorPtr != IntPtr.Zero)
            {
                UnityEngine.Debug.LogError($"cwipc.proxy: {Marshal.PtrToStringAnsi(errorPtr)}. Attempting to continue.");
            }
            return new source(rdr);
        }

        public static source realsense2(string filename)
        {
            IntPtr errorPtr = IntPtr.Zero;
            IntPtr rdr = _API_cwipc_realsense2.cwipc_realsense2(filename, ref errorPtr);
            if (rdr == IntPtr.Zero)
            {
                if (errorPtr == IntPtr.Zero)
                {
                    throw new Exception("cwipc.realsense2: returned null without setting error message");
                }
                throw new Exception($"cwipc.realsense2: {Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            if (errorPtr != IntPtr.Zero)
            {
                UnityEngine.Debug.LogError($"cwipc.realsense2: {Marshal.PtrToStringAnsi(errorPtr)}. Attempting to continue.");
            }
            return new source(rdr);
        }
        public static source kinect(string filename)
        {
            IntPtr errorPtr = IntPtr.Zero;
            IntPtr rdr = _API_cwipc_kinect.cwipc_kinect(filename, ref errorPtr);
            if (rdr == IntPtr.Zero)
            {
                if (errorPtr == IntPtr.Zero)
                {
                    throw new Exception("cwipc.kinect: returned null without setting error message");
                }
                throw new Exception($"cwipc.kinect: {Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            if (errorPtr != IntPtr.Zero)
            {
                UnityEngine.Debug.LogError($"cwipc.kinect: {Marshal.PtrToStringAnsi(errorPtr)}. Attempting to continue.");
            }
            return new source(rdr);
        }

        public static decoder new_decoder()
        {
            IntPtr errorPtr = IntPtr.Zero;
            IntPtr dec = _API_cwipc_codec.cwipc_new_decoder(ref errorPtr);
            if (dec == IntPtr.Zero)
            {
                if (errorPtr == IntPtr.Zero)
                {
                    throw new Exception("cwipc.new_decoder: returned null without setting error message");
                }
                throw new Exception($"cwipc_new_decoder: {Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            if (errorPtr != IntPtr.Zero)
            {
                UnityEngine.Debug.LogError($"cwipc_new_decoder: {Marshal.PtrToStringAnsi(errorPtr)}. Attempting to continue.");
            }
            return new decoder(dec);

        }

        public static encoder new_encoder(encoder_params par)
        {
            IntPtr errorPtr = IntPtr.Zero;
            IntPtr enc = _API_cwipc_codec.cwipc_new_encoder(_API_cwipc_codec.CWIPC_ENCODER_PARAM_VERSION, ref par, ref errorPtr);
            if (enc == IntPtr.Zero)
            {
                if (errorPtr == IntPtr.Zero)
                {
                    throw new Exception("cwipc.new_encoder: returned null without setting error message");
                }
                throw new Exception($"cwipc_new_encoder: {Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            if (errorPtr != IntPtr.Zero)
            {
                UnityEngine.Debug.LogError($"cwipc_new_encoder: {Marshal.PtrToStringAnsi(errorPtr)}. Attempting to continue.");
            }
            return new encoder(enc);

        }

        public static encodergroup new_encodergroup()
        {
            IntPtr errorPtr = IntPtr.Zero;
            IntPtr enc = _API_cwipc_codec.cwipc_new_encodergroup(ref errorPtr);
            if (enc == IntPtr.Zero)
            {
                if (errorPtr == IntPtr.Zero)
                {
                    throw new Exception("cwipc.new_encodergroup: returned null without setting error message");
                }
                throw new Exception($"cwipc_new_encodergroup: {Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            if (errorPtr != IntPtr.Zero)
            {
                UnityEngine.Debug.LogError($"cwipc_new_encodergroup: {Marshal.PtrToStringAnsi(errorPtr)}. Attempting to continue.");
            }
            return new encodergroup(enc);

        }

        public static pointcloud downsample(pointcloud pc, float voxelSize)
        {
            IntPtr pcPtr = pc._intptr();
            IntPtr rvPtr = _API_cwipc_util.cwipc_downsample(pcPtr, voxelSize);
            if (rvPtr == IntPtr.Zero) return null;
            return new pointcloud(rvPtr);
        }

        public static pointcloud tilefilter(pointcloud pc, int tileNum)
        {
            IntPtr pcPtr = pc._intptr();
            IntPtr rvPtr = _API_cwipc_util.cwipc_tilefilter(pcPtr, tileNum);
            if (rvPtr == IntPtr.Zero) return null;
            return new pointcloud(rvPtr);
        }

        public static pointcloud tilemap(pointcloud pc, byte[] map)
        {
            IntPtr pcPtr = pc._intptr();
            IntPtr rvPtr = _API_cwipc_util.cwipc_tilemap(pcPtr, map);
            if (rvPtr == IntPtr.Zero) return null;
            return new pointcloud(rvPtr);
        }

        public static pointcloud colormap(pointcloud pc, uint clearMask, uint setMask)
        {
            IntPtr pcPtr = pc._intptr();
            IntPtr rvPtr = _API_cwipc_util.cwipc_colormap(pcPtr, clearMask, setMask);
            if (rvPtr == IntPtr.Zero) return null;
            return new pointcloud(rvPtr);
        }

        public static pointcloud crop(pointcloud pc, float[] bbox)
        {
            IntPtr pcPtr = pc._intptr();
            IntPtr rvPtr = _API_cwipc_util.cwipc_crop(pcPtr, bbox);
            if (rvPtr == IntPtr.Zero) return null;
            return new pointcloud(rvPtr);
        }

        public static pointcloud join(pointcloud pc1, pointcloud pc2)
        {
            IntPtr pc1Ptr = pc1._intptr();
            IntPtr pc2Ptr = pc2._intptr();
            IntPtr rvPtr = _API_cwipc_util.cwipc_join(pc1Ptr, pc2Ptr);
            if (rvPtr == IntPtr.Zero) return null;
            return new pointcloud(rvPtr);
        }

        public static pointcloud from_certh(IntPtr certhPC, float[] move, float[] bbox, Timestamp timestamp)
        {
            IntPtr errorPtr = IntPtr.Zero;
            // Need to pass origin and bbox as array pointers.
            IntPtr rvPtr = _API_cwipc_util.cwipc_from_certh(certhPC, move, bbox, timestamp, ref errorPtr);
            if (rvPtr == IntPtr.Zero)
            {
                if (errorPtr == IntPtr.Zero)
                {
                    throw new Exception("cwipc.from_certh: returned null without setting error message");
                }
                throw new Exception($"cwipc_from_certh: {Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            if (errorPtr != IntPtr.Zero)
            {
                UnityEngine.Debug.LogError($"cwipc_from_certh: {Marshal.PtrToStringAnsi(errorPtr)}. Attempting to continue.");
            }
            return new pointcloud(rvPtr);
        }

        public static pointcloud read(string filename, Timestamp timestamp)
        {
            System.IntPtr errorPtr = System.IntPtr.Zero;
            System.IntPtr rvPtr = _API_cwipc_util.cwipc_read(filename, timestamp, ref errorPtr);
            if (rvPtr == System.IntPtr.Zero)
            {
                if (errorPtr == System.IntPtr.Zero)
                {
                    throw new System.Exception("cwipc.read: returned null without setting error message");
                }
                throw new System.Exception($"cwipc_read: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            return new pointcloud(rvPtr);
        }

        public static pointcloud readdump(string filename)
        {
            System.IntPtr errorPtr = System.IntPtr.Zero;
            System.IntPtr rvPtr = _API_cwipc_util.cwipc_read_debugdump(filename, ref errorPtr);
            if (rvPtr == System.IntPtr.Zero)
            {
                if (errorPtr == System.IntPtr.Zero)
                {
                    throw new System.Exception("cwipc.read: returned null without setting error message");
                }
                throw new System.Exception($"cwipc_read: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            return new pointcloud(rvPtr);
        }

        public static pointcloud from_packet(IntPtr packet, IntPtr size)
        {
            System.IntPtr errorPtr = System.IntPtr.Zero;
            System.IntPtr rvPtr = _API_cwipc_util.cwipc_from_packet(packet, size, ref errorPtr);
            if (rvPtr == System.IntPtr.Zero)
            {
                if (errorPtr == System.IntPtr.Zero)
                {
                    throw new System.Exception("cwipc.from_packet: returned null without setting error message");
                }
                throw new System.Exception($"cwipc_from_packet: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            return new pointcloud(rvPtr);
        }

        public static pointcloud from_packet(byte[] packet)
        {
            IntPtr size = (IntPtr)packet.Length;
            pointcloud rv = null;
            unsafe
            {
                fixed(byte *packetPtr = packet)
                {
                    rv = from_packet((IntPtr)packetPtr, size);
                }
            }
            return rv;
        }
    }
}