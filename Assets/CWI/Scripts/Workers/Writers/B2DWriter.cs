using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class B2DWriter : BaseWorker {
        System.IntPtr uploader;

        public B2DWriter(Config._PCs._Bin2Dash cfg) : base(WorkerType.End) {
            try {
                signals_unity_bridge_pinvoke.SetPaths("bin2dash");
                uploader = bin2dash_pinvoke.vrt_create(cfg.streamName, bin2dash_pinvoke.VRT_4CC('c', 'w', 'i', '1'), cfg.url, cfg.segmentSize, cfg.segmentLife);
                if (uploader != System.IntPtr.Zero)
                {
                    Start();
                }
                else
                    throw new System.Exception($"PCRealSense2Reader: vrt_create: failed to create uploader {cfg.url}/{cfg.streamName}.mpd");
            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public override void OnStop() {
            if (uploader != System.IntPtr.Zero) { bin2dash_pinvoke.vrt_destroy(uploader); uploader = System.IntPtr.Zero; }
            base.OnStop();
            Debug.Log("B2DWriter Sopped");
        }

        protected override void Update() {
            base.Update();
            if (token != null) {
                if (!bin2dash_pinvoke.vrt_push_buffer(uploader, token.currentBuffer, (uint)token.currentSize))
                    Debug.Log("ERROR sending data");
                Next();
            }
        }
    }
}
