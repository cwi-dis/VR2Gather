using System;
using Cwipc;

namespace Cwipc
{

    /// <summary>
    /// Implementation of BaseMemoryChunk using AllocHGlobal.
    /// </summary>
    public class NativeMemoryChunk : BaseMemoryChunk
    {
        public NativeMemoryChunk(int len) : base(AllocMemory(len))
        {
            length = len;
        }

        protected NativeMemoryChunk() : base()
        {
        }

        static IntPtr AllocMemory(int len)
        {
            return System.Runtime.InteropServices.Marshal.AllocHGlobal(len);
        }

        protected override void onfree()
        {
            if (pointer == IntPtr.Zero) throw new Exception("Calling NativeMemoryChunk.onfree with Zero pointer.");
            System.Runtime.InteropServices.Marshal.FreeHGlobal(pointer);
        }
    }
}