using UnityEngine;
using System;
using System.Collections;

[Flags]
public enum VertexInfo : int
{
	None = 0,
	Position = 1,
	Normal = 2,
	Color = 4,
	UV = 8,		
}

public struct MeshFrameHeader
{
	public MeshFrameHeader(int frame,long timestamp)
	{
		this.FrameID = frame;
		this.Timestamp = timestamp;
	}
	internal readonly int FrameID;
	internal readonly long Timestamp;
	internal static readonly long SizeOf = sizeof(int) + sizeof(long);
}

public struct ViewpointInfo
{
	public ViewpointInfo(Matrix4x4 extrinsics,Matrix4x4 colorIntrinsics,
	                     Matrix4x4 depth2color, Matrix4x4 global2color)
	{
		this.Extrinsics = extrinsics;
		this.ColorIntrinsics = colorIntrinsics;
		this.Depth2Color = depth2color;
		this.Global2Color = global2color;
	}

	public ViewpointInfo(Matrix4x4 colorIntrinsics,
	                     Matrix4x4 global2color)
	{
		this.ColorIntrinsics = colorIntrinsics;
		this.Global2Color = global2color;
		this.Extrinsics = Matrix4x4.identity;
		this.Depth2Color = Matrix4x4.identity;
	}
	
	internal readonly Matrix4x4 Extrinsics;
	internal readonly Matrix4x4 ColorIntrinsics;
	internal readonly Matrix4x4 Depth2Color;
	internal readonly Matrix4x4 Global2Color;
}

public struct ReconstructedMeshInfo
{
	internal VertexInfo VertexInfo { get; set; }
	internal ViewpointInfo[] Viewpoints;
    internal long AcquisitionTimestamp { get; set; }
	internal long KinectTimestamp { get; set; }
}
