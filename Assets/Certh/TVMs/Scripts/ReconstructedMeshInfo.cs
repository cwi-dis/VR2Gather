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

	public ViewpointInfo(Matrix4x4 colorIntrinsics,
	                     Matrix4x4 global2color)
	{
		this.ColorIntrinsics = colorIntrinsics;
		this.Global2Color = global2color;
	}
	
	internal readonly Matrix4x4 ColorIntrinsics;
	internal readonly Matrix4x4 Global2Color;
}

public struct ReconstructedMeshInfo
{
	internal ViewpointInfo Viewpoint;
    internal long AcquisitionTimestamp { get; set; }
	internal long KinectTimestamp { get; set; }
}
