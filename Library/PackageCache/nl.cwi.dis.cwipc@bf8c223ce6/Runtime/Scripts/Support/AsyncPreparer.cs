using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    /// <summary>
    /// Abstract base class for objects implementing IPreparer asynchronously, through the AsyncWorker class.
    /// </summary>
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

        /// <summary>
        /// Assign a synchronizer to this stream. See ISynchronizer documentation for details.
        /// </summary>
        /// <param name="_synchronizer">The synchronizer.</param>
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

        public bool EndOfData()
        {
            return InQueue == null || (InQueue.IsClosed() && InQueue.Count() == 0);
        }

    }
}