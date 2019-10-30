using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class RS2Reader : BaseWorker
    {
        cwipc.source reader;

        public RS2Reader(Config._User._PCSelfConfig cfg) : base(WorkerType.Init) {
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
                if (pc != null)
                {
                    token.currentPointcloud = pc;
                    Next();
                }
            }
        }
    }
}
