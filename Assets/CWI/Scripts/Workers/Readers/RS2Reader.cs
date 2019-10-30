using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class RS2Reader : BaseWorker
    {
        cwipc.source reader;
        float voxelSize;

        public RS2Reader(Config._User._PCSelfConfig cfg) : base(WorkerType.Init) {
            voxelSize = cfg.voxelSize;
            try {
                reader = cwipc.realsense2(cfg.configFilename);  
                if (reader != null)
                    Start();
                else
                    throw new System.Exception($"PCRealSense2Reader: cwipc_realsense2 could not be created"); // Should not happen, should throw exception
            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public override void OnStop() {
            base.OnStop();
            reader = null;
        }

        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                cwipc.pointcloud pc = reader.get();
                Debug.Log($"xxxjack grabbed pointcloud of {pc.count()} points");
                if (pc != null && voxelSize != 0)
                {
                    pc = cwipc.downsample(pc, voxelSize);
                    if (pc == null)
                    {
                        Debug.LogError($"Voxelating pointcloud with {voxelSize} got rid of all points?");
                    }
                    else
                    {
                        Debug.Log($"xxxjack voxelated with {voxelSize} gives {pc.count()} points");
                    }
                }
                if (pc != null)
                {
                    token.currentPointcloud = pc;
                    Next();
                }
            }
        }
    }
}
