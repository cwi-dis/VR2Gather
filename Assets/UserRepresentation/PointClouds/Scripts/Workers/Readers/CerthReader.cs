using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Utils;

namespace Workers {
    public class MyRabbitMQReceiver : RabbitMQReceiver
    {
        public MyRabbitMQReceiver(string uri, string exchange)
        {
            ConnectionProperties.ConnectionURI = uri;
            ConnectionProperties.ExchangeName = exchange;
        }
    }

    public class CerthReader : TiledWorker   // Doesn't have to be a BaseWorker, but EntityPipeline expects it.
    {
        float voxelSize;
        Vector3 originCorrection;
        Vector3 boundingBotLeft;
        Vector3 boundingTopRight;
        QueueThreadSafe outQueue;
        QueueThreadSafe out2Queue;

        bool metaDataReceived = false;          // Set to true once metadata has been received
        GCHandle metaDataHandle;                // Set to unmanaged memory handle where metadata has been stored.
        cwipc.pointcloud mostRecentPc;          // Stores the most recently received pointcloud (if any)
        const int pcl_id = 0;                   // Index of Cert pc constructor (constant for now)
        const int numWrappers = 1;              // Total number of pc constructors (constant for now)
        object constructorLock;                 // Lock around PC constructor and its (static) return value

        private RabbitMQReceiver PCLRabbitMQReceiver;
        private RabbitMQReceiver MetaRabbitMQReceiver;

        public CerthReader(string _ConnectionURI, string _PCLExchangeName, string _MetaExchangeName, Vector3 _originCorrection, Vector3 _boundingBotLeft, Vector3 _boundingTopRight, float _voxelSize, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue=null)
        {
            if (_outQueue == null)
            {
                throw new System.Exception("CerthReader: outQueue is null");
            }
            constructorLock = new object();
            outQueue = _outQueue;
            out2Queue = _out2Queue;
            voxelSize = _voxelSize;
            originCorrection = _originCorrection;
            boundingBotLeft = _boundingBotLeft;
            boundingTopRight = _boundingBotLeft;

            // Tell Certh library how many pc constructors we want. pcl_id must be < this.
            // Locking here only for completeness (no-one else can have a reference yet)
            lock (constructorLock) {
                native_pointcloud_receiver_pinvoke.set_number_wrappers(numWrappers);
            }

            PCLRabbitMQReceiver = new MyRabbitMQReceiver(_ConnectionURI, _PCLExchangeName);
            if (PCLRabbitMQReceiver == null)
            {
                throw new System.Exception("CerthReader: PCLRabbitMQReceiver is null");
            }

            MetaRabbitMQReceiver = new MyRabbitMQReceiver(_ConnectionURI, _MetaExchangeName);
            if (MetaRabbitMQReceiver == null)
            {

                throw new System.Exception("CerthReader: MetaRabbitMQReceiver is null");
            }

            Debug.Log($"PCCertReader: receiving PCs from {_ConnectionURI}");
            PCLRabbitMQReceiver.OnDataReceived += OnNewPCLData;
            PCLRabbitMQReceiver.Enabled = true;

            MetaRabbitMQReceiver.OnDataReceived += OnNewMetaData;
            MetaRabbitMQReceiver.Enabled = true;
        }

        public override void StopAndWait() {
            Stop();
        }

        public override void Stop() {
            Debug.Log("PCCerthReader: Stopping...");
            base.Stop();
            PCLRabbitMQReceiver.Enabled = false;
            MetaRabbitMQReceiver.Enabled = false;
            PCLRabbitMQReceiver.OnDataReceived -= OnNewPCLData;
            MetaRabbitMQReceiver.OnDataReceived -= OnNewMetaData;

            Debug.Log("PCCerthReader: Stopped.");
        }

        // Informing that the metadata were received
        private void OnNewMetaData(object sender, EventArgs<byte[]> e) {
            try
            {
                lock (constructorLock)
                {
                    if (e.Value != null && !metaDataReceived)
                    {
                        var buffer = e.Value; // Buffer 's data
                        metaDataHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned); // GCHandler for the buffer
                        var pnt = metaDataHandle.AddrOfPinnedObject(); // Buffer 's address
                        metaDataReceived = native_pointcloud_receiver_pinvoke.received_metadata(pnt, buffer.Length, pcl_id);
                        if (!metaDataReceived)
                        {
                            throw new System.Exception("PCCerthReader: metadata received, but native_pointcloud_receiver_pinvoke.received_metadata() returned false");
                        }
                    }
                }
            } catch (System.Exception exc)
            {
                Debug.LogError($"PCCerthReader: OnNewMetaData: caught exception: {exc.Message}");
                throw exc;
            }
        }

