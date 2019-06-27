using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class PCEncoder : BaseWorker {
        System.IntPtr encoder;
        System.IntPtr encoderBuffer;
        System.IntPtr pointCloudData;
        int dampedSize = 0;

        public PCEncoder(Config._PCs._Encoder cfg):base(WorkerType.Run) {
            try {
                API_cwipc_codec.cwipc_encoder_params parms = new API_cwipc_codec.cwipc_encoder_params { octree_bits = cfg.octreeBits, do_inter_frame = false, exp_factor = 0, gop_size = 1, jpeg_quality = 75, macroblock_size = 0, tilenumber = 0, voxelsize = 0 };
                signals_unity_bridge_pinvoke.SetPaths("cwipc_codec");
                System.IntPtr errorPtr = System.IntPtr.Zero;
                encoder = API_cwipc_codec.cwipc_new_encoder(API_cwipc_codec.CWIPC_ENCODER_PARAM_VERSION, ref parms, ref errorPtr);
                if (encoder != System.IntPtr.Zero) {
                    Start();
                }
                else
                    throw new System.Exception($"PCRealSense2Reader: cwipc_new_encoder: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)}");

            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public override void OnStop() {
            base.OnStop();
            if (encoder != System.IntPtr.Zero) { API_cwipc_codec.cwipc_encoder_free(encoder); encoder = System.IntPtr.Zero; }
            if (encoderBuffer != System.IntPtr.Zero) { System.Runtime.InteropServices.Marshal.FreeHGlobal(encoderBuffer); encoderBuffer = System.IntPtr.Zero; }
        }

        protected override void Update() {
            base.Update();
            if (token != null) {
                API_cwipc_codec.cwipc_encoder_feed(encoder, token.currentBuffer);
                if (API_cwipc_codec.cwipc_encoder_available(encoder, true)) {
                    unsafe {
                        int size = (int)API_cwipc_codec.cwipc_encoder_get_encoded_size(encoder);
                        if (dampedSize < size) {
                            dampedSize = (int)(size * Config.Instance.memoryDamping);
                            if (encoderBuffer != System.IntPtr.Zero) System.Runtime.InteropServices.Marshal.FreeHGlobal(encoderBuffer);
                            encoderBuffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(dampedSize);
                        }
                        if (API_cwipc_codec.cwipc_encoder_copy_data(encoder, encoderBuffer, (System.IntPtr)dampedSize)) {
                            token.currentBuffer = encoderBuffer;
                            token.currentSize = size;
                            Next();
                        }
                        else
                            Debug.LogError("PCRealSense2Reader: cwipc_encoder_copy_data returned false");
                    }
                }
            }
        }
    }
}