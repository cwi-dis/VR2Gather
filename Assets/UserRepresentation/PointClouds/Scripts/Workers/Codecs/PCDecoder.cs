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
                    Debug.Log($"{Name()} Inited");
                }

            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
            
        }

        public override string Name()
        {
            return $"{this.GetType().Name}#{instanceNumber}";
        }

        public override void Stop()
        {
            base.Stop();
            if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
        }

        public override void OnStop() {
            base.OnStop();
            lock (this)
            {
                decoder?.free();
                decoder = null;
                if (outQueue != null && !outQueue.IsClosed()) outQueue.Close();
            }
            if (debugThreading) Debug.Log($"{Name()} Stopped");
        }

        protected override void Update(){
            base.Update();
            NativeMemoryChunk mc;
            lock (this)
            {
                mc = (NativeMemoryChunk)inQueue.Dequeue();
                if (mc == null) return;
                if (decoder == null) return;
            }
            decoder.feed(mc.pointer, mc.length);
            mc.free();
            while (decoder.available(false)) {
                cwipc.pointcloud pc = decoder.get();
                if (pc == null)
                {
                    throw new System.Exception($"{Name()}: cwipc_decoder: available() true, but did not return a pointcloud");
                }
                statsUpdate(pc.count(), pc.timestamp());
                outQueue.Enqueue(pc);
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
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: {Name()}: {statsTotalPointclouds / 10} fps, {(int)(statsTotalPoints / statsTotalPointclouds)} points per cloud, {statsTotalLatency/statsTotalPointclouds} seconds pipeline latency");
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