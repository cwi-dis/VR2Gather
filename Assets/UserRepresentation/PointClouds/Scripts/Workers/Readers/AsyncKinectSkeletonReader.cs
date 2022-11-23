using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;

namespace VRT.UserRepresentation.PointCloud
{
    using cwipc_skeleton = Cwipc.SkeletonSupport.cwipc_skeleton;

    public class AsyncKinectSkeletonReader : Cwipc.AsyncKinectReader
    {

        public cwipc_skeleton mostRecentSkeleton;
        bool wantedSkeleton = false;
        int count = 0;

        public AsyncKinectSkeletonReader(string _configFilename, float _voxelSize, float _frameRate, bool _wantedSkeleton, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(_configFilename, _voxelSize, _frameRate, _outQueue, _out2Queue)
        {
            wantedSkeleton = _wantedSkeleton;
            if (wantedSkeleton)
            {
                Statistics.Output(Name(), "skeleton=1");
                bool result = reader.request_auxiliary_data("skeleton");
                if (!result) throw new System.Exception($"{Name()}: cwipc_kinect skeleton tracker could not be initialized");
                Debug.Log($"{Name()}: Requested Skeleton.");
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
                    cwipc_skeleton new_skeleton = new cwipc_skeleton();
                    bool found_skeleton = false;
                    for (int i = 0; i < n_auxdata; i++)
                    {
                        
                        string aux_name = pc_aux_data.name(i);
                        //Debug.Log($"xxxnacho aux_name {count} = {aux_name}");
                        if (aux_name.Contains("skeleton"))
                        {
                            if (!found_skeleton)
                            {
                                new_skeleton = new cwipc_skeleton(pc_aux_data.pointer(i), pc_aux_data.size(i), pc.timestamp());
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
