using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;

namespace VRT.UserRepresentation.PointCloud
{
    public class KinectPointCloudSkeletonReader : Cwipc.KinectPointCloudReader
    {

        public Cwipc.SkeletonSupport mostRecentSkeleton;
        bool wantedSkeleton = false;
        int count = 0;

        public KinectPointCloudSkeletonReader(string _configFilename, float _voxelSize, float _frameRate, bool _wantedSkeleton, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(_outQueue, _out2Queue)
        {
            voxelSize = _voxelSize;
            wantedSkeleton = _wantedSkeleton;
            if (_frameRate > 0)
            {
                frameInterval = System.TimeSpan.FromSeconds(1 / _frameRate);
            }
            try
            {
                reader = cwipc.kinect(_configFilename);
                if (wantedSkeleton)
                {
                    Statistics.Output(Name(), "skeleton=1");
                    bool result = reader.request_auxiliary_data("skeleton");
                    if (!result) throw new System.Exception($"{Name()}: cwipc_kinect skeleton tracker could not be initialized");
                    Debug.Log($"{Name()}: Requested Skeleton.");
                }
                if (reader != null)
                {
                    Start();
                    Debug.Log($"{Name()}: Started.");
                }
                else
                    throw new System.Exception($"{Name()}: cwipc_kinect could not be created"); // Should not happen, should throw exception
            }
            catch (System.DllNotFoundException e)
            {
                throw new System.Exception($"{Name()}: support for Kinect grabber not installed on this computer. Missing DLL {e.Message}.");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: caught System.exception {e.Message}");
                throw;
            }
        }

        override protected void OptionalProcessing(cwipc.pointcloud pc) 
        {
            if (wantedSkeleton)
            {
                count++;
                cwipc.cwipc_auxiliary_data pc_aux_data = pc.access_auxiliary_data();
                int n_auxdata = pc_aux_data.count();
                //Debug.Log($"xxxnacho pc has {n_auxdata} auxdata elements. timestamp={pc.timestamp()}");
                if (n_auxdata > 0)
                {
                    cwipc.cwipc_skeleton new_skeleton = new cwipc.cwipc_skeleton();
                    bool found_skeleton = false;
                    for (int i = 0; i < n_auxdata; i++)
                    {
                        
                        string aux_name = pc_aux_data.name(i);
                        //Debug.Log($"xxxnacho aux_name {count} = {aux_name}");
                        if (aux_name.Contains("skeleton"))
                        {
                            if (!found_skeleton)
                            {
                                new_skeleton = new cwipc.cwipc_skeleton(pc_aux_data.pointer(i), pc_aux_data.size(i), pc.timestamp());
                                if (new_skeleton.joints.Count > 0)
                                {
                                    found_skeleton = true;
                                    Debug.Log($"xxxnacho found_skeleton, {new_skeleton.joints.Count}");
                                }
                            }
                            else
                            {
                                bool ok = new_skeleton.fuse_skeletons(pc_aux_data.pointer(i), pc_aux_data.size(i));
                                Debug.Log($"xxxnacho fused_skeleton = {ok}, {new_skeleton.joints.Count} joints");
                            }
                        }
                    }
                    if (found_skeleton) mostRecentSkeleton = new_skeleton;
                }
            }
        }

        public bool has_skeleton()
        {
            if (wantedSkeleton && mostRecentSkeleton != null)
                return true;
            return false;
        }

        public bool supports_skeleton()
        {
            return wantedSkeleton;
        }
    }
}
