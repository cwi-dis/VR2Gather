using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class BufferPreparer : BaseWorker
    {
        bool isReady = false;
        Unity.Collections.NativeArray<byte> byteArray;
        System.IntPtr                       currentBuffer;
        int                                 currentSize;
        public BufferPreparer():base(WorkerType.End) {
            Start();
        }

        public override void OnStop() {
            base.OnStop();
            if (byteArray.Length != 0) byteArray.Dispose();
        }

        protected override void Update() {
            base.Update();
            if (token != null && !isReady) {
                unsafe {
                    currentSize = (int)API_cwipc_util.cwipc_get_uncompressed_size(token.currentBuffer);
                    if (currentSize > 0) {
                        if (currentSize > byteArray.Length) {
                            if (byteArray.Length != 0) byteArray.Dispose();
                            byteArray = new Unity.Collections.NativeArray<byte>(currentSize, Unity.Collections.Allocator.TempJob);
                            currentBuffer = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(byteArray);
                        }
                        API_cwipc_util.cwipc_copy_uncompressed(token.currentBuffer, currentBuffer, (System.IntPtr)currentSize);
                        isReady = true;
                        Next();
                    }
                }
            }
        }

        public int GetComputeBuffer(ref ComputeBuffer computeBuffer) {
            int size = currentSize / 16;
            if (isReady && size != 0) {
                unsafe {
                    int dampedSize = (int)(size * Config.Instance.memoryDamping);
                    if (computeBuffer == null || computeBuffer.count < dampedSize) {
                        if (computeBuffer != null) computeBuffer.Release();
                        computeBuffer = new ComputeBuffer(dampedSize, sizeof(float) * 4);
                    }
                    computeBuffer.SetData<byte>(byteArray, 0, 0, byteArray.Length);
                }
                isReady = false;
            }
            return size;
        }

    }
}
