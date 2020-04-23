using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using PCLDataProviders;
using Utils;

namespace Workers {

    [RequireComponent(typeof(MetaDataProvider))]
    [RequireComponent(typeof(PCLDataProvider))]
    public class CerthReader : BaseWorker // xxxjack also needs MonoBehaviour?
    {
        float voxelSize;
        QueueThreadSafe outPLCQueue;
        QueueThreadSafe outMetaQueue;

        private PCLIdataProvider dataProvider;  // Connection to RabbitMQ over which normal RGBD data comes in.
        private PCLIdataProvider metaDataProvider;  // Connection to RabbitMQ over which metadata comes in.
        bool metaDataReceived = false;          // Set to true once metadata has been received
        GCHandle metaDataHandle;                // Set to unmanaged memory handle where metadata has been stored.
        cwipc.pointcloud mostRecentPc;          // Stores the most recently received pointcloud (if any)

        private RabbitMQReceiver PLCRabbitMQReceiver = new RabbitMQReceiver();
        private RabbitMQReceiver MetaRabbitMQReceiver = new RabbitMQReceiver();

        public CerthReader(Config._User._PCSelfConfig cfg, QueueThreadSafe _outPLCQueue, QueueThreadSafe _outMetaQueue ) : base(WorkerType.Init) {
            outPLCQueue  = _outPLCQueue;
            outMetaQueue = _outMetaQueue;
            voxelSize = cfg.voxelSize;

            PLCRabbitMQReceiver.OnDataReceived += OnNewPCLData;
            PLCRabbitMQReceiver.ConnectionProperties.ConnectionURI = cfg.TVMs.PCLConnectionURI;
            PLCRabbitMQReceiver.ConnectionProperties.ExchangeName = cfg.TVMs.PCLExchangeName;
            PLCRabbitMQReceiver.Enabled = true;

            MetaRabbitMQReceiver.OnDataReceived += OnNewMetaData;
            MetaRabbitMQReceiver.ConnectionProperties.ConnectionURI = cfg.TVMs.MetaConnectionURI;
            MetaRabbitMQReceiver.ConnectionProperties.ExchangeName = cfg.TVMs.MetaExchangeName;
            MetaRabbitMQReceiver.Enabled = true;
        }

        public override void OnStop() {
            base.OnStop();
            PLCRabbitMQReceiver.OnDataReceived -= OnNewPCLData;
            PLCRabbitMQReceiver.Enabled = false;
            MetaRabbitMQReceiver.OnDataReceived -= OnNewMetaData;
            MetaRabbitMQReceiver.Enabled = false;

            Debug.Log("PCCerthReader: Stopped.");
        }

        protected override void Update() {
            base.Update();
        }

        // Informing that the metadata were received
        private void OnNewMetaData(object sender, EventArgs<byte[]> e) {
            lock (e) {
                if (e.Value != null && !metaDataReceived) {
                    var buffer = e.Value; // Buffer 's data
                    metaDataHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned); // GCHandler for the buffer
                    var pnt = metaDataHandle.AddrOfPinnedObject(); // Buffer 's address
                    metaDataReceived = native_pointcloud_receiver_pinvoke.received_metadata(pnt, buffer.Length, pcl_id);
                }
            }
        }

        // Updating the pointcloud every time a new buffer is received from the network
        private void OnNewPCLData(object sender, EventArgs<byte[]> e) {
            lock (e) {
                if (e.Value == null) {
                    Debug.LogWarning("CerthReader: OnNewPCLData: received null data");
                    return;
                }

                if (!metaDataReceived) {
                    Debug.Log("CerthReader: OnNewPCLData: received data, but no metadata yet");
                    return;

                }
                // Flaging that a new buffer is received
                var buffer = e.Value; // Buffer 's data
                GCHandle rgbdHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned); // GCHandler for the buffer
                System.IntPtr rgbdPtr = rgbdHandle.AddrOfPinnedObject(); // Buffer 's address
                System.IntPtr pclPtr = native_pointcloud_receiver_pinvoke.callColorizedPCloudFrameDLL(rgbdPtr, buffer.Length, pcl_id); // Pointer of the returned structure
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
