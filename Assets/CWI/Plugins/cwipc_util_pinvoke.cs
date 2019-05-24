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
    const System.UInt64 CWIPC_API_VERSION = 0x20190522;
    public const int CWIPC_ENCODER_PARAM_VERION = 0x20190506;

    [DllImport("cwipc_codec")]
    internal extern static IntPtr cwipc_new_decoder(ref System.IntPtr errorMessage, System.UInt64 apiVersion = CWIPC_API_VERSION);

    [DllImport("cwipc_codec")]
    internal extern static void cwipc_decoder_feed(IntPtr dec, IntPtr compFrame, int len);

    [DllImport("cwipc_codec")]
    internal extern static void cwipc_decoder_free(IntPtr dec);

    [DllImport("cwipc_codec")]
    internal extern static IntPtr cwipc_new_encoder(int paramVersion, IntPtr encParams, ref System.IntPtr errorMessage, System.UInt64 apiVersion = CWIPC_API_VERSION);

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

internal class API_kernel {
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern int GetModuleFileName(IntPtr hModule, System.Text.StringBuilder modulePath, int nSize);
}

