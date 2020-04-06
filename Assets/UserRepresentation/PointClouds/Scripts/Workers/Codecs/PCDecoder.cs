using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class PCDecoder : BaseWorker {
        cwipc.decoder decoder;
        cwipc.pointcloud pointCloudData;
        
        public PCDecoder():base(WorkerType.Run) {
            try {
                decoder = cwipc.new_decoder();
                if (decoder == null)
                    throw new System.Exception("PCSUBReader: cwipc_new_decoder creation failed"); // Should not happen, should throw exception
                else {
                    Start();
                    Debug.Log("PCDecoder Inited");
                }

            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
            
        }

        public override void OnStop() {
            base.OnStop();
            decoder?.free();
            decoder = null;
            Debug.Log("PCDecoder Stopped");
        }

        protected override void Update(){
            base.Update();
            if (token != null) {
                lock (token) {
                    decoder.feed(token.currentBuffer, token.currentSize);
                    if (decoder.available(true)) {
                        pointCloudData = decoder.get();
                        if (pointCloudData != null) {
                            token.currentPointcloud = pointCloudData;
                            statsUpdate(pointCloudData.count(), pointCloudData.timestamp());
                            Next();
                        }
                        else {
                            Debug.LogError("PCSUBReader: cwipc_decoder: available() true, but did not return a pointcloud");
                        }

                    }
                    else
                        Debug.LogError($"PCSUBReader: cwipc_decoder: no pointcloud available currentSize {token.currentSize}");
                }
            }
        }

        System.DateTime statsLastTime;
        double statsTotalPoints;
        double statsTotalPointclouds;
        double statsTotalLatency;

        public void statsUpdate(int pointCount, ulong timeStamp)
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            double latency = (double)(sinceEpoch.TotalMilliseconds - timeStamp) / 1000.0;
            if (statsLastTime == null)
            {
                statsLastTime = System.DateTime.Now;
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
                statsTotalLatency = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(10))
            {
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: PCDecoder: {statsTotalPointclouds / 10} fps, {(int)(statsTotalPoints / statsTotalPointclouds)} points per cloud, latency {statsTotalLatency/statsTotalPointclouds}");
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
                statsTotalLatency = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalPoints += pointCount;
            statsTotalPointclouds += 1;
            statsTotalLatency += latency;
        }
    }
}