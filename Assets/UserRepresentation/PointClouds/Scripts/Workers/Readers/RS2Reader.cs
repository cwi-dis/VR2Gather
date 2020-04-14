using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class RS2Reader : BaseWorker {
        cwipc.source reader;
        float voxelSize;

        public RS2Reader(Config._User._PCSelfConfig cfg) : base(WorkerType.Init) {
            voxelSize = cfg.voxelSize;
            try {
                reader = cwipc.realsense2(cfg.configFilename);  
                if (reader != null) {
                    Start();
                    Debug.Log("RS2Reader Inited");
                } else
                    throw new System.Exception($"PCRealSense2Reader: cwipc_realsense2 could not be created"); // Should not happen, should throw exception
            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public override void OnStop() {
            base.OnStop();
            if (currentPointCloud != null) currentPointCloud.free();
            currentPointCloud = null;
            reader?.free();
            reader = null;
            Debug.Log("RS2Reader Stopped");
        }

        cwipc.pointcloud currentPointCloud;
        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                lock (token) {
                    if (currentPointCloud != null) currentPointCloud.free();
                    currentPointCloud = reader.get();
                    if (currentPointCloud == null) return;
                    if (voxelSize != 0) {
                        int oldCount = currentPointCloud.count();
                        var tmp = currentPointCloud;
                        currentPointCloud = cwipc.downsample(tmp, voxelSize);
                        tmp.free();
                        if (currentPointCloud == null) {
                            Debug.LogError($"Voxelating pointcloud with {voxelSize} got rid of all points?");
                            return;
                        }
                        //Debug.Log($"xxxjack voxelSize={voxelSize} from {oldCount} to {currentPointCloud.count()} point, cellsize={currentPointCloud.cellsize()}");
                    }
                    statsUpdate(currentPointCloud.count());
                    token.currentPointcloud = currentPointCloud;
                    Next();
                }
            }
        }

        System.DateTime statsLastTime;
        double statsTotalPoints;
        double statsTotalPointclouds;

        public void statsUpdate(int pointCount)
        {
            if (statsLastTime == null)
            {
                statsLastTime = System.DateTime.Now;
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(10))
            {
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: RS2Reader: {statsTotalPointclouds / 10} fps, {(int)(statsTotalPoints / statsTotalPointclouds)} points per cloud");
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalPoints += pointCount;
            statsTotalPointclouds += 1;
        }
    }
}
