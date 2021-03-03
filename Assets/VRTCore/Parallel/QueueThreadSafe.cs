using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace VRTCore
{
    public class QueueThreadSafe
    {

        string name;
        int size;
        bool dropWhenFull;
        CancellationTokenSource isClosed;
        Queue<BaseMemoryChunk> queue;
        SemaphoreSlim empty;
        SemaphoreSlim full;

        // Concurrent queue with limited capacity.
        // Enqueue semantics depend on _dropWhenFull: for _dropWhenFull=true the item
        // will be discarded, for _dropWhenFull the call will wait until space is available.
        // Dequeue always waits for an item to become available.
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

        // Close the queue for further pushes, signals to consumers that we are about to stop
        public void Close()
        {
            if (isClosed.Token.IsCancellationRequested)
            {
                UnityEngine.Debug.LogWarning($"QueueThreadSafe: Close() on closed queue {name}");
                return;
            }
            UnityEngine.Debug.Log($"[FPA] Closing {name} queue.");
            isClosed.Cancel();
            while (true)
            {
                BaseMemoryChunk item = TryDequeue(0);
                if (item == null) break;
                item.free();
            }
        }

        // Return true if the queue is closed and we are about to stop
        public bool IsClosed()
        {
            return isClosed.Token.IsCancellationRequested;
        }

        // Return true if we can probably enqueue something (but note that there is no guarantee if we have multiple producers)
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

        // Return true if we can probably dequeue something (but note that there is no guarantee if we have multiple consumers) 
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

        // Return number of items in the queue (but note that active producers or consumers can cause this value to change quickly)
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

        // Return the item that will probably be returned by the next Dequeue (but unsafe if we have multiple consumers)
        public BaseMemoryChunk _Peek()
        {
            lock (queue)
            {
                if (queue.Count <= 0) return null;
                return queue.Peek();
            }
        }

        // Return timestamp of next frame, or zeroReturn if frame has no timestamp, or 0 if there is nothing in
        // the queue. Potentially unsafe.
        public ulong _PeekTimestamp(ulong zeroReturn=0)
        {
            BaseMemoryChunk head = _Peek();
            if (head != null)
            {
                ulong rv = (ulong)head.info.timestamp;
                if (rv == 0) rv = zeroReturn;
                return rv;
            }
            return 0;
        }

        // Get the next item from the queue.
        // Wait semantics: waits until something is available.
        // The caller gets ownership of the returned object.
        // If the queue was closed null will be returned. 
        public virtual BaseMemoryChunk Dequeue()
        {
            try
            {
                full.Wait(isClosed.Token);
                BaseMemoryChunk item;
                lock (queue)
                {
                    item = queue.Dequeue();
                }
                empty.Release();
                return item;
            }
            catch (System.OperationCanceledException)
            {
            }
            return null;
        }

        // Get the next item from the queue, waiting at most millisecondsTimeout
        // (which can be 0) for an item to become available.
        // Ownership of the item is transferred to the caller.
        // If no item is available in time null is returned.
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

        // Put an item in the queue.
        // If there is no space this call waits until there is space available.
        // The ownership of the item is transferred to the queue (so it will be freed
        // if there is no space and the caller should not reuse or free this item
        // unless it has done ann AddRef()).
        public virtual bool Enqueue(BaseMemoryChunk item)
        {
            if (dropWhenFull)
            {
                return TryEnqueue(0, item);
            }
            try
            {
                empty.Wait(isClosed.Token);
                lock (queue)
                {
                    queue.Enqueue(item);
                }
                full.Release();
                return true;
            }
            catch (System.OperationCanceledException)
            {
                UnityEngine.Debug.LogWarning($"QueueThreadSafe: Enqueue on closed queue {name}");
                item.free();
            }
            return false;
        }

        // Put an item in the queue.
        // If there is no space this call waits for at most millisecondsTimeout until there 
        // is space available.
        // The ownership of the item is transferred to the queue (so it will be freed
        // if there is no space and the caller should not reuse or free this item
        // unless it has done ann AddRef()).
        public virtual bool TryEnqueue(int millisecondsTimeout, BaseMemoryChunk item)
        {
            try
            {
                bool gotSlot = empty.Wait(millisecondsTimeout, isClosed.Token);
                if (gotSlot)
                {
                    lock (queue)
                    {
                        queue.Enqueue(item);
                    }
                    full.Release();
                    return true;
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