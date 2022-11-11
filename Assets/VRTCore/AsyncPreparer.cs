using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;

namespace VRT.Core
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using QueueThreadSafe = Cwipc.QueueThreadSafe;

    public abstract class AsyncPreparer : AsyncWorker, IPreparer
    {
        protected ISynchronizer synchronizer = null;
        protected QueueThreadSafe InQueue;

        public AsyncPreparer(QueueThreadSafe _InQueue) : base()
        {
            if (_InQueue == null)
            {
                throw new System.Exception($"{Name()}: InQueue is null");
            }
            InQueue = _InQueue;
        }

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public virtual void SetSynchronizer(ISynchronizer _synchronizer)
        {
            synchronizer = _synchronizer;
       }

        public abstract void Synchronize();

        public abstract bool LatchFrame();
        
        public Timedelta getQueueDuration()
        {
            if (InQueue == null) return 0;
            return InQueue.QueuedDuration();
        }
    }
}