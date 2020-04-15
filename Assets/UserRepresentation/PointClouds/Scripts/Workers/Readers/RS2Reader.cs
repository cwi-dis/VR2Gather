using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class RS2Reader : BaseWorker {
        cwipc.source reader;
        float voxelSize;
        QueueThreadSafe preparerQueue;
        QueueThreadSafe encoderQueue;

        public RS2Reader(Config._User._PCSelfConfig cfg, QueueThreadSafe _preparerQueue, QueueThreadSafe _encoderQueue=null) : base(WorkerType.Init) {
            preparerQueue = _preparerQueue;
            encoderQueue = _encoderQueue;
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
            reader?.free();
            reader = null;
            Debug.Log("RS2Reader Stopped");
        }

        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                lock (token) {
                    cwipc.pointcloud pc = reader.get();
                    if (pc == null) return;
                    if (voxelSize != 0) {
                        var tmp = pc;
                        pc = cwipc.downsample(tmp, voxelSize);
                        tmp.free();
                        if (pc== null)  throw new System.Exception($"Voxelating pointcloud with {voxelSize} got rid of all points?");
                    }
                    statsUpdate(pc.count());
                    preparerQueue?.Enqueue( pc.AddRef() );
                    if (encoderQueue!=null && encoderQueue.Count<2)  encoderQueue.Enqueue(pc.AddRef());
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
