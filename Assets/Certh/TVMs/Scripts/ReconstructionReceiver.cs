using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class ReconstructionReceiver {

	[StructLayout(LayoutKind.Sequential)]
	public struct Vertex
	{
		public float x, y, z;
		public float nx, ny, nz;
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
		public IntPtr indices;
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
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern int Test(int a, int b);
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern bool Init();
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern int AddClient();
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern bool StartClient(uint id);	

	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern bool StopClient(uint id);	
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern bool Shutdown();
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern string GetConnectionURI(uint id);
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern string GetExchangeName(uint id);
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern bool SetConnectionURI(uint id,string ConnectionURI);
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern bool SetExchangeName(uint id,string ExchangeName);
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern void RegisterOnConnectionErrorCallBack(uint id,MulticastDelegate callback);
	
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern void RegisterOnReceivedMeshCallBack(uint id, MulticastDelegate callback);
	[DllImport("ReconstructionReceiver", CharSet = CharSet.Ansi)]
	public static extern float GetReconstructionReceiveFrameRate(uint id);
}
