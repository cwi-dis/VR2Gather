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
            Start();
            Debug.Log($"{Name()}: Inited");
            stats = new Stats(Name());
        }
        public override string Name() {
            return $"{GetType().Name}#{instanceNumber}";
        }


        public override void OnStop()
        {
            for (int i=0; i<outputs.Length; i++) {
                outputs[i].outQueue.Close();
                outputs[i].outQueue = null;
            }
            base.OnStop();
        }

        protected override void Update()
        {
            base.Update();
            cwipc.pointcloud pc = (cwipc.pointcloud)inQueue.TryDequeue(0);
            if (pc == null) return; // Terminating, or no pointcloud currently available
            for (int i=0; i<outputs.Length; i++)
            {
                NativeMemoryChunk mc = null;
                if (outputs[i].tileNumber == 0)
                {
                    int size = pc.copy_packet(System.IntPtr.Zero, 0);
                    mc = new NativeMemoryChunk(size);
                    pc.copy_packet(mc.pointer, mc.length);
                }
                else
                {
                    cwipc.pointcloud pcTile = cwipc.tilefilter(pc, outputs[i].tileNumber);
                    int size = pcTile.copy_packet(System.IntPtr.Zero, 0);
                    mc = new NativeMemoryChunk(size);
                    pcTile.copy_packet(mc.pointer, mc.length);
                    pcTile.free();

                }
                bool dropped = !outputs[i].outQueue.Enqueue(mc); ;
                stats.statsUpdate(dropped, outputs[i].outQueue.QueuedDuration());
                
            }
            pc.free();
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalPointclouds = 0;
            double statsTotalDropped = 0;
            double statsTotalQueuedDuration = 0;

            public void statsUpdate(bool dropped, ulong queuedDuration)
            {
                statsTotalPointclouds++;
                statsTotalQueuedDuration += queuedDuration;
                if (dropped) statsTotalDropped++;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalPointclouds / Interval():F2}, fps_dropped={statsTotalDropped / Interval():F2}, transmitter_queue_ms={statsTotalQueuedDuration / statsTotalPointclouds}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPointclouds = 0;
                }
            }
        }

        protected Stats stats;

    }
}