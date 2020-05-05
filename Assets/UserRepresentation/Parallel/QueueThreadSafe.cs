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

    public QueueThreadSafe(int _size = 2) { Size = _size; auxSize = _size * 2; window = new BaseMemoryChunk[auxSize]; }
    public int Size { get; private set; }
    public bool Free() { lock (window) { return alloc - read < Size; } } // retorna el hueco libre incluso los prereservados
    public int Count { get { lock (window) { return write -read; } } } // Retorna los datos reales disponibles.
    public int Alloc() { lock (window) { return alloc++; } }

    public BaseMemoryChunk Peek() { lock (window) { return window[read % auxSize]; } }
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
    public void Enqueue(BaseMemoryChunk item, int position = -1) {
        lock (window) {
            if (position == -1) position=alloc++;
            window[position % auxSize] = item;
            for(int i=write; i != alloc;++i) {
                if (window[i % auxSize] != null)
                    write = i;
            }
//            UnityEngine.Debug.Log($"alloc {alloc} position {position} write {write} read {read} ");
        }
    }

}

