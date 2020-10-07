using System;
using System.Runtime.InteropServices;

internal class API_kernel
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern int GetModuleFileName(IntPtr hModule, System.Text.StringBuilder modulePath, int nSize);
}

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
        public System.Byte r;
        public System.Byte g;
        public System.Byte b;
        public System.Byte tile;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct tileinfo
    {
        public vector normal;
        public IntPtr camera;
        public System.Byte ncamera;
    };

    private class _API_cwipc_util
    {
        const string myDllName = "cwipc_util";
        public const System.UInt64 CWIPC_API_VERSION = 0x20200703;

        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_read([MarshalAs(UnmanagedType.LPStr)]string filename, System.UInt64 timestamp, ref System.IntPtr errorMessage, System.UInt64 apiVersion = CWIPC_API_VERSION);
        [DllImport(myDllName)]
        internal extern static void cwipc_free(IntPtr pc);
        [DllImport(myDllName)]
        internal extern static UInt64 cwipc_timestamp(IntPtr pc);
        [DllImport(myDllName)]
        internal extern static int cwipc_count(IntPtr pc);
        [DllImport(myDllName)]
        internal extern static float cwipc_cellsize(IntPtr pc);
        [DllImport(myDllName)]
        internal extern static System.IntPtr cwipc_get_uncompressed_size(IntPtr pc);
        [DllImport(myDllName)]
        internal extern static int cwipc_copy_uncompressed(IntPtr pc, IntPtr data, System.IntPtr size);

        [DllImport(myDllName)]
        internal extern static System.IntPtr cwipc_source_get(IntPtr src);
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
        internal extern static int cwipc_sink_interact(IntPtr sink, [MarshalAs(UnmanagedType.LPStr)]string prompt, [MarshalAs(UnmanagedType.LPStr)]string responses, System.Int32 millis);

        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_synthetic(int fps, int npoints, ref System.IntPtr errorMessage, System.UInt64 apiVersion = CWIPC_API_VERSION);

        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_from_certh(IntPtr certhPC, float[] origin, float[] bbox, UInt64 timestamp, ref System.IntPtr errorMessage, System.UInt64 apiVersion = CWIPC_API_VERSION);
        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_downsample(IntPtr pc, float voxelSize);

        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_tilefilter(IntPtr pc, int tilenum);

    }
    private class _API_cwipc_realsense2
    {
        const string myDllName = "cwipc_realsense2";

        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_realsense2([MarshalAs(UnmanagedType.LPStr)]string filename, ref System.IntPtr errorMessage, System.UInt64 apiVersion = _API_cwipc_util.CWIPC_API_VERSION);
    }
    private class _API_cwipc_kinect
    {
        const string myDllName = "cwipc_kinect";

        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_kinect([MarshalAs(UnmanagedType.LPStr)]string filename, ref System.IntPtr errorMessage, System.UInt64 apiVersion = _API_cwipc_util.CWIPC_API_VERSION);
    }
    private class _API_cwipc_codec
    {
        const string myDllName = "cwipc_codec";
        public const int CWIPC_ENCODER_PARAM_VERSION = 0x20190506;

        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_new_decoder(ref System.IntPtr errorMessage, System.UInt64 apiVersion = _API_cwipc_util.CWIPC_API_VERSION);

        [DllImport(myDllName)]
        internal extern static void cwipc_decoder_feed(IntPtr dec, IntPtr compFrame, int len);

        [DllImport(myDllName)]
        internal extern static void cwipc_decoder_close(IntPtr dec);

        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_new_encoder(int paramVersion, ref encoder_params encParams, ref System.IntPtr errorMessage, System.UInt64 apiVersion = _API_cwipc_util.CWIPC_API_VERSION);

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
        internal extern static System.IntPtr cwipc_encoder_get_encoded_size(IntPtr enc);

        [DllImport(myDllName)]
        internal extern static bool cwipc_encoder_copy_data(IntPtr enc, IntPtr data, System.IntPtr size);

        [DllImport(myDllName)]
        internal extern static bool cwipc_encoder_at_gop_boundary(IntPtr enc);

        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_new_encodergroup(ref System.IntPtr errorMessage, System.UInt64 apiVersion = _API_cwipc_util.CWIPC_API_VERSION);

        [DllImport(myDllName)]
        internal extern static void cwipc_encodergroup_free(IntPtr enc);

        [DllImport(myDllName)]
        internal extern static void cwipc_encodergroup_close(IntPtr enc);

        [DllImport(myDllName)]
        internal extern static IntPtr cwipc_encodergroup_addencoder(IntPtr enc, int paramVersion, ref encoder_params encParams, ref System.IntPtr errorMessage);

        [DllImport(myDllName)]
        internal extern static void cwipc_encodergroup_feed(IntPtr enc, IntPtr pc);

    }

    public class pointcloud : BaseMemoryChunk {
        internal pointcloud(System.IntPtr _pointer): base(_pointer) {
            if (_pointer == System.IntPtr.Zero)
                throw new System.Exception("cwipc.pointcloud called with NULL pointer argument");
        }

        ~pointcloud() {
            free();
        }
        
        protected override void onfree() {
            if( pointer == IntPtr.Zero ) throw new System.Exception("cwipc.pointcloud.onfree called with NULL pointer");
            _API_cwipc_util.cwipc_free(pointer);
        }

        public UInt64 timestamp()         {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.pointcloud.timestamp called with NULL pointer");
            return _API_cwipc_util.cwipc_timestamp(pointer);
        }

        public int count() {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.pointcloud.count called with NULL pointer");
            return (int)_API_cwipc_util.cwipc_count(pointer);
            
        }

        public float cellsize() {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.pointcloud.cellsize called with NULL pointer");
            return _API_cwipc_util.cwipc_cellsize(pointer);
        }

        public int get_uncompressed_size() {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.pointcloud.get_uncompressed_size called with NULL pointer");
            return (int)_API_cwipc_util.cwipc_get_uncompressed_size(pointer);
        }

        public int copy_uncompressed(System.IntPtr data, int size) {
            if( pointer == IntPtr.Zero) throw new System.Exception("cwipc.pointcloud.copy_uncompressed called with NULL pointer");
            return _API_cwipc_util.cwipc_copy_uncompressed(pointer, data, (System.IntPtr)size);
        }

        internal System.IntPtr _intptr() {
            return pointer;
        }
    }

    public class source : BaseMemoryChunk {
        internal source(System.IntPtr _pointer) : base(_pointer) {
            if (_pointer == System.IntPtr.Zero) throw new System.Exception("cwipc.source called with NULL pointer argument");
        }

        protected override void onfree() {
            if (pointer == IntPtr.Zero)  throw new System.Exception("cwipc.source.onfree called with NULL pointer");
            _API_cwipc_util.cwipc_source_free(pointer);
        }

        /* xxxjack need to check how this works with BaseMemoryChunk
        ~source() {
            free();
        }
        */
        public pointcloud get() {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.source.get called with NULL pointer");
            IntPtr pc = _API_cwipc_util.cwipc_source_get(pointer);
            if (pc == System.IntPtr.Zero) return null;
            return new pointcloud(pc);
        }

        public bool eof() {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.source.eof called with NULL pointer");
            return _API_cwipc_util.cwipc_source_eof(pointer);
        }

        public bool available(bool wait) {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.source.available called with NULL pointer");
            return _API_cwipc_util.cwipc_source_available(pointer, wait);
        }

        public tileinfo[] get_tileinfo()
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.source.get_tileinfo called with NULL pointer");
            int maxTile = _API_cwipc_util.cwipc_tiledsource_maxtile(pointer);
            if (maxTile == 0) return null;
            tileinfo[] rv = new tileinfo[maxTile];
            for (int i=0; i<maxTile; i++)
            {
                bool ok = _API_cwipc_util.cwipc_tiledsource_get_tileinfo(pointer, i, out rv[i]);
            }
            return rv;
        }
    }

    public class decoder : source {
        internal decoder(System.IntPtr _obj) : base(_obj) {
            if (_obj == System.IntPtr.Zero)  throw new System.Exception("cwipc.decoder: constructor called with null pointer");
        }

        public void feed(IntPtr compFrame, int len)
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.decoder.feed called with NULL pointer");
            _API_cwipc_codec.cwipc_decoder_feed(pointer, compFrame, len);
        }

        public void close()
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.decoder.close called with NULL pointer");
            _API_cwipc_codec.cwipc_decoder_close(pointer);
        }

    }


    public class encoder : source
    {
        internal encoder(System.IntPtr _obj) : base(_obj)
        {
            if (pointer == System.IntPtr.Zero) throw new System.Exception("cwipc.encoder called with NULL pointer argument");
        }

        /* xxxjack need to check how this works with BaseMemoryChunk
                ~encoder() {
                    free();
                }
        */
        public void feed(pointcloud pc)
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.encoder.feed called with NULL pointer argument");
            _API_cwipc_codec.cwipc_encoder_feed(pointer, pc.pointer);
        }

        public void close()
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.encoder.close called with NULL pointer argument");
            _API_cwipc_codec.cwipc_encoder_close(pointer);
        }

        public new bool eof()
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.encoder.eof called with NULL pointer argument");
            return _API_cwipc_codec.cwipc_encoder_eof(pointer);
        }

        new public bool available(bool wait)
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.encoder.available called with NULL pointer argument");
            return _API_cwipc_codec.cwipc_encoder_available(pointer, wait);
        }

        public int get_encoded_size()
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.encoder.get_encoded_size called with NULL pointer argument");
            return (int)_API_cwipc_codec.cwipc_encoder_get_encoded_size(pointer);
        }

        public bool copy_data(System.IntPtr data, int size)
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.encoder.copy_data called with NULL pointer argument");
            return _API_cwipc_codec.cwipc_encoder_copy_data(pointer, data, (System.IntPtr)size);
        }

        public bool at_gop_boundary()
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.encoder.at_gop_boundary called with NULL pointer argument");
            return _API_cwipc_codec.cwipc_encoder_at_gop_boundary(pointer);
        }

    }

    public class encodergroup : BaseMemoryChunk
    {
        internal encodergroup(System.IntPtr _obj) : base(_obj)
        {
            if (pointer == System.IntPtr.Zero) throw new System.Exception("cwipc.encodergroup called with NULL pointer argument");
        }

        /* xxxjack need to check how this works with BaseMemoryChunk
                ~encodergroup() {
                    free();
                }
        */
        public void feed(pointcloud pc)
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.encodergroup.feed called with NULL pointer argument");
            _API_cwipc_codec.cwipc_encodergroup_feed(pointer, pc.pointer);
        }

        public void close()
        {
            if (pointer == IntPtr.Zero) throw new System.Exception("cwipc.encodergroup.close called with NULL pointer argument");
            _API_cwipc_codec.cwipc_encodergroup_close(pointer);
        }

        public encoder addencoder(encoder_params par)
        {
            System.IntPtr errorPtr = System.IntPtr.Zero;
            System.IntPtr enc = _API_cwipc_codec.cwipc_encodergroup_addencoder(pointer, _API_cwipc_codec.CWIPC_ENCODER_PARAM_VERSION, ref par, ref errorPtr);
            if (enc == System.IntPtr.Zero)
            {
                if (errorPtr == System.IntPtr.Zero)
                {
                    throw new System.Exception("cwipc.encodergroup.addencoder: returned null without setting error message");
                }
                throw new System.Exception($"cwipc_encoder_addencoder: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
            }
            return new encoder(enc);

        }
    }

    public static source synthetic(int fps=0, int npoints=0) {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        System.IntPtr rdr = _API_cwipc_util.cwipc_synthetic(fps, npoints, ref errorPtr);
        if (rdr == System.IntPtr.Zero) {
            if (errorPtr == System.IntPtr.Zero) {
                throw new System.Exception("cwipc.synthetic: returned null without setting error message");
            }
            throw new System.Exception($"cwipc.synthetic: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
        }
        return new source(rdr);
    }

    public static source realsense2(string filename)
    {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        System.IntPtr rdr = _API_cwipc_realsense2.cwipc_realsense2(filename, ref errorPtr);
        if (rdr == System.IntPtr.Zero)
        {
            if (errorPtr == System.IntPtr.Zero)
            {
                throw new System.Exception("cwipc.realsense2: returned null without setting error message");
            }
            throw new System.Exception($"cwipc.realsense2: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
        }
        return new source(rdr);
    }
    public static source kinect(string filename)
    {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        System.IntPtr rdr = _API_cwipc_kinect.cwipc_kinect(filename, ref errorPtr);
        if (rdr == System.IntPtr.Zero)
        {
            if (errorPtr == System.IntPtr.Zero)
            {
                throw new System.Exception("cwipc.kinect: returned null without setting error message");
            }
            throw new System.Exception($"cwipc.realsense2: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
        }
        return new source(rdr);
    }

    public static decoder new_decoder() {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        System.IntPtr dec = _API_cwipc_codec.cwipc_new_decoder(ref errorPtr);
        if (dec == System.IntPtr.Zero) {
            if (errorPtr == System.IntPtr.Zero) {
                throw new System.Exception("cwipc.new_decoder: returned null without setting error message");
            }
            throw new System.Exception($"cwipc_new_decoder: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
        }
        return new decoder(dec);

    }

    public static encoder new_encoder(encoder_params par)
    {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        System.IntPtr enc = _API_cwipc_codec.cwipc_new_encoder(_API_cwipc_codec.CWIPC_ENCODER_PARAM_VERSION, ref par, ref errorPtr);
        if (enc == System.IntPtr.Zero)
        {
            if (errorPtr == System.IntPtr.Zero)
            {
                throw new System.Exception("cwipc.new_encoder: returned null without setting error message");
            }
            throw new System.Exception($"cwipc_new_encoder: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
        }
        return new encoder(enc);

    }

    public static encodergroup new_encodergroup()
    {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        System.IntPtr enc = _API_cwipc_codec.cwipc_new_encodergroup(ref errorPtr);
        if (enc == System.IntPtr.Zero)
        {
            if (errorPtr == System.IntPtr.Zero)
            {
                throw new System.Exception("cwipc.new_encodergroup: returned null without setting error message");
            }
            throw new System.Exception($"cwipc_new_encodergroup: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
        }
        return new encodergroup(enc);

    }

    public static pointcloud downsample(pointcloud pc, float voxelSize) {
        System.IntPtr pcPtr = pc._intptr();
        System.IntPtr rvPtr = _API_cwipc_util.cwipc_downsample(pcPtr, voxelSize);
        if (rvPtr == System.IntPtr.Zero) return null;
        return new pointcloud(rvPtr);
    }

    public static pointcloud tilefilter(pointcloud pc, int tileNum) {
        System.IntPtr pcPtr = pc._intptr();
        System.IntPtr rvPtr = _API_cwipc_util.cwipc_tilefilter(pcPtr, tileNum);
        if (rvPtr == System.IntPtr.Zero) return null;
        return new pointcloud(rvPtr);
    }

    public static pointcloud from_certh(IntPtr certhPC, float[] move, float[] bbox, UInt64 timestamp)
    {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        // Need to pass origin and bbox as array pointers.
        System.IntPtr rvPtr = _API_cwipc_util.cwipc_from_certh(certhPC, move, bbox, timestamp, ref errorPtr);
        if (rvPtr == System.IntPtr.Zero)
        {
            if (errorPtr == System.IntPtr.Zero)
            {
                throw new System.Exception("cwipc.from_certh: returned null without setting error message");
            }
            throw new System.Exception($"cwipc_from_certh: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)} ");
        }
        return new pointcloud(rvPtr);
    }
}
