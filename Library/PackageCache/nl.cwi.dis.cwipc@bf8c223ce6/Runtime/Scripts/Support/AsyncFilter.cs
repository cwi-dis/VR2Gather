using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    /// <summary>
    /// Abstract base class for asynchronous filters: objects that read items from
    /// an input queue, do an operation on them and then deposit the result on an output queue.
    ///
    /// Examples of such filters are encoders and decoders.
    /// </summary>
    public abstract class AsyncFilter : AsyncWorker
    {
        protected QueueThreadSafe inQueue;
        protected QueueThreadSafe outQueue;

        protected AsyncFilter(QueueThreadSafe _inQueue, QueueThreadSafe _outQueue)
        {
            if (_inQueue == null)
            {
                throw new System.Exception($"{Name()}: inQueue is null");
            }
            if (_outQueue == null)
            {
                throw new System.Exception($"{Name()}: outQueue is null");
            }
            inQueue = _inQueue;
            outQueue = _outQueue;
        }
    }
}