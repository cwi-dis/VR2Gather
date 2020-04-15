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
    const bool debugAllocations = true; 
    IntPtr          _pointer;
    int             refCount;

    protected void _debugPrint(string message)
    {
        Debug.Log($"BaseMemoryChunk {this.GetType().Name}({this.GetHashCode()}): {message}");
    }
    protected BaseMemoryChunk(IntPtr _pointer) {
        if (_pointer== IntPtr.Zero)  throw new Exception($"{this.GetType().Name} {this.GetHashCode()}: constructor called with null pointer");
        this._pointer = _pointer;
        if (debugAllocations) _debugPrint("allocated");
        refCount = 0;
        BaseMemoryChunkReferences.AddReference( this.GetType() );
    }

    protected BaseMemoryChunk() {
        throw new Exception("BaseMemoryChunk: default constructor called");
    }

    public IntPtr reference { 
        get {
            if (_pointer == IntPtr.Zero) throw new Exception($"{this.GetType().Name} {this.GetHashCode()}: reference called after free()");
            refCount++;
            if (debugAllocations) _debugPrint($"reference, count={refCount}");
            return _pointer; 
        } 
    }
    public IntPtr pointer { 
        get {
            if (_pointer == IntPtr.Zero) throw new Exception($"{this.GetType().Name} {this.GetHashCode()}: pointer called after free()");
            return _pointer; 
        } 
    }

    public void free() {
        if (_pointer == IntPtr.Zero) throw new Exception($"{this.GetType().Name} {this.GetHashCode()}: free() called after free()");
        if ( --refCount < 1) {
            lock (this) {
                if (debugAllocations) _debugPrint($"free, count={refCount}, onfree()");
                if (_pointer!=IntPtr.Zero) {
                    onfree();
                    _pointer = IntPtr.Zero;
                    BaseMemoryChunkReferences.DeleteReference(this.GetType());
                }
            }
        } else
        {
            if (debugAllocations) _debugPrint($"free, count={refCount}");

        }
    }

    protected virtual void onfree() {
    }
}
