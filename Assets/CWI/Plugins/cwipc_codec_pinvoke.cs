using System;
using System.Runtime.InteropServices;
using UnityEngine;


public class cwipc_codec_pinvoke
{
    [DllImport("cwipc_codec")]
    public extern static IntPtr cwipc_decompress(IntPtr compFrame, int len);
}
