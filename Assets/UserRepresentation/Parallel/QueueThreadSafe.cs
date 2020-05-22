using System.Collections.Generic;

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

    BaseMemoryChunk[] window;
    int alloc = 0;
    int write = 0;
    int read = 0;

    int auxSize;
    bool isClosed = false;

    public QueueThreadSafe(int _size = 2) { Size = _size; auxSize = _size * 2; window = new BaseMemoryChunk[auxSize]; }
    private int Size { get; set; }

    // Close the queue for further pushes, signals to consumers that we are about to stop
    public void Close()
    {
        if (isClosed) throw new System.Exception("QueueThreadSafe: operation on closed queue");
        isClosed = true;

    }

    // Return true if the queue is closed and we are about to stop
    public bool IsClosed() 
    { 
        return isClosed; 
    }

    // Return true if we can probably enqueue something (but note that there is no guarantee if we have multiple producers)
    public bool CanEnqueue() {
        lock (window) { 
            return alloc - read < Size; 
        }
    } // retorna el hueco libre incluso los prereservados

    // Return true if we can probably dequeue something (but note that there is no guarantee if we have multiple consumers) 
    public bool CanDequeue() {
        lock (window) {
            return (write - read) > 0; 
        }
    }

    // Return number of items in the queue (but note that active producers or consumers can cause this value to change quickly)
    public int Count { get { lock (window) { return write - read; } } } // Retorna los datos reales disponibles.

    // Return the item that will probably be returned by the next Dequeue (but unsafe if we have multiple consumers)
    public BaseMemoryChunk Peek() {
        lock (window) {
            return window[read % auxSize]; 
        }
    }

    // Get the next item from the queue.
    // Wait semantics: currently does not wait but raises exception if nothing is available.
    public BaseMemoryChunk Dequeue() {
        lock (window) {
            if (write - read > 0) {
                BaseMemoryChunk tmp = window[read % auxSize];
                window[read++ % auxSize] = null; // Borra la referencia.
                return tmp;
            }
            throw new System.Exception("No data on window");
        }
    }

    // Put an item in the queue.
    // Wait semantics: currently does not wait but overwrites old item. Without calling free().
    public void Enqueue(BaseMemoryChunk item) {
        lock (window) {
            int position=alloc++;
            window[position % auxSize] = item;
            for(int i=write; i != alloc;++i) {
                if (window[i % auxSize] != null)
                    write = i;
            }
//            UnityEngine.Debug.Log($"alloc {alloc} position {position} write {write} read {read} ");
        }
    }

}

