using System;
using System.Runtime.InteropServices;

namespace VRT.UserRepresentation.PointCloud
{
    public static class native_pointcloud_receiver_pinvoke
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct PointCloud
        {
            public int numDevices;
            public IntPtr vertexPtr;
            public IntPtr normalPtr;
            public IntPtr colorPtr;
            public IntPtr deviceNames;
            public IntPtr verticesPerCamera;
            public IntPtr vertexChannels;
            public IntPtr normalChannels;
            public IntPtr colorChannels;
            private IntPtr pclData;
        };

        [DllImport("native_pcloud_receiver", CharSet = CharSet.Ansi)]
        public static extern IntPtr callColorizedPCloudFrameDLL(IntPtr PCloudBuffer, int size, int index);

        [DllImport("native_pcloud_receiver", CharSet = CharSet.Ansi)]
        public static extern void set_number_wrappers(int numPClouds);

        [DllImport("native_pcloud_receiver", CharSet = CharSet.Ansi)]
        public static extern bool received_metadata(IntPtr MetaDataBuffer, int size, int index);
    }
}