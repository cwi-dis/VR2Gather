using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class PCEncoder : BaseWorker {
        cwipc.encoder encoder;
        System.IntPtr encoderBuffer;
        cwipc.pointcloud pointCloudData;
        int dampedSize = 0;
        QueueThreadSafe inQueue;
        QueueThreadSafe outQueue;

        public PCEncoder( int _octreeBits, QueueThreadSafe _inQueue, QueueThreadSafe _outQueue ) :base(WorkerType.Run) {
            inQueue = _inQueue;
            outQueue = _outQueue;
            try {
                cwipc.encoder_params parms = new cwipc.encoder_params { octree_bits = _octreeBits, do_inter_frame = false, exp_factor = 0, gop_size = 1, jpeg_quality = 75, macroblock_size = 0, tilenumber = 0, voxelsize = 0 };
                encoder = cwipc.new_encoder(parms);
                if (encoder != null) {
                    Start();
                    Debug.Log("PCEncoder Inited");

                } else
                {
                    Debug.LogError("PCEncoder: cloud not create cwipc_encoder"); // Should not happen, should thorw an exception
                }

            }
            catch (System.Exception e) {
                Debug.LogError($"Exception during call to PCEncoder constructor: {e.Message}");
                throw e;
            }
        }

        public override void OnStop() {
            base.OnStop();
            encoder?.free();
            encoder = null;
            Debug.Log("PCEncoder Stopped");
            if (encoderBuffer != System.IntPtr.Zero) { System.Runtime.InteropServices.Marshal.FreeHGlobal(encoderBuffer); encoderBuffer = System.IntPtr.Zero; }
        }

        protected override void Update() {
            base.Update();
            if (inQueue.Count>0) {
                cwipc.pointcloud pc = (cwipc.pointcloud)inQueue.Dequeue();
                encoder.feed(pc);
                pc.free();
                if (encoder.available(true)) {
                    unsafe {
                        NativeMemoryChunk mc = new NativeMemoryChunk( encoder.get_encoded_size() );
                        if (encoder.copy_data(mc.pointer, mc.length))
                            if (outQueue.Count < outQueue.Size)
                                outQueue.Enqueue(mc);
                            else
                                mc.free();
                        else
                            Debug.LogError("PCEncoder: cwipc_encoder_copy_data returned false");
                    }
                } else {
                    Debug.Log("NO FRAME!!!! Frame available");
                }
            }
        }
    }
}