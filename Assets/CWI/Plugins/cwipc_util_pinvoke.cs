using System;
using System.Runtime.InteropServices;


internal class API_cwipc_util {
    const System.UInt64 CWIPC_API_VERSION = 0x20190522;

    [DllImport("cwipc_util")]
    internal extern static IntPtr cwipc_read([MarshalAs(UnmanagedType.LPStr)]string filename, System.UInt64 timestamp, ref System.IntPtr errorMessage, System.UInt64 apiVersion=CWIPC_API_VERSION);
    [DllImport("cwipc_util")]
    internal extern static void cwipc_free(IntPtr pc);
    [DllImport("cwipc_util")]
    internal extern static UInt64 cwipc_timestamp(IntPtr pc);
    [DllImport("cwipc_util")]
    internal extern static System.IntPtr cwipc_get_uncompressed_size(IntPtr pc);
    [DllImport("cwipc_util")]
    internal extern static int cwipc_copy_uncompressed(IntPtr pc, IntPtr data, System.IntPtr size);

    [DllImport("cwipc_util")]
    internal extern static System.IntPtr cwipc_source_get(IntPtr src);
    [DllImport("cwipc_util")]
    internal extern static bool cwipc_source_eof(IntPtr src);
    [DllImport("cwipc_util")]
    internal extern static bool cwipc_source_available(IntPtr src, bool available);
    [DllImport("cwipc_util")]
    internal extern static void cwipc_source_free(IntPtr src);

    [DllImport("cwipc_util")]
    internal extern static IntPtr cwipc_synthetic(ref System.IntPtr errorMessage, System.UInt64 apiVersion=CWIPC_API_VERSION);

    [DllImport("cwipc_util")]
    internal extern static int cwipc_tiledsource_maxtile(IntPtr src);
    [DllImport("cwipc_util")]
    internal extern static uint cwipc_tiledsource_get_tileinfo(IntPtr src, int tileNum, IntPtr tileinfo);

}

internal class API_cwipc_realsense2 {
    const System.UInt64 CWIPC_API_VERSION = 0x20190522;
    [DllImport("cwipc_realsense2")]
    internal extern static IntPtr cwipc_realsense2([MarshalAs(UnmanagedType.LPStr)]string filename, ref System.IntPtr errorMessage, System.UInt64 apiVersion=CWIPC_API_VERSION);
}

internal class API_cwipc_codec {

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct cwipc_encoder_params
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

    const System.UInt64 CWIPC_API_VERSION = 0x20190522;
    public const int CWIPC_ENCODER_PARAM_VERSION = 0x20190506;

    [DllImport("cwipc_codec")]
    internal extern static IntPtr cwipc_new_decoder(ref System.IntPtr errorMessage, System.UInt64 apiVersion = CWIPC_API_VERSION);

    [DllImport("cwipc_codec")]
    internal extern static void cwipc_decoder_feed(IntPtr dec, IntPtr compFrame, int len);

    [DllImport("cwipc_codec")]
    internal extern static void cwipc_decoder_free(IntPtr dec);

    [DllImport("cwipc_codec")]
    internal extern static IntPtr cwipc_new_encoder(int paramVersion, ref cwipc_encoder_params encParams, ref System.IntPtr errorMessage, System.UInt64 apiVersion = CWIPC_API_VERSION);

    [DllImport("cwipc_codec")]
    internal extern static void cwipc_encoder_free(IntPtr enc);

    [DllImport("cwipc_codec")]
    internal extern static void cwipc_encoder_feed(IntPtr enc, IntPtr pc);

    [DllImport("cwipc_codec")]
    internal extern static bool cwipc_encoder_available(IntPtr enc, bool wait);

    [DllImport("cwipc_codec")]
    internal extern static System.IntPtr cwipc_encoder_get_encoded_size(IntPtr enc);

    [DllImport("cwipc_codec")]
    internal extern static bool cwipc_encoder_copy_data(IntPtr enc, IntPtr data, System.IntPtr size);

    [DllImport("cwipc_codec")]
    internal extern static bool cwipc_encoder_at_gop_boundary(IntPtr enc);

}
/*
internal class API_bin2dash {
    [DllImport("bin2dash")]
    internal extern static IntPtr vrt_create([MarshalAs(UnmanagedType.LPStr)]string name, Int32 fourcc, [MarshalAs(UnmanagedType.LPStr)]string url, Int32 seg_dur_in_ms=10000, Int32 timeshift_buffer_depth_in_ms=30000);
    
    [DllImport("bin2dash")]
    internal extern static void vrt_destroy(IntPtr b2d);
    
    [DllImport("bin2dash")]
    internal extern static bool vrt_push_buffer(IntPtr b2d, IntPtr ptr, IntPtr size);
    
    [DllImport("bin2dash")]
    internal extern static Int64 vrt_get_media_time(Int32 scale);
    
}
*/
internal class API_kernel {
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern int GetModuleFileName(IntPtr hModule, System.Text.StringBuilder modulePath, int nSize);
}


