using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    /// <summary>
    /// A queue of BaseMemoryChunk objects. Used by AsyncWorker classes to transfer frame data between
    /// thread without copying large volumes of memory.
    ///
    /// The queues have a fixed maximum size, and it it configurable whether a full queue
    /// will result in a dropped frame or wait until space becomes available.
    /// </summary>
    public class QueueThreadSafe
    {
       
        string name;
        int size;
        bool dropWhenFull;
        CancellationTokenSource isClosed;
        Queue<BaseMemoryChunk> queue;
        Timestamp latestTimestamp = 0;
        Timestamp latestTimestampReturned = 0;
        SemaphoreSlim empty;
        SemaphoreSlim full;

        // Concurrent queue with limited capacity.
        // Enqueue semantics depend on _dropWhenFull: for _dropWhenFull=true the item
        // will be discarded, for _dropWhenFull the call will wait until space is available.
        // Dequeue always waits for an item to become available.
        /// <summary>
        /// Create q QueueThreadSafe.
        /// </summary>
        /// <param name="name">Name of the queue (mainly for debug log messages and statistics)</param>
        /// <param name="_size">Maximum size of the queue</param>
        /// <param name="_dropWhenFull">Drop frame on push when queue is full</param>
        public QueueThreadSafe(string name, int _size = 2, bool _dropWhenFull = false)
        {
            this.name = name;
            size = _size;
            dropWhenFull = _dropWhenFull;
            queue = new Queue<BaseMemoryChunk>(size);
            empty = new SemaphoreSlim(size, size);
            full = new SemaphoreSlim(0, size);
            isClosed = new CancellationTokenSource();
        }

        ~QueueThreadSafe()
        {
            if (!IsClosed())
            {
                UnityEngine.Debug.LogWarning($"{Name()}: Not closed before finalizer called. Deleting items.");
                while (true)
                {
                    BaseMemoryChunk item = TryDequeue(0);
                    if (item == null) break;
                    item.free();
                }
            }
        }

        public string Name()
        {
            return $"{GetType().Name}#{name}";
        }

        /// <summary>
        /// Called by the producer to signal that no more frame will be Enqueue()ed.
        /// Any frames still in the queue will be discarded.
        /// </summary>
        public void Close()
        {
            if (isClosed.Token.IsCancellationRequested)
            {
                UnityEngine.Debug.LogWarning($"{Name()}: Close() on closed queue {name}");
                return;
            }
#if CWIPC_WITH_LOGGING
            UnityEngine.Debug.Log($"{Name()}: closing");
#endif
            isClosed.Cancel();
            while (true)
            {
                BaseMemoryChunk item = TryDequeue(0);
                if (item == null) break;
                item.free();
            }
        }

        /// <summary>
        /// Return true if the queue has been closed.
        /// </summary>
        /// <returns></returns>
        public bool IsClosed()
        {
            return isClosed.Token.IsCancellationRequested;
        }

        /// <summary>
        /// Return true if we can probably enqueue something (but note that there is no guarantee if we have multiple producers)
        /// </summary>
        /// <returns>True if there is currently space in the queue</returns>
        public bool _CanEnqueue()
        {
            try
            {
                if (empty.Wait(0, isClosed.Token))
                {
                    // A slot is available. We got that slot, se we immedeately  return it.
                    empty.Release();
                    return true;
                }
            }
            catch (System.OperationCanceledException)
            {
            }
            return false;
        }

        /// <summary>
        /// Return true if we can probably dequeue something (but note that there is no guarantee if we have multiple consumers)
        /// </summary>
        /// <returns>True if there are curretnly frames in the queue</returns>
        public bool _CanDequeue()
        {
            try
            {
                if (full.Wait(0, isClosed.Token))
                {
                    // A slot is available. We got that slot, se we immedeately  return it.
                    full.Release();
                    return true;
                }
            }
            catch (System.OperationCanceledException)
            {
            }
            return false;
        }

        /// <summary>
        /// Return number of items in the queue (but note that active producers or consumers can cause this value to change quickly)
        /// </summary>
        public int _Count
        {
            get
            {
                try
                {
                    if (full.Wait(0, isClosed.Token))
                    {
                        int count = full.Release();
                        return count + 1;
                    }
                }
                catch (System.OperationCanceledException)
                {
                }
                return 0;
            }
        }

        /// <summary>
        /// Return the item that will probably be returned by the next Dequeue (but unsafe if we have multiple consumers).
        /// Returns null if there are no frames in the queue.
        /// </summary>
        /// <returns>a BaseMemoryChunk or null</returns>
        public BaseMemoryChunk _Peek()
        {
            lock (queue)
            {
                if (queue.Count <= 0) return null;
                return queue.Peek();
            }
        }

        /// <summary>
        /// Return timestamp of next frame, or zeroReturn if frame has no timestamp, or 0 if there is nothing in
        /// the queue. Potentially unsafe.
        /// This method is used by the synchronizing IPreparer implementations to determine their possible
        /// range of timestamps.
        /// </summary>
        /// <param name="zeroReturn">Value to return if the queue is empty</param>
        /// <returns>a timestamp</returns>
        public Timestamp _PeekTimestamp(Timestamp zeroReturn = 0)
        {
            BaseMemoryChunk head = _Peek();
            if (head != null)
            {
                Timestamp rv = (Timestamp)head.metadata.timestamp;
                if (rv == 0) rv = zeroReturn;
                return rv;
            }
            return 0;
        }

        /// <summary>
        /// Return timestamp of most recently pushed frame, or 0 if there is nothing in
        /// the queue. Potentially unsafe.
        /// This method is used by the synchronizing IPreparer implementations to determine their possible
        /// range of timestamps.
        /// </summary>
        /// <returns>a timestamp</returns>
        public Timestamp LatestTimestamp()
        {
            return latestTimestamp;
        }

        /// <summary>
        /// Return the time span of the queue (difference of timestamps of earliest and latest timestamps)
        /// </summary>
        /// <returns>A timestamp difference in milliseconds.</returns>
        public Timedelta QueuedDuration()
        {
            if (latestTimestampReturned == 0 || latestTimestamp == 0 || latestTimestampReturned > latestTimestamp)
            {
                //UnityEngine.Debug.Log($"xxxjack Queue not fully operational yet: latestTimestampReturned={latestTimestampReturned}, latestTimestamp={latestTimestamp}");
                return 0;
            }
            return latestTimestamp - latestTimestampReturned;
        }

        /// <summary>
        /// Return how many frames there are currently in the queue.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return queue.Count;
        }


        /// <summary>
        /// Get the next item from the queue.
        /// Wait semantics: waits until something is available.
        /// The caller gets ownership of the returned object.
        /// If the queue was closed null will be returned.
        /// </summary>
        /// <returns>A BaseMemoryChunk or null</returns>
        public virtual BaseMemoryChunk Dequeue()
        {
            try
            {
                full.Wait(isClosed.Token);
                BaseMemoryChunk item;
                lock (queue)
                {
                    item = queue.Dequeue();
                    latestTimestampReturned = item.metadata.timestamp;
                }
                empty.Release();
                return item;
            }
            catch (System.OperationCanceledException)
            {
            }
            return null;
        }

        /// <summary>
        /// Get the next item from the queue, waiting at most millisecondsTimeout
        /// (which can be 0) for an item to become available.
        /// Ownership of the item is transferred to the caller.
        /// If no item is available in time null is returned.
        /// </summary>
        /// <param name="millisecondsTimeout">timeout in milliseconds</param>
        /// <returns>The frame or null</returns>
        public virtual BaseMemoryChunk TryDequeue(int millisecondsTimeout)
        {
            try
            {
                bool gotItem = full.Wait(millisecondsTimeout, isClosed.Token);
                if (gotItem)
                {
                    BaseMemoryChunk item;
                    lock (queue)
                    {
                        item = queue.Dequeue();
                        latestTimestampReturned = item.metadata.timestamp;
                    }
                    empty.Release();
                    return item;
                }
            }
            catch (System.OperationCanceledException)
            {
            }
            return null;
        }

        /// <summary>
        /// Put an item in the queue.
        /// If there is no space this call waits until there is space available.
        /// The ownership of the item is transferred to the queue. If the item cannot be
        /// put in the queue (if the queue is leaky, or if the queue has been closed) the item will be
        /// freed.
        ///
        /// Note that this item will be dropped if the queue was full, not the head of the queue.
        /// That would seem better, but it messes up the timestamp handling for ISynchronizer because the
        /// queue can change in incompatible ways while the calculations are taking place.
        /// </summary>
        /// <param name="item">The frame to put in the queue</param>
        /// <returns>True if the item was deposited in the queue false if it was dropped</returns>
        public virtual bool Enqueue(BaseMemoryChunk item)
        {
            if (dropWhenFull)
            {
                return EnqueueWithDrop(item);
            }
            try
            {
                empty.Wait(isClosed.Token);
                lock (queue)
                {
                    latestTimestamp = item.metadata.timestamp;
                    if (latestTimestamp == 0)
                    {
#if CWIPC_WITH_LOGGING
                        UnityEngine.Debug.Log($"{Name()}: Enqueue() got item with timestamp=0");
#endif
                    }
                    queue.Enqueue(item);
                }
                full.Release();
                return true;
            }
            catch (System.OperationCanceledException)
            {
                UnityEngine.Debug.LogWarning($"{Name()}: Enqueue on closed queue {name}");
                item.free();
            }
            return false;
        }

        // Put an item in the queue, make room if there isn't.
        // If an item is dropped free it.
        bool EnqueueWithDrop(BaseMemoryChunk item)
        {
            try
            {
                lock(queue)
                {
                    bool gotSlot = empty.Wait(0, isClosed.Token);
                    if (!gotSlot)
                    {
                        // No room. Get oldest item and free it.
                        // Note that the lock() in Dequeue doesn't bother us because we are in the same thread.
                        // But: if we have a 1-entry queue we could end up in a livelock if we use
                        // dequeue() because the consumer does the Wait outside the lock. So we have to cater
                        // for it overtaking us and grabbing the item, in which case we would be stuck in a livelock.
                       
                        BaseMemoryChunk oldItem = TryDequeue(0);
                        if (oldItem == null)
                        {
                            item.free();
                            return false;
                        }
                        empty.Wait(isClosed.Token);
                    }
                    latestTimestamp = item.metadata.timestamp;
                    if (latestTimestamp == 0)
                    {
#if CWIPC_WITH_LOGGING
                        UnityEngine.Debug.Log($"{Name()}: TryEnqueue() got item with timestamp=0");
#endif
                    }
                    queue.Enqueue(item);
                    full.Release();
                    return gotSlot;
                }
            }
            catch (System.OperationCanceledException)
            {
            }
            item.free();
            return false;
        }
    }
}