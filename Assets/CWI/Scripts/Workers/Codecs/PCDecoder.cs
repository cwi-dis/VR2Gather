using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class PCDecoder : BaseWorker {
        cwipc.decoder decoder;
        cwipc.pointcloud pointCloudData;
        
        public PCDecoder():base(WorkerType.Run) {
            try {
                // xxxjack this seems sillly here: signals_unity_bridge_pinvoke.SetPaths("cwipc_codec");
                decoder = cwipc.new_decoder(); 
                if (decoder == null)
                    throw new System.Exception("PCSUBReader: cwipc_new_decoder creation failed"); // Should not happen, should throw exception
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
            // xxxjack removed if (pointCloudData != System.IntPtr.Zero) API_cwipc_util.cwipc_source_free(decoder);
        }

        protected override void Update(){
            base.Update();
            if (token != null) {
                // xxxjack removed if(pointCloudData!= System.IntPtr.Zero) API_cwipc_util.cwipc_source_free(decoder);
                decoder.feed(token.currentBuffer, token.currentSize);
                // xxxjack wonders whether this shouldn't be decoder.available(false) which doesn't block...
                if ( decoder.available(true) ) {
                    pointCloudData = decoder.get();
                    if (pointCloudData != null)
                    {
                        token.currentPointcloud = pointCloudData;
                        Next();
                    } else {
                        Debug.LogError("PCSUBReader: cwipc_decoder: available() true, but did not return a pointcloud");
                    }

                }
                else
                    Debug.LogError($"PCSUBReader: cwipc_decoder: no pointcloud available currentSize {token.currentSize}");
            }
        }
    }
}