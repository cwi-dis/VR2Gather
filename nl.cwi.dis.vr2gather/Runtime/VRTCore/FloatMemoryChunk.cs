using System;
using System.Runtime.InteropServices;
using Cwipc;

namespace VRT.Core
{
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;
    public class FloatMemoryChunk : NativeMemoryChunk
    {
        public int elements { get; private set; }
        public float[] buffer;
        GCHandle handle;

        public FloatMemoryChunk(int _elements) : base()
        {
            buffer = new float[_elements];
            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _pointer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
            elements = _elements;
            length = _elements * 4;
            metadata = new FrameMetadata();
        }

        protected override void onfree()
        {
            _pointer = IntPtr.Zero;
            handle.Free();
            buffer = null;
        }
    }
}