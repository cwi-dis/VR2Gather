using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Workers {
    public class B2DWriter : BaseWorker {
        System.IntPtr uploader;
        BinaryWriter bw;

        public B2DWriter(Config._User._PCSelfConfig._Bin2Dash cfg) : base(WorkerType.End) {
            try {
                if ( cfg.fileMirroring ) bw = new BinaryWriter(new FileStream( $"{Application.dataPath}/../{cfg.streamName}.dot", FileMode.Create));
                signals_unity_bridge_pinvoke.SetPaths("bin2dash.so");
                uploader = bin2dash_pinvoke.vrt_create(cfg.streamName, bin2dash_pinvoke.VRT_4CC('c', 'w', 'i', '1'), cfg.url, cfg.segmentSize, cfg.segmentLife);
                if (uploader != System.IntPtr.Zero)
                {
                    Debug.Log($"Bin2Dash {cfg.url}");
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
            bw?.Close();
            base.OnStop();
            Debug.Log("B2DWriter Sopped");
        }

        protected override void Update() {
            base.Update();
            if (token != null) {
                bw?.Write(token.currentByteArray, 0, token.currentSize);
                if (!bin2dash_pinvoke.vrt_push_buffer(uploader, token.currentBuffer, (uint)token.currentSize))
                    Debug.Log("ERROR sending data");
                Next();
            }
        }
    }
}
