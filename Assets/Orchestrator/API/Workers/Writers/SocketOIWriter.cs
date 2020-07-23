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
            streams[0].name = remoteURL + remoteStream + "_0";
            OrchestratorWrapper.instance.DeclareDataStream(streams[0].name);
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
            if (!streams[0].inQueue.IsClosed())
            {
                Debug.LogWarning($"{Name()}: inQueue not closed, closing");
                streams[0].inQueue.Close();
            }
            Debug.Log($"{Name()}: Stopped.");
            OrchestratorWrapper.instance.RemoveDataStream("AUDIO");
        }

        protected override void Update() {
            base.Update();
            if (OrchestratorWrapper.instance!=null) {
                BaseMemoryChunk chk = streams[0].inQueue.Dequeue();
                if (chk == null) return;
                var buf = new byte[chk.length];
                // Debug.Log($"SocketOIWriter {chk.length}");
                System.Runtime.InteropServices.Marshal.Copy(chk.pointer, buf, 0, chk.length);
                OrchestratorWrapper.instance.SendData(streams[0].name, buf);
                chk.free();
            }
        }

    }
}