        // Updating the pointcloud every time a new buffer is received from the network
        private void OnNewPCLData(object sender, EventArgs<byte[]> e) {
            try {
                lock (constructorLock) {
                    if (e.Value == null) {
                        Debug.LogWarning("CerthReader: OnNewPCLData: received null data");
                        return;
                    }

                    if (!metaDataReceived) {
                        Debug.Log("CerthReader: OnNewPCLData: received data, but no metadata yet");
                        return;

                    }
                    //
                    // Get the RGBD data from the rabbitMQ message
                    //
                    var buffer = e.Value; // Buffer 's data
                    GCHandle rgbdHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned); // GCHandler for the buffer
                    System.IntPtr rgbdPtr = rgbdHandle.AddrOfPinnedObject(); // Buffer 's address
                    //
                    // Convert the RGBD data to a Certh-style pointcloud
                    //
                    System.IntPtr pclPtr = native_pointcloud_receiver_pinvoke.callColorizedPCloudFrameDLL(rgbdPtr, buffer.Length, pcl_id); // Pointer of the returned structure
                    if (pclPtr == System.IntPtr.Zero)
                    {
                        Debug.LogWarning("CerthReader: callColorizedPCloudFrameDLL returned NULL");
                        return;
                    }
                    //
                    // Convert the Certh pointcloud to a cwipc pointcloud
                    //
                    float[] bbox = null;
                    float[] move = null;
                    if (boundingBotLeft.x != boundingTopRight.x || boundingBotLeft.y != boundingTopRight.y || boundingBotLeft.z != boundingTopRight.z)
                    {
                        bbox = new float[6]
                        {
                            boundingBotLeft.x, boundingTopRight.x,
                            boundingBotLeft.y, boundingTopRight.y,
                            boundingBotLeft.z, boundingTopRight.z
                        };
                    }
                    if (originCorrection.x != 0 || originCorrection.y != 0 || originCorrection.z != 0)
                    {
                        move = new float[3] { originCorrection.x, originCorrection.y, originCorrection.z };
                    }
                    System.UInt64 timestamp = 0;
                    cwipc.pointcloud pc = cwipc.from_certh(pclPtr, move, bbox, timestamp);
                    if (voxelSize != 0)
                    {
                        var newPc = cwipc.downsample(pc, voxelSize);
                        if (newPc == null)
                        {
                            Debug.LogWarning($"{Name()}: Voxelating pointcloud with {voxelSize} got rid of all points?");
                        }
                        else
                        {
                            pc.free();
                            pc = newPc;
                        }
                    }
                    //
                    // We can now safely free the RGBD data
                    //
                    rgbdHandle.Free();
                    //
                    // Push the cwipc pointcloud to the consumers
                    //
                    statsUpdate(pc.count());
                    if (pc == null)
                    {
                        Debug.LogWarning("CerthReader: cwipc.from_certh did not produce a pointcloud");
                        return;
                    }
                    if (outQueue == null)
                    {
                        Debug.LogError($"CerthReader: no outQueue, dropping pointcloud");
                    }
                    else
                    {
                        if (outQueue._CanEnqueue())
                        {
                            outQueue.Enqueue(pc.AddRef());
                        }
                        else
                        {
                            Debug.Log($"CerthReader: outQueue full, dropping pointcloud");
                        }
                    }
                    if (out2Queue == null)
                    {
                        // This is not an error. Debug.LogError($"RS2Reader: no outQueue2, dropping pointcloud");
                    }
                    else
                    {
                        if (out2Queue._CanEnqueue())
                        {
                            out2Queue.Enqueue(pc.AddRef());
                        }
                        else
                        {
                            Debug.Log($"CerthReader: outQueue2 full, dropping pointcloud");
                        }
                    }

                    pc.free();

                }
            }
            catch (System.Exception exc)
            {
                Debug.LogError($"PCCerthReader: OnNewPCLData: caught exception: {exc.Message}");
                throw exc;
            }


        }
        System.DateTime statsLastTime;
        double statsTotalPoints;
        double statsTotalPointclouds;

        public void statsUpdate(int pointCount)
        {
            if (statsLastTime == null)
            {
                statsLastTime = System.DateTime.Now;
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(10))
            {
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: CerthReader: {statsTotalPointclouds / 10} fps, {(int)(statsTotalPoints / statsTotalPointclouds)} points per cloud");
                statsTotalPoints = 0;
                statsTotalPointclouds = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalPoints += pointCount;
            statsTotalPointclouds += 1;
        }
    }
}
