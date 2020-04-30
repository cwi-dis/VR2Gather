using System.Collections.Generic;

public class QueueThreadSafe {
    public QueueThreadSafe(int _size=2) { Size = _size; }
    public int Size{ get; private set; }
    public int Count { get { lock (queue) { return queue.Count; } } }
    Queue<BaseMemoryChunk> queue = new Queue<BaseMemoryChunk>();

    public BaseMemoryChunk Peek() { lock (queue) { return queue.Peek(); } }
    public BaseMemoryChunk Dequeue() { lock (queue) { return queue.Dequeue(); } }
    public void Enqueue(BaseMemoryChunk item) { lock (queue) { queue.Enqueue(item); } }
}

