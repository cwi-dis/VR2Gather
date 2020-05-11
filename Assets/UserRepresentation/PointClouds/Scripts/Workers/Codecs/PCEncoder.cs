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
        public struct EncoderStreamDescription
        {
            public int octreeBits;
            public int tileNumber;
            public QueueThreadSafe outQueue;
        };
        EncoderStreamDescription[] outputs;

        public PCEncoder(QueueThreadSafe _inQueue, EncoderStreamDescription[] _outputs ) :base(WorkerType.Run) {
            if (_inQueue == null)
            {
                throw new System.Exception("PCEncoder: inQueue is null");
            }
            if (_outputs.Length != 1)
            {
                throw new System.Exception("PCEncoder: outputs length must be 1");
            }
            inQueue = _inQueue;
            outputs = _outputs;
            try {
                cwipc.encoder_params parms = new cwipc.encoder_params { 
                    octree_bits = outputs[0].octreeBits, 
                    do_inter_frame = false, 
                    exp_factor = 0, 
                    gop_size = 1, 
                    jpeg_quality = 75, 
                    macroblock_size = 0, 
                    tilenumber = outputs[0].tileNumber, 
                    voxelsize = 0 
                };
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
                // xxxjack next bit of code should go to per-stream handler
                int stream_number = 0;
                QueueThreadSafe outQueue = outputs[stream_number].outQueue;
                if (encoder.available(true)) {
                    unsafe {
                        NativeMemoryChunk mc = new NativeMemoryChunk( encoder.get_encoded_size() );
                        if (encoder.copy_data(mc.pointer, mc.length))
                            if (outQueue.Free())
                                outQueue.Enqueue(mc);
                            else
                                mc.free();
                        else
                            Debug.LogError("PCEncoder: cwipc_encoder_copy_data returned false");
                    }
                } else {
                    Debug.Log("PCEncoder: available(true) after feed() returned false");
                }
            }
        }
    }
}