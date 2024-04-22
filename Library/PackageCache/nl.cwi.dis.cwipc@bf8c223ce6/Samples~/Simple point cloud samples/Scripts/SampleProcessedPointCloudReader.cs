using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;

/// <summary>
/// Example to show hot to use cwipc.pointcloud.get_points() and cwipc.from_points().
/// This is a reader that will read synthetic point clouds, and for every point cloud every point
/// is processed: it is projected onto a 1m radius ball centered around the point cloud centroid.
/// 
/// </summary>
public class SampleProcessedPointCloudReader : SyntheticPointCloudReader
{
  protected override cwipc.pointcloud filter(cwipc.pointcloud pc)
    {
        var points = pc.get_points();
        Vector3 centroid = new Vector3();
        foreach (var p in points)
        {
            centroid += p.point;
        }
        centroid /= points.Length;
        for(int i=0; i<points.Length; i++)
        {
            points[i].point = centroid + (points[i].point - centroid).normalized;
        }
        cwipc.pointcloud rv = cwipc.from_points(points, pc.timestamp());
        rv._set_cellsize(pc.cellsize());
        pc.free();
        return rv;
    }
}
