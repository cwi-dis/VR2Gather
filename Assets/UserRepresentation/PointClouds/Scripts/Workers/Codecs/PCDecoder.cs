using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class PCDecoder : BaseWorker {
        cwipc.decoder decoder;
        QueueThreadSafe inQueue;
        QueueThreadSafe outQueue;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public PCDecoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue) :base(WorkerType.Run) {
            if (_inQueue == null)
            {
                throw new System.Exception("PCDecoder: inQueue is null");
            }
            if (_outQueue == null)
            {
                throw new System.Exception("PCDecoder: outQueue is null");
            }
            try
            {
                inQueue = _inQueue;
                outQueue = _outQueue;
                decoder = cwipc.new_decoder();
                if (decoder == null)
                    throw new System.Exception("PCSUBReader: cwipc_new_decoder creation failed"); // Should not happen, should throw exception
                else {
                    Start();
                    Debug.Log($"PCDecoder#{instanceNumber} Inited");
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
            Debug.Log($"PCDecoder#{instanceNumber} Stopped");
        }

        protected override void Update(){
            base.Update();
            if (inQueue.Count > 0 ) {
                NativeMemoryChunk mc = (NativeMemoryChunk)inQueue.Dequeue();
                if (!outQueue.Free())
                {
                    Debug.Log($"PCDecoder#{instanceNumber}: skip decode, no room in outQueue");
                    mc.free();
                    return;
                }
                decoder.feed(mc.pointer, mc.length);
                mc.free();
                if (decoder.available(true)) {
                    cwipc.pointcloud pc = decoder.get();
                    if (pc != null) {
                        if (outQueue.Free())
                        {
                            statsUpdate(pc.count(), pc.timestamp());
                            outQueue.Enqueue(pc);
                        }
                        else
                        {
                            Debug.LogError($"PCDecoder#{instanceNumber}: after decode, no room in outQueue any more");
                            pc.free();
                        }
                    } else throw new System.Exception($"PCDecoder#{instanceNumber}: cwipc_decoder: available() true, but did not return a pointcloud");
                }
                else
                    Debug.LogError($"PCDecoder#{instanceNumber}: cwipc_decoder: no pointcloud available currentSize {mc.length}");
            }
        }

        System.DateTime statsLastTime;
        double statsTotalPoints;
        double statsTotalPointclouds;
        double statsTotalLatency;

        public void statsUpdate(int pointCount, ulong timeStamp) {
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
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: PCDecoder#{instanceNumber}: {statsTotalPointclouds / 10} fps, {(int)(statsTotalPoints / statsTotalPointclouds)} points per cloud, latency {statsTotalLatency/statsTotalPointclouds}");
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