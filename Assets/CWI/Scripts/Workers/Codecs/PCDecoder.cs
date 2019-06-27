using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class PCDecoder : BaseWorker {
        System.IntPtr decoder;
        System.IntPtr pointCloudData;
        
        public PCDecoder():base(WorkerType.Run) {
            try {
                signals_unity_bridge_pinvoke.SetPaths("cwipc_codec");
                System.IntPtr errorPtr = System.IntPtr.Zero;
                decoder = API_cwipc_codec.cwipc_new_decoder(ref errorPtr);
                if (decoder == System.IntPtr.Zero)
                    throw new System.Exception($"PCSUBReader: cwipc_new_decoder: {System.Runtime.InteropServices.Marshal.PtrToStringAnsi(errorPtr)}");
                else
                    Start();

            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
            
        }

        public override void OnStop() {
            base.OnStop();
            if (pointCloudData != System.IntPtr.Zero) API_cwipc_util.cwipc_source_free(decoder);
        }

        protected override void Update(){
            base.Update();
            if (token != null) {
                if(pointCloudData!= System.IntPtr.Zero) API_cwipc_util.cwipc_source_free(decoder);
                API_cwipc_codec.cwipc_decoder_feed(decoder, token.currentBuffer, token.currentSize);
                if ( API_cwipc_util.cwipc_source_available(decoder, true) ) {
                    pointCloudData = API_cwipc_util.cwipc_source_get(decoder);
                    if (pointCloudData != System.IntPtr.Zero) {
                        token.currentBuffer = pointCloudData;
                        Next();
                    }
                    else
                        Debug.LogError("PCSUBReader: cwipc_decoder: did not return a pointcloud");

                }
                else
                    Debug.LogError($"PCSUBReader: cwipc_decoder: no pointcloud available currentSize {token.currentSize}");
            }
        }
    }
}