using System;
using System.Runtime.InteropServices;

namespace VRT.UserRepresentation.TVM
{
    public static class DllFunctions
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Mesh
        {
            public int numDevices;
            public int width;
            public int height;
            public int numVertices;
            public int numFaces;
            public IntPtr timestamp;
            public IntPtr id1;
            public IntPtr id2;
            public IntPtr weights;
            public IntPtr vertices;
            public IntPtr faces;
            public IntPtr colorExts;
            public IntPtr colorInts;
            public IntPtr textures;
            public IntPtr radialDistortionCoeffs;
            public IntPtr tangentialDistortionCoeffs;
            private IntPtr meshData;
        };

        [DllImport("textured_TVMesh_receiver", CharSet = CharSet.Ansi)]//, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr callTVMFrameDLL(IntPtr buffer, int size, int index);

        [DllImport("textured_TVMesh_receiver", CharSet = CharSet.Ansi)]//, CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_number_TVMS(int numTVMS);

    }
}