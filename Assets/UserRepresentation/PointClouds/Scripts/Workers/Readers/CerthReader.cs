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

    public class CerthReader : BaseWorker   // Doesn't have to be a BaseWorker, but EntityPipeline expects it.
    {
        float voxelSize;
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

        public CerthReader(Config._User._PCSelfConfig cfg, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue)
        {
            constructorLock = new object();
            outQueue = _outQueue;
            out2Queue = _out2Queue;
            voxelSize = cfg.voxelSize;

            // Tell Certh library how many pc constructors we want. pcl_id must be < this.
            // Locking here only for completeness (no-one else can have a reference yet)
            lock (constructorLock)
            {
                native_pointcloud_receiver_pinvoke.set_number_wrappers(numWrappers);
            }

            if (cfg.CerthReaderConfig == null)
            {
                Debug.LogError("CerthReader: CerthReaderConfig is null");
                return;
            }

            PCLRabbitMQReceiver = new MyRabbitMQReceiver(cfg.CerthReaderConfig.ConnectionURI, cfg.CerthReaderConfig.PCLExchangeName);
            if (PCLRabbitMQReceiver == null)
            {
                Debug.LogError("CerthReader: PCLRabbitMQReceiver is null");
                return;
            }

            MetaRabbitMQReceiver = new MyRabbitMQReceiver(cfg.CerthReaderConfig.ConnectionURI, cfg.CerthReaderConfig.MetaExchangeName);
            if (MetaRabbitMQReceiver == null)
            {
                Debug.LogError("CerthReader: MetaRabbitMQReceiver is null");
                return;
            }

            Debug.Log($"PCCertReader: receiving PCs from {cfg.CerthReaderConfig.ConnectionURI}");
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
                Debug.Log($"xxxjack received metadata");
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
                            Debug.LogError("PCCerthReader: metadata received, but native_pointcloud_receiver_pinvoke.received_metadata() returned false");
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
            Debug.Log("xxxjack OnNewPCLData");
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
                    float[] bbox = { -1f, 1f, -1f, 0.5f, -3f, 1f };
                    System.UInt64 timestamp = 0;
                    cwipc.pointcloud pc = cwipc.from_certh(pclPtr, bbox, timestamp);
                    //
                    // We can now safely free the RGBD data
                    //
                    rgbdHandle.Free();
                    //
                    // Push the cwipc pointcloud to the consumers
                    //
                    if (pc == null)
                    {
                        Debug.LogWarning("CerthReader: cwipc.from_certh did not produce a pointcloud");
                        return;
                    }
                    pc.AddRef(); // xxxjack
                    statsUpdate(pc.count());

                    if (outQueue != null && outQueue.Count < 2)
                    {
                        outQueue.Enqueue(pc.AddRef());
                    }
                    else
                    {
                        pc.free();
                    }
                    if (out2Queue != null && out2Queue.Count < 2)
                    {
                        out2Queue.Enqueue(pc.AddRef());
                    }
                    else
                    {
                        pc.free();
                    }
                    // xxxjack add when initial ref to pc is counted: pc.free();

                }
            }
            catch (System.Exception exc)
            {
                Debug.LogError($"PCCerthReader: OnNewPCLData: caught exception: {exc.Message}");
                throw exc;
            }
            Debug.Log("xxxjack OnNewPCLData returning");


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
