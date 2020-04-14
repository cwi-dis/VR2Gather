using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseMemoryChunkReferences {
    static List<Type> types = new List<Type>();
    public static void AddReference(Type _type) {
        lock(types) {
            types.Add(_type);
        }
    }
    public static void DeleteReference(Type _type) {
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

public class BaseMemoryChunk {
    IntPtr          _pointer;
    int             refCount;

    protected BaseMemoryChunk(IntPtr _pointer) {
        if (_pointer== IntPtr.Zero)  throw new Exception("BaseMemoryChunk: constructor called with null pointer");
        this._pointer = _pointer;
        refCount = 0;
        BaseMemoryChunkReferences.AddReference( this.GetType() );
    }

    protected BaseMemoryChunk() {
        throw new Exception("BaseMemoryChunk: default constructor called");
    }

    public IntPtr reference { get {  refCount++; return _pointer; } }
    public IntPtr pointer { get { return _pointer; } }

    public void free() {
        if( --refCount < 1) {
            lock (this) {
                if (_pointer!=IntPtr.Zero) {
                    onfree();
                    _pointer = IntPtr.Zero;
                    BaseMemoryChunkReferences.DeleteReference(this.GetType());
                }
            }
        }
    }

    protected virtual void onfree() {
    }
}
