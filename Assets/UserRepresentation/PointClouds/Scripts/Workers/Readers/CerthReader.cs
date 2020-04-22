using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class CerthReader : BaseWorker {
        cwipc.source reader;
        float voxelSize;
        QueueThreadSafe outQueue;
        QueueThreadSafe out2Queue;

        public CerthReader(Config._User._PCSelfConfig cfg, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue=null) : base(WorkerType.Init) {
            outQueue = _outQueue;
            out2Queue = _out2Queue;
            voxelSize = cfg.voxelSize;
            try {
                reader = cwipc.realsense2(cfg.CerthReaderConfig.configFilename);  
                if (reader != null) {
                    Start();
                    Debug.Log("PCCerthReader: Started.");
                } else
                    throw new System.Exception($"PCCerthReader: cwipc_realsense2 could not be created"); // Should not happen, should throw exception
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
            Debug.Log("PCCerthReader: Stopped.");
        }

        protected override void Update() {
            base.Update();
            cwipc.pointcloud pc = reader.get();
            if (pc == null) return;
            if (voxelSize != 0) {
                var tmp = pc;
                pc = cwipc.downsample(tmp, voxelSize);
                tmp.free();
                if (pc== null)  throw new System.Exception($"PCCerthReader: Voxelating pointcloud with {voxelSize} got rid of all points?");
            }
            pc.AddRef(); pc.AddRef();
            statsUpdate(pc.count());

            if (outQueue != null && outQueue.Count < 2)
                outQueue?.Enqueue(pc);
            else
                pc.free();

            if (out2Queue != null && out2Queue.Count < 2)
                out2Queue.Enqueue(pc);
            else
                pc.free();
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
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: CerthReader: {statsTotalPointclouds / 10} fps, {(int)(statsTotalPoints / statsTotalPointclouds)} points per cloud");
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalPoints += pointCount;
            statsTotalPointclouds += 1;
        }
    }
}
