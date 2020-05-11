using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class RS2Reader : BaseWorker {
        cwipc.source reader;
        float voxelSize;
        QueueThreadSafe outQueue;
        QueueThreadSafe out2Queue;

        public RS2Reader(string _configFilename, float _voxelSize, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue=null) : base(WorkerType.Init) {
            if (_outQueue == null)
            {
                throw new System.Exception("RS2Reader: outQueue is null");
            }
            outQueue = _outQueue;
            out2Queue = _out2Queue;
            voxelSize = _voxelSize;
            try {
                reader = cwipc.realsense2(_configFilename);  
                if (reader != null) {
                    Start();
                    Debug.Log("RS2Reader: Started.");
                } else
                    throw new System.Exception($"RS2Reader: cwipc_realsense2 could not be created"); // Should not happen, should throw exception
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
            Debug.Log("RS2Reader: Stopped.");
        }

        protected override void Update() {
            base.Update();
            cwipc.pointcloud pc = reader.get();
            if (pc == null) return;
            if (voxelSize != 0) {
                var tmp = pc;
                pc = cwipc.downsample(tmp, voxelSize);
                tmp.free();
                if (pc== null)  throw new System.Exception($"RS2Reader: Voxelating pointcloud with {voxelSize} got rid of all points?");
            }
            statsUpdate(pc.count());

            if (outQueue != null && outQueue.Free())
                outQueue?.Enqueue( pc.AddRef() );

            if (out2Queue != null && out2Queue.Free())
                out2Queue.Enqueue( pc.AddRef() );

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
