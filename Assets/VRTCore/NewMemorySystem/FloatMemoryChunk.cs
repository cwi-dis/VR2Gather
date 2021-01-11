using System;

namespace VRTCore
{
    public class FloatMemoryChunk : NativeMemoryChunk
    {
        public int elements { get; private set; }
        public float[] buffer;

        public FloatMemoryChunk(int _elements) : base()
        {
            buffer = new float[_elements];
            _pointer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
            elements = _elements;
            length = _elements * 4;
        }

        protected override void onfree()
        {
            buffer = null;
        }
    }
}