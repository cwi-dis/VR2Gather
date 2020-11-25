using System;
using System.IO;
using System.Runtime.InteropServices;

namespace VRTCore
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

}