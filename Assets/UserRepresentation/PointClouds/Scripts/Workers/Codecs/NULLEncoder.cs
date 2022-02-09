using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class NULLEncoder : BaseWorker
    {
        cwipc.encodergroup encoderGroup;
        System.IntPtr encoderBuffer;
        QueueThreadSafe inQueue;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        PCEncoder.EncoderStreamDescription[] outputs;

        public NULLEncoder(QueueThreadSafe _inQueue, PCEncoder.EncoderStreamDescription[] _outputs) : base()
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
                Timedelta encodeDuration = 0;
                NativeMemoryChunk mc = null;
                if (outputs[i].tileNumber == 0)
                {
                    System.DateTime encodeStartTime = System.DateTime.Now;
                    int size = pc.copy_packet(System.IntPtr.Zero, 0);
                    mc = new NativeMemoryChunk(size);
                    pc.copy_packet(mc.pointer, mc.length);
                    mc.info.timestamp = pc.timestamp();
                    System.DateTime encodeStopTime = System.DateTime.Now;
                    encodeDuration = (Timedelta)(encodeStopTime - encodeStartTime).TotalMilliseconds;
                }
                else
                {
                    System.DateTime encodeStartTime = System.DateTime.Now;
                    cwipc.pointcloud pcTile = cwipc.tilefilter(pc, outputs[i].tileNumber);
                    int size = pcTile.copy_packet(System.IntPtr.Zero, 0);
                    mc = new NativeMemoryChunk(size);
                    pcTile.copy_packet(mc.pointer, mc.length);
                    mc.info.timestamp = pc.timestamp();
                    pcTile.free();
                    System.DateTime encodeStopTime = System.DateTime.Now;
                    encodeDuration = (Timedelta)(encodeStopTime - encodeStartTime).TotalMilliseconds;
                }
                QueueThreadSafe queue = outputs[i].outQueue;
                Timedelta queuedDuration = queue.QueuedDuration();
                bool dropped = !queue.Enqueue(mc);
                stats.statsUpdate(dropped, encodeDuration, queuedDuration);
            }
            pc.free();
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalPointclouds = 0;
            double statsTotalDropped = 0;
            double statsTotalEncodeDuration = 0;
            double statsTotalQueuedDuration = 0;

            public void statsUpdate(bool dropped, Timedelta encodeDuration, Timedelta queuedDuration)
            {
                statsTotalPointclouds++;
                statsTotalEncodeDuration += encodeDuration;
                statsTotalQueuedDuration += queuedDuration;
                if (dropped) statsTotalDropped++;

                if (ShouldOutput())
                {
                    Output($"fps={statsTotalPointclouds / Interval():F2}, fps_dropped={statsTotalDropped / Interval():F2}, encoder_ms={statsTotalEncodeDuration / statsTotalPointclouds}, transmitter_queue_ms={statsTotalQueuedDuration / statsTotalPointclouds}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPointclouds = 0;
                    statsTotalEncodeDuration = 0;
                    statsTotalQueuedDuration = 0;
                }
            }
        }

        protected Stats stats;

    }
}