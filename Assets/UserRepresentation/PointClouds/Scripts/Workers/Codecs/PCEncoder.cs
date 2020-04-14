using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class PCEncoder : BaseWorker {
        cwipc.encoder encoder;
        System.IntPtr encoderBuffer;
        cwipc.pointcloud pointCloudData;
        int dampedSize = 0;

        public PCEncoder(Config._User._PCSelfConfig._Encoder cfg):base(WorkerType.Run) {
            try {
                cwipc.encoder_params parms = new cwipc.encoder_params { octree_bits = cfg.octreeBits, do_inter_frame = false, exp_factor = 0, gop_size = 1, jpeg_quality = 75, macroblock_size = 0, tilenumber = 0, voxelsize = 0 };
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
            if (token != null) {
                lock (token) {
                    if (token.currentPointcloud != null) {
                        encoder.feed(token.currentPointcloud);
                        token.currentPointcloud.free();
                        token.currentPointcloud = null;
                    }
                    if (encoder.available(true)) {
                        unsafe {
                            int size = encoder.get_encoded_size();
                            if (dampedSize < size) {
                                dampedSize = (int)(size * Config.Instance.memoryDamping);
                                if (encoderBuffer != System.IntPtr.Zero) System.Runtime.InteropServices.Marshal.FreeHGlobal(encoderBuffer);
                                encoderBuffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(dampedSize);
                            }
                            if (encoder.copy_data(encoderBuffer, dampedSize)) {
                                token.currentBuffer = encoderBuffer;
                                token.currentSize = size;
                                Next();
                            } else
                                Debug.LogError("PCRealSense2Reader: cwipc_encoder_copy_data returned false");
                        }
                    } else {
                        Debug.Log("NO FRAME!!!! Frame available");
                    }
                }
            }
        }
    }
}