using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using PCLDataProviders;

namespace Workers {

    [RequireComponent(typeof(MetaDataProvider))]
    [RequireComponent(typeof(PCLDataProvider))]
    public class CerthReader : BaseWorker // xxxjack also needs MonoBehaviour?
    {
        float voxelSize;
        QueueThreadSafe outQueue;
        QueueThreadSafe out2Queue;
        private PCLIdataProvider dataProvider;  // Connection to RabbitMQ over which normal RGBD data comes in.
        private PCLIdataProvider metaDataProvider;  // Connection to RabbitMQ over which metadata comes in.
        bool metaDataReceived = false;          // Set to true once metadata has been received
        GCHandle metaDataHandle;                // Set to unmanaged memory handle where metadata has been stored.
        cwipc.pointcloud mostRecentPc;          // Stores the most recently received pointcloud (if any)

        public CerthReader(Config._User._PCSelfConfig cfg, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null) : base(WorkerType.Init)
        {
            outQueue = _outQueue;
            out2Queue = _out2Queue;
            voxelSize = cfg.voxelSize;

            // xxxjack need to pass in config.json parameters 
            dataProvider = GetComponent<PCLDataProvider>();
            dataProvider.OnNewPCLData += OnNewPCLData;

            // xxxjack need to pass in config.json parameters 
            metaDataProvider = GetComponent<MetaDataProvider>();
            metaDataProvider.OnNewMetaData += OnNewMetaData;
        }

        public override void OnStop()
        {
            base.OnStop();
            dataProvider.OnNewPCLData -= OnNewPCLData;
            metaDataProvider.OnNewMetaData -= OnNewMetaData;
            // xxxjack free dataProvider and metaDataProvider?
            Debug.Log("PCCerthReader: Stopped.");
        }

        protected override void Update()
        {
            base.Update();
            // Atomically get most recently received pointcloud
            cwipc.pointcloud pc;
            lock (this)
            {
                pc = mostRecentPc;
                mostRecentPc = null;
            }
            if (pc == null) return;
            if (voxelSize != 0)
            {
                var tmp = pc;
                pc = cwipc.downsample(tmp, voxelSize);
                tmp.free();
                if (pc == null) throw new System.Exception($"PCCerthReader: Voxelating pointcloud with {voxelSize} got rid of all points?");
            }
            pc.AddRef(); pc.AddRef();
            statsUpdate(pc.count());

            if (outQueue != null && outQueue.Count < 2)
                outQueue?.Enqueue(pc);
            else
                pc.free();

            if (out2Queue != null && out2Queue.Count < 2)
                out2Queue.Enqueue(pc);
            else
                pc.free();
        }

        // Informing that the metadata were received
        private void OnNewMetaData(object sender, EventArgs<byte[]> e)
        {
            lock (e)
            {
                if (e.Value != null && !metaDataReceived)
                {
                    var buffer = e.Value; // Buffer 's data
                    metaDataHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned); // GCHandler for the buffer
                    var pnt = metaDataHandle.AddrOfPinnedObject(); // Buffer 's address
                    metaDataReceived = native_pointcloud_receiver_pinvoke.received_metadata(pnt, buffer.Length, pcl_id);
                }
            }
        }

        // Updating the pointcloud every time a new buffer is received from the network
        private void OnNewPCLData(object sender, EventArgs<byte[]> e)
        {
            lock (e)
            {
                if (e.Value == null)
                {
                    Debug.LogWarning("CerthReader: OnNewPCLData: received null data");
                    return;
                }

                if (!metaDataReceived)
                {
                    Debug.Log("CerthReader: OnNewPCLData: received data, but no metadata yet");
                    return;

                }
                // Flaging that a new buffer is received
                var buffer = e.Value; // Buffer 's data
                GCHandle rgbdHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned); // GCHandler for the buffer
                IntPtr rgbdPtr = rgbdHandle.AddrOfPinnedObject(); // Buffer 's address
                IntPtr pclPtr = native_pointcloud_receiver_pinvoke.callColorizedPCloudFrameDLL(rgbdPtr, buffer.Length, pcl_id); // Pointer of the returned structure
                cwipc.pointcloud pc = cwipc.from_certh(pclPtr);
                // Atomically store it, for update to pick it up. Delete any previously stored pointcloud that hasn't been picked up yet.
                lock(this)
                {
                    cwipc.pointcloud prevPc = mostRecentPc;
                    mostRecentPc = pc;
                    if (prevPc) prevPc.free();
                }
                // Freeing the GCHandler
                rgbdHandle.Free();
            }
        }
    }
}
