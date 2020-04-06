using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMemoryChunkReferences {
    static List<System.Type> types = new List<System.Type>();
    public static void AddReference(System.Type _type) {
        lock(types) {
            types.Add(_type);
        }
    }
    public static void DeleteReference(System.Type _type) {
        lock (types) {
            types.Remove(_type);
        }
    }

    public static void ShowTotalRefCount() {
        lock (types) {
            Debug.Log($"{types.Count} TotalRefCount pending.");
            for (int i = 0; i < types.Count; ++i)
                Debug.Log($"(i)--> {types[i]}");
        }
    }

}

public class BaseMemoryChunk<T> {
    T       _pointer;
    int     refCount;

    protected BaseMemoryChunk(T _pointer) {
        if (EqualityComparer<T>.Default.Equals(_pointer, default(T))) {
            throw new System.Exception("cwipc_pointcloud: constructor called with null pointer");
        }
        this._pointer = _pointer;
        refCount = 0;
        BaseMemoryChunkReferences.AddReference( this.GetType() );
    }

    protected BaseMemoryChunk() {
        throw new System.Exception("BaseMemoryChunk: default constructor called");
    }

    public T reference { get {  refCount++; return _pointer; } }
    public T pointer { get { return _pointer; } }

    public void free() {
        if( --refCount < 1) {
            lock (this) {
                if (!EqualityComparer<T>.Default.Equals(_pointer, default(T))) {
                    onfree();
                    _pointer = default(T);
                    BaseMemoryChunkReferences.DeleteReference(this.GetType());
                }
            }
        }
    }

    protected virtual void onfree() {
    }
}
