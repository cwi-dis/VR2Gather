using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using VRT.Core;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class SkeletonSupport
    {
     
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _cwipc_skeleton_joint
        {
            public int confidence;
            public float x;
            public float y;
            public float z;
            public float q_w;
            public float q_x;
            public float q_y;
            public float q_z;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _cwipc_skeleton_collection
        {
            public int n_skeletons;
            public int n_joints;
            public _cwipc_skeleton_joint[] joints;
        };

        public class cwipc_skeleton_joint
        {
            public int confidence;      // 0=None, 1=Low, 2=Medium, 3=High
            public float[] position;    // x, y, z
            public float[] orientation; // q_w, q_x, q_y, q_z
            public cwipc_skeleton_joint(int _confidence, float _x, float _y, float _z, float _q_w, float _q_x, float _q_y, float _q_z) 
            {
                confidence = _confidence;
                position = new float[] { _x, _y, _z };
                orientation = new float[] { _q_w, _q_x, _q_y, _q_z };
            }
        }

        public class cwipc_skeleton
        {
            public enum JointIndex
            {
                PELVIS = 0,
                SPINE_NAVEL,
                SPINE_CHEST,
                NECK,
                CLAVICLE_LEFT,
                SHOULDER_LEFT,
                ELBOW_LEFT,
                WRIST_LEFT,
                HAND_LEFT,
                HANDTIP_LEFT,
                THUMB_LEFT,
                CLAVICLE_RIGHT,
                SHOULDER_RIGHT,
                ELBOW_RIGHT,
                WRIST_RIGHT,
                HAND_RIGHT,
                HANDTIP_RIGHT,
                THUMB_RIGHT,
                HIP_LEFT,
                KNEE_LEFT,
                ANKLE_LEFT,
                FOOT_LEFT,
                HIP_RIGHT,
                KNEE_RIGHT,
                ANKLE_RIGHT,
                FOOT_RIGHT,
                HEAD,
                NOSE,
                EYE_LEFT,
                EAR_LEFT,
                EYE_RIGHT,
                EAR_RIGHT
            };

            public Timestamp timestamp;
            public List<cwipc_skeleton_joint> joints;
            public cwipc_skeleton() { }
            public cwipc_skeleton(IntPtr data_pointer, int data_size, Timestamp _timestamp)
            {
                timestamp = _timestamp;
                joints = new List<cwipc_skeleton_joint>();
                //int bytesize = (sizeof(int) + sizeof(float) * 7) * 32;
                byte[] data = new byte[data_size];
                Marshal.Copy(data_pointer, data, 0, data_size);
                var reader = new BinaryReader(new MemoryStream(data), System.Text.Encoding.ASCII);
                //_cwipc_skeleton_collection data = (_cwipc_skeleton_collection)Marshal.PtrToStructure(data_pointer, typeof(_cwipc_skeleton_collection));
                int n_skeletons = reader.ReadInt32();
                if (n_skeletons > 0)
                {
                    int n_joints = reader.ReadInt32();
                    for (int i = 0; i < n_joints; i++)
                    {
                        joints.Add(new cwipc_skeleton_joint(reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                    }
                }
            }

            public bool fuse_skeletons(IntPtr data_pointer, int data_size) 
            {
                List<cwipc_skeleton_joint> new_joints = new List<cwipc_skeleton_joint>();
                //int bytesize = (sizeof(int) + sizeof(float) * 7) * 32;
                byte[] data = new byte[data_size];
                Marshal.Copy(data_pointer, data, 0, data_size);
                var reader = new BinaryReader(new MemoryStream(data), System.Text.Encoding.ASCII);
                //_cwipc_skeleton_collection data = (_cwipc_skeleton_collection)Marshal.PtrToStructure(data_pointer, typeof(_cwipc_skeleton_collection));
                int n_skeletons = reader.ReadInt32();
                if (n_skeletons > 0)
                {
                    int n_joints = reader.ReadInt32();
                    if (n_joints == joints.Count)
                    {
                        for (int i = 0; i < n_joints; i++)
                        {
                            cwipc_skeleton_joint new_joint = new cwipc_skeleton_joint(reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            if (joints[i].confidence == new_joint.confidence)  //average positions
                            {
                                float x = (joints[i].position[0] + new_joint.position[0]) / 2;
                                float y = (joints[i].position[0] + new_joint.position[0]) / 2;
                                float z = (joints[i].position[0] + new_joint.position[0]) / 2;
                                joints[i].position = new float[] { x, y, z };
                            }
                            else if (joints[i].confidence < new_joint.confidence) //Use joint with higher coinfidence
                            {
                                joints[i] = new_joint; 
                            } 
                        }
                        return true;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Error :different number of joints {n_joints}!={joints.Count}");
                    }
                }
                return false;
            }
        }

    }
}