using OrchestratorWrapping;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Workers
{
    public class SocketIOWriter : BaseWorker
    {
        QueueThreadSafe inQueue;


        public SocketIOWriter(string _userID, QueueThreadSafe _inQueue) : base(WorkerType.End) {
            OrchestratorWrapper.instance.DeclareDataStream("AUDIO");
            if (_inQueue == null) {
                throw new System.Exception($"{Name()}: outQueue is null");
            }
            inQueue = _inQueue;
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
            inQueue?.Close();
            Debug.Log($"{Name()}: Stopped.");
            OrchestratorWrapper.instance.RemoveDataStream("AUDIO");
        }

        protected override void Update() {
            base.Update();
            if (OrchestratorWrapper.instance!=null) {
                BaseMemoryChunk chk = inQueue.Dequeue();
                var buf = new byte[chk.length];
                // Debug.Log($"SocketOIWriter {chk.length}");

                System.Runtime.InteropServices.Marshal.Copy(chk.pointer, buf, 0, chk.length);
                OrchestratorWrapper.instance.SendData("AUDIO", buf);
                chk.free();
            }
        }

    }
}
