using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class SharedReconstructionReceiver {

	[StructLayout(LayoutKind.Sequential)]
    public struct Vec4f
    {
        public float x, y, z, w;
    };
    public struct Vec3f
    {
        public float x, y, z;
    };
    public struct Vec3iu
    {
        public int X, Y, Z;
    };
    public struct mVertex
	{
		public float x, y, z;
		//public float nx, ny, nz;
		//public float tid1, tid2, w;
	}

    public struct mNormal
    {
        public float nx, ny, nz;
    }

    public struct mColor
    {
        public float tid1, tid2, w;
    }


    [StructLayout(LayoutKind.Sequential)]
	public struct Texture
	{
		public int width, height;
		public IntPtr data;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct DMesh
	{                        
		public int nTextures;
		public int nTriangles;
		public int nVertices;
		public IntPtr textures;
		public IntPtr vertices;
        public IntPtr normals;
		public IntPtr colors;
        public IntPtr triangles;
        
		public IntPtr intrinsics;
		public IntPtr global2LocalColor;
		public long acquisitionTimestamp;
		public long kinectTimestamp;

        /// kapostol additions
        public IntPtr jointPositions;
        public IntPtr jointOrientations;
        /// end kapostol additions
    }
    
    public delegate void OnConnectionError(uint id); 
	public delegate void OnReceivedMesh(uint id, DMesh dmesh);
	
	[DllImport("VRT.TVM.SelfStream", CharSet = CharSet.Ansi)]
	public static extern int Test(int a, int b);
	
	[DllImport("VRT.TVM.SelfStream", CharSet = CharSet.Ansi)]
	public static extern bool Init();

    [DllImport("VRT.TVM.SelfStream", CharSet = CharSet.Ansi)]
	public static extern void RegisterOnReceivedMeshCallBack(uint id, MulticastDelegate callback);

    [DllImport("VRT.TVM.SelfStream", CharSet = CharSet.Ansi)]
    public static extern void RegisterOnConnectionErrorCallBack(uint id, MulticastDelegate callback);

    [DllImport("VRT.TVM.SelfStream", CharSet = CharSet.Ansi)]
    public static extern void AddClient();

    [DllImport("VRT.TVM.SelfStream", CharSet = CharSet.Ansi)]
    public static extern void StartClient(uint id);

    [DllImport("VRT.TVM.SelfStream", CharSet = CharSet.Ansi)]
    public static extern void StopClient(uint id);

    [DllImport("VRT.TVM.SelfStream", CharSet = CharSet.Ansi)]
    public static extern void ShutDown();
}
