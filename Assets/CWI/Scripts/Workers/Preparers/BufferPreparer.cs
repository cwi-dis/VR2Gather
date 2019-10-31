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
        float                               currentCellSize = 0.008f;
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
                lock (token) {
                    unsafe {
                        currentSize = token.currentPointcloud.get_uncompressed_size();
                        currentCellSize = token.currentPointcloud.cellsize();
                        // xxxjack if currentCellsize is != 0 it is the size at which the points should be displayed
                        if (currentSize > 0) {
                            if (currentSize > byteArray.Length) {
                                if (byteArray.Length != 0) byteArray.Dispose();
                                byteArray = new Unity.Collections.NativeArray<byte>(currentSize, Unity.Collections.Allocator.TempJob);
                                currentBuffer = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(byteArray);
                            }
                            int ret = token.currentPointcloud.copy_uncompressed(currentBuffer, currentSize);
                            if (ret * 16 != currentSize) {
                                Debug.LogError($"BufferPreparer decompress size problem: currentSize={currentSize}, copySize={ret * 16}, #points={ret}");
                            }
                            isReady = true;
                            Next();
                        }
                    }
                }
            }
        }

        public int GetComputeBuffer(ref ComputeBuffer computeBuffer) {
            // xxxjack I don't understand this computation of size, the sizeof(float)*4 below and the byteArray.Length below that.
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

        public float SetPointSize() {
            if (currentCellSize > 0.0000f) return currentCellSize;
            else return 0.008f;
        }

    }
}
