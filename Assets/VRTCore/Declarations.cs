using System;
using System.IO;
using System.Runtime.InteropServices;

namespace VRT.Core
{

    // This structure should really be declared in the sub package, but that creates a circular reference.
    // And because the format is shared with the C++ native code we cannot turn it into an object (Jack thinks).
    //
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FrameInfo
    {
        // presentation timestamp, in milliseconds units.
        public long timestamp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] dsi;
        public int dsi_size;
    }


    public class API_kernel
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetModuleFileName(IntPtr hModule, System.Text.StringBuilder modulePath, int nSize);
    }
}