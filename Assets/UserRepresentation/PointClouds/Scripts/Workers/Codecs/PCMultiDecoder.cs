using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using VRT.Core;

namespace VRT.UserRepresentation.PointCloud
{
    public class PCMultiDecoder : PCDecoder
    {
        public PCMultiDecoder(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue) : base(_inQueue, _outQueue)
        {
        }

        protected override void Update()
        {
            NativeMemoryChunk mc;
            int order;
            lock (this)
            {
                mc = (NativeMemoryChunk)inQueue.Dequeue();
                if (mc == null) return;
                if (decoder == null) return;
                order = (outQueue as QueueOrderedThreadSafe).order++;
            }
            decoder.feed(mc.pointer, mc.length);
            mc.free();
            while (decoder.available(false))
            {
                cwipc.pointcloud pc = decoder.get();
                if (pc == null)
                {
                    throw new System.Exception($"{Name()}: cwipc_decoder: available() true, but did not return a pointcloud");
                }
                stats.statsUpdate(pc.count(), pc.timestamp());
                (outQueue as QueueOrderedThreadSafe).Enqueue(pc, order, this);
            }
        }
    }
}