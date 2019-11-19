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
                lock (token) {
                    cwipc.pointcloud pc = reader.get();
                    if (pc == null) return;
                    if (voxelSize != 0) {
                        int oldCount = pc.count();
                        pc = cwipc.downsample(pc, voxelSize);
                        if (pc == null) {
                            Debug.LogError($"Voxelating pointcloud with {voxelSize} got rid of all points?");
                            return;
                        }
                        Debug.Log($"xxxjack voxelSize={voxelSize} from {oldCount} to {pc.count()} point, cellsize={pc.cellsize()}");
                    }
                    Debug.Log($"xxxjack voxelSize={voxelSize} to {pc.count()} point, cellsize={pc.cellsize()} uncompressed_size={pc.get_uncompressed_size()}");
                    token.currentPointcloud = pc;
                    Next();
                }
            }
        }
    }
}
