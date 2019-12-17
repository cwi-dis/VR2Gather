using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Workers {
    public class B2DWriter : BaseWorker {
        bin2dash.connection uploader;
        //BinaryWriter bw;
        string url;

        public B2DWriter(Config._User._PCSelfConfig._Bin2Dash cfg, string _url = "") : base(WorkerType.End) {
            try {
                //if (cfg.fileMirroring) bw = new BinaryWriter(new FileStream($"{Application.dataPath}/../{cfg.streamName}.dashdump", FileMode.Create));
                if (_url == string.Empty)
                    url = cfg.url;
                else
                    url = _url;
                uploader = bin2dash.create(cfg.streamName, bin2dash.VRT_4CC('c', 'w', 'i', '1'), url, cfg.segmentSize, cfg.segmentLife);
                if (uploader != null) {
                    Debug.Log($"Bin2Dash vrt_create(url={url + cfg.streamName}.mpd)");
                    Start();
                }
                else
                    throw new System.Exception($"B2DWriter: vrt_create: failed to create uploader {url + cfg.streamName}.mpd");
            }
            catch (System.Exception e) {
                Debug.LogError($"B2DWriter({url}:{e.Message}");
                throw e;
            }
        }

        public override void OnStop() {
            uploader.free();
            uploader = null;
            //bw?.Close();
            base.OnStop();
            Debug.Log($"B2DWriter {url} Stopped");
        }

        protected override void Update() {
            base.Update();
            if (token != null) {
                lock (token) {
                    //bw?.Write(token.currentByteArray, 0, token.currentSize);
                    if (!uploader.push_buffer(token.currentBuffer, (uint)token.currentSize))
                        Debug.Log("ERROR sending data");
                    Next();
                }
            }
        }
    }
}
