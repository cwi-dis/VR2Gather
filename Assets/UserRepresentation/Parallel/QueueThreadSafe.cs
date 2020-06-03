using System.Collections.Generic;
using System.Threading;

public class QueueThreadSafe {
    /*
    public QueueThreadSafe(int _size=2) { Size = _size; }
    public int Size { get; private set; }
    public int Free { get { lock (queue) { return Size - queue.Count; } } }
    public int Count { get { lock (queue) { return queue.Count; } } }
    Queue<BaseMemoryChunk> queue = new Queue<BaseMemoryChunk>();

    public BaseMemoryChunk Peek() { lock (queue) { return queue.Peek(); } }
    public BaseMemoryChunk Dequeue() { lock (queue) { return queue.Dequeue(); } }
    public void Enqueue(BaseMemoryChunk item) { lock (queue) { queue.Enqueue(item); } }
    */

    //BaseMemoryChunk[] window;
    //int alloc = 0;
    //int write = 0;
    //int read = 0;

    int size;
    CancellationTokenSource isClosed;
    Queue<BaseMemoryChunk> queue;
    SemaphoreSlim empty;
    SemaphoreSlim full;

    public QueueThreadSafe(int _size = 2)
    {
        size = _size;
        queue = new Queue<BaseMemoryChunk>(size);
        empty = new SemaphoreSlim(size, size);
        full = new SemaphoreSlim(0, size);
        isClosed = new CancellationTokenSource();
    }

    // Close the queue for further pushes, signals to consumers that we are about to stop
    public void Close()
    {
        if (isClosed.Token.IsCancellationRequested) throw new System.Exception("QueueThreadSafe: operation on closed queue");
        isClosed.Cancel();
    }

    // Return true if the queue is closed and we are about to stop
    public bool IsClosed() 
    {
        return isClosed.Token.IsCancellationRequested;
    }

    // Return true if we can probably enqueue something (but note that there is no guarantee if we have multiple producers)
    public bool _CanEnqueue() {
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
    public bool _CanDequeue() {
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
    public int _Count { 
        get {
            try
            {
                if (full.Wait(0, isClosed.Token))
                {
                    int count = full.Release();
                    return count + 1;
                }
            }
            catch(System.OperationCanceledException)
            {
            }
            return 0;
        } 
    }

    // Return the item that will probably be returned by the next Dequeue (but unsafe if we have multiple consumers)
    public BaseMemoryChunk _Peek() {
        return queue.Peek();
    }

    // Get the next item from the queue.
    // Wait semantics: waits until something is available.
    public BaseMemoryChunk Dequeue() {
        try
        {
            full.Wait(isClosed.Token);
            BaseMemoryChunk item = queue.Dequeue();
            empty.Release();
            return item;
        }
        catch (System.OperationCanceledException)
        {
        }
        return null;
    }

    // Put an item in the queue.
    // Wait semantics: currently does not wait but overwrites old item. Without calling free().
    public void Enqueue(BaseMemoryChunk item) {
        try
        {
            empty.Wait(isClosed.Token);
            queue.Enqueue(item);
            full.Release();
        }
        catch (System.OperationCanceledException)
        {
            UnityEngine.Debug.LogError("QueueThreadSafe: Enqueue on closed queue");
            item.free();
        }
    }

}

