using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class NULLEncoder : BaseWorker
    {
        cwipc.encodergroup encoderGroup;
        System.IntPtr encoderBuffer;
        QueueThreadSafe inQueue;
        QueueThreadSafe outQueue;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        PCEncoder.EncoderStreamDescription[] outputs;

        public NULLEncoder(QueueThreadSafe _inQueue, PCEncoder.EncoderStreamDescription[] _outputs) : base(WorkerType.Run)
        {
            if (_inQueue == null)
            {
                throw new System.Exception("{Name()}: inQueue is null");
            }
            inQueue = _inQueue;
            outputs = _outputs;
            int nOutputs = outputs.Length;
            if (nOutputs != 1)
            {
                throw new System.Exception($"{Name()}: only single output supported");
            }
            outQueue = _outputs[0].outQueue;

            Start();
            Debug.Log($"{Name()}: Inited");
            stats = new Stats(Name());
        }
        public override string Name() {
            return $"{GetType().Name}#{instanceNumber}";
        }


        public override void OnStop()
        {
            if (outQueue != null)
            {
                outQueue.Close();
                outQueue = null;
            }
            base.OnStop();
        }

        protected override void Update()
        {
            base.Update();
            cwipc.pointcloud pc = (cwipc.pointcloud)inQueue.Dequeue();
            if (pc == null) return; // Terminating
            int size = pc.copy_packet(System.IntPtr.Zero, 0);
            NativeMemoryChunk mc = new NativeMemoryChunk(size);
            pc.copy_packet(mc.pointer, mc.length);
            stats.statsUpdate();
            outputs[0].outQueue.Enqueue(mc);    
            pc.free();
        }

        protected class Stats : VRT.Core.BaseStats {
            public Stats(string name) : base(name) { }

            double statsTotalPointclouds = 0;

            public void statsUpdate() {
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                statsTotalPointclouds++;

                if (ShouldOutput()) {
                    Output($"fps={statsTotalPointclouds / Interval():F2}");
                }
                if (ShouldClear()) {
                    Clear();
                    statsTotalPointclouds = 0;
                }
            }
        }

        protected Stats stats;

    }
}