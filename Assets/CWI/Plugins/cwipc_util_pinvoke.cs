using System;
using System.Runtime.InteropServices;

internal class API_cwipc_util { 
    [DllImport("cwipc_util")]
    internal extern static IntPtr cwipc_read([MarshalAs(UnmanagedType.LPStr)]string filename, System.UInt64 timestamp, ref System.IntPtr errorMessage);
    [DllImport("cwipc_util")]
    internal extern static void cwipc_free(IntPtr pc);
    [DllImport("cwipc_util")]
    internal extern static UInt64 cwipc_timestamp(IntPtr pc);
    [DllImport("cwipc_util")]
    internal extern static System.IntPtr cwipc_get_uncompressed_size(IntPtr pc, uint dataVersion = 0x20190424);
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
    internal extern static IntPtr cwipc_synthetic();

    [DllImport("cwipc_util")]
    internal extern static int cwipc_tiledsource_maxtile(IntPtr src);
    [DllImport("cwipc_util")]
    internal extern static bool cwipc_tiledsource_get_tileinfo(IntPtr src, int tileNum, IntPtr tileinfo, int infoVersion = 0x20190516);

}

internal class API_cwipc_realsense2 {
    [DllImport("cwipc_realsense2")]
    internal extern static IntPtr cwipc_realsense2([MarshalAs(UnmanagedType.LPStr)]string filename, ref System.IntPtr errorMessage);
}

internal class API_cwipc_codec {
    [DllImport("cwipc_codec")]
    internal extern static IntPtr cwipc_new_decoder();

    [DllImport("cwipc_codec")]
    internal extern static void cwipc_decoder_feed(IntPtr dec, IntPtr compFrame, int len);

}

internal class API_kernel {
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr GetModuleHandle(string lpModuleName);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern int GetModuleFileName(IntPtr hModule, System.Text.StringBuilder modulePath, int nSize);
}

