using OrchestratorWrapping;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Workers
{
    public class SocketIOWriter : BaseWorker
    {
        Workers.B2DWriter.DashStreamDescription[] streams;

        public SocketIOWriter(string remoteURL, string remoteStream, Workers.B2DWriter.DashStreamDescription[] streams) : base(WorkerType.End) {
            if (streams == null) {
                throw new System.Exception($"{Name()}: outQueue is null");
            }
            this.streams = streams;
            for (int i = 0; i < streams.Length; ++i) {
                streams[i].name = $"{remoteURL}{remoteStream}#{i}";
                OrchestratorWrapper.instance.DeclareDataStream(streams[i].name);
            }
            try {
                Start();
                Debug.Log($"{Name()}: Started.");
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public override string Name() {
            return $"{this.GetType().Name}";
        }


        public override void OnStop() {
            base.OnStop();
            for (int i = 0; i < streams.Length; ++i) {
                if (!streams[i].inQueue.IsClosed()) {
                    Debug.LogWarning($"{Name()}: inQueue not closed, closing");
                    streams[i].inQueue.Close();
                }
            }
            Debug.Log($"{Name()}: Stopped.");
            OrchestratorWrapper.instance.RemoveDataStream("AUDIO");
        }

        protected override void Update() {
            base.Update();
            if (OrchestratorWrapper.instance!=null) {
                for (int i = 0; i < streams.Length; ++i) {
                    BaseMemoryChunk chk = streams[i].inQueue.Dequeue();
                    if (chk == null) return;

                    var buf = new byte[chk.length];
                    System.Runtime.InteropServices.Marshal.Copy(chk.pointer, buf, 0, chk.length);
                    OrchestratorWrapper.instance.SendData(streams[i].name, buf);
                    chk.free();
                }
            }
        }

    }
}
