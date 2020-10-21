using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Workers;

public class QueueOrderedThreadSafe : QueueThreadSafe {
    public int order=0;
    int turn = 0;
    public QueueOrderedThreadSafe(string name, int _size = 2, bool _dropWhenFull = false) : base(name, _size, _dropWhenFull) {
    }

    public override BaseMemoryChunk Dequeue() {
        return base.Dequeue();
    }
    public override BaseMemoryChunk TryDequeue(int millisecondsTimeout) {
        return base.TryDequeue(millisecondsTimeout);
    }
    public bool Enqueue(BaseMemoryChunk item, int order, BaseWorker worker) {
        while (worker.isRunning && order != turn) Thread.Sleep(0);
        lock (this) {
            bool ret = base.Enqueue(item);
            turn++;
            return ret;
        }

    }

    public bool TryEnqueue(int millisecondsTimeout, BaseMemoryChunk item, int order, BaseWorker worker) {
        while (worker.isRunning && order != turn) Thread.Sleep(0);
        lock (this) {
            bool ret = base.TryEnqueue(millisecondsTimeout, item);
            turn++;
            return ret;
        }

    }


}
