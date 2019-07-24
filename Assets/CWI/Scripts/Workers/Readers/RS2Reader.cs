using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class RS2Reader : BaseWorker
    {
        protected System.IntPtr reader;
        System.IntPtr currentBuffer;

        public RS2Reader(Config._User._PCSelfConfig cfg) : base(WorkerType.Init) {
            try {
                System.IntPtr errorPtr = System.IntPtr.Zero;
                reader = API_cwipc_realsense2.cwipc_realsense2(cfg.configFilename, ref errorPtr);
                if (reader != System.IntPtr.Zero)
                    Start();
                else
                    throw new System.Exception($"PCRealSense2Reader: cwipc_realsense2: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)}");
            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public override void OnStop() {
            base.OnStop();
            if (reader != System.IntPtr.Zero) { API_cwipc_util.cwipc_source_free(reader); reader = System.IntPtr.Zero; } // Free RealSense reader???
            if (currentBuffer != System.IntPtr.Zero) { API_cwipc_util.cwipc_source_free(currentBuffer); currentBuffer = System.IntPtr.Zero; }
        }

        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                currentBuffer = API_cwipc_util.cwipc_source_get(reader);
                if (currentBuffer != System.IntPtr.Zero) {
                    token.currentBuffer = currentBuffer;
                    Next();
                }
            }
        }
    }
}
