using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;

    /// <summary>
    /// Structure with metadata for a frame.
    ///
    /// Currently very much modeled after what Dash implementation in VRTogether needed.
    /// </summary>
    [Serializable]
    public class FrameMetadata
    {
        /// <summary>
        /// Presentation timestamp (milliseconds).
        /// </summary>
        public Timestamp timestamp;
        /// <summary>
        /// Per-frame metadata carried by Dash packets.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] dsi;
        /// <summary>
        /// Length of dsi.
        /// </summary>
        public int dsi_size;
        /// <summary>
        /// For pointclouds read from file: the filename it was read from.
        /// </summary>
        public string filename;
    }

    /// <summary>
    /// Class that tries to keep some reference of how many objects are allocated, to help with debugging
    /// memory leaks.
    /// </summary>
    public class BaseMemoryChunkReferences
    {
        static List<Type> types = new List<Type>();
        public static void AddReference(Type _type)
        {
            lock (types)
            {
                types.Add(_type);
            }
        }
        public static void DeleteReference(Type _type)
        {
            lock (types)
            {
                types.Remove(_type);
            }
        }

        public static void ShowTotalRefCount()
        {
            lock (types)
            {
                if (types.Count == 0) return;
                Debug.Log($"BaseMemoryChunkReferences: {types.Count} TotalRefCount pending:");
                for (int i = 0; i < types.Count; ++i)
                    Debug.Log($"BaseMemoryChunkReferences: [{i}] --> {types[i]}");
            }
        }
    }

    /// <summary>
    /// Abstract class representing a buffer in native memory. Used throughout to forestall copying
    /// data from native buffers to C# arrays only to copy it back to native buffers after a short while.
    ///
    /// These objects are explicitly refcounted, becaue the code cannot know when ownership of the object has passed
    /// from C# to some native dynamic library.
    ///
    /// Usually the buffer will hold a frame (of pointcloud, video or audio data) but this class is also used as the base class
    /// of the various cwipc objects.
    /// </summary>
    public abstract class BaseMemoryChunk
    {

        protected IntPtr _pointer;
        int refCount;
        /// <summary>
        /// Frame metadata, if this is a media frame.
        /// </summary>
        public FrameMetadata metadata;
        public int length { get; protected set; }

        protected BaseMemoryChunk(IntPtr _pointer)
        {
            if (_pointer == IntPtr.Zero) throw new Exception("BaseMemoryChunk: constructor called with null pointer");
            this._pointer = _pointer;
            this.metadata = new FrameMetadata();
            refCount = 1;
            BaseMemoryChunkReferences.AddReference(GetType());
        }

        protected BaseMemoryChunk()
        {
            // _pointer will be set later, in the subclass constructor. Not a pattern I'm happy with but difficult to
            refCount = 1;
            BaseMemoryChunkReferences.AddReference(GetType());
        }

        /// <summary>
        /// Increase reference count on this object.
        /// </summary>
        /// <returns>The object itself</returns>
        public BaseMemoryChunk AddRef()
        {
            lock (this)
            {
                refCount++;
                return this;
            }
        }

        /// <summary>
        /// Get the native pointer for this object.
        /// The caller is responsible for ensuring that the reference count on the object cannot go to
        /// zero while the pointer is in use.
        /// </summary>
        public IntPtr pointer
        {
            get
            {
                lock (this)
                {
                    if (refCount <= 0)
                    {
                        throw new Exception($"BaseMemoryChunk.pointer: refCount={refCount}");
                    }
                    return _pointer;
                }
            }
        }

        /// <summary>
        /// Decrement the reference count on this object, and free it when it reaches zero.
        /// </summary>
        /// <returns>The new reference count.</returns>
        public int free()
        {
            lock (this)
            {
                if (--refCount < 1)
                {
                    if (refCount < 0)
                    {
                        throw new Exception($"BaseMemoryChunk.free: refCount={refCount}");
                    }
                    if (_pointer != IntPtr.Zero)
                    {
                        refCount = 1;   // Temporarily increase refcount so onfree() can use pointer.
                        onfree();
                        refCount = 0;
                        _pointer = IntPtr.Zero;
                        BaseMemoryChunkReferences.DeleteReference(GetType());
                    }
                }
                return refCount;
            }
        }

        /// <summary>
        /// Method called when the underlying native memory object should be freed. Must be implemented by subclasses.
        /// </summary>
        protected abstract void onfree();
    }
}