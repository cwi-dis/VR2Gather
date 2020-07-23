using OrchestratorWrapping;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Workers
{
    public class SocketIOReader : BaseWorker, ISocketReader
    {
        Workers.PCSubReader.TileDescriptor[] descriptors;

        public SocketIOReader(string remoteURL, string remoteStream, Workers.PCSubReader.TileDescriptor[] descriptors) : base(WorkerType.End) {
            if (descriptors == null) {
                throw new System.Exception($"{Name()}: descriptors is null");
            }
            this.descriptors = descriptors;
            this.descriptors[0].name = remoteURL + remoteStream + "_0";

            try {
                Debug.Log($"{Name()}: Started {this.descriptors[0].name}.");

                OrchestratorWrapper.instance.RegisterForDataStream( OrchestratorController.Instance.SelfUser.userId, this.descriptors[0].name);
                OrchestratorWrapper.instance.OnDataStreamReceived += OnAudioPacketReceived;

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
            this.descriptors[0].outQueue?.Close();
            Debug.Log($"{Name()}: Stopped.");
            OrchestratorWrapper.instance.UnregisterFromDataStream(OrchestratorController.Instance.SelfUser.userId, this.descriptors[0].name);
        }
        private void OnAudioPacketReceived(UserDataStreamPacket pPacket) {
            //if (pPacket.dataStreamUserID == userID) 
            {
                BaseMemoryChunk chunk = new NativeMemoryChunk(pPacket.dataStreamPacket.Length);
                System.Runtime.InteropServices.Marshal.Copy(pPacket.dataStreamPacket, 0, chunk.pointer, chunk.length);
                this.descriptors[0].outQueue.Enqueue(chunk);
                OnData(pPacket.dataStreamPacket);
            }
        }

        public void OnData(byte[] data) {
        }

        protected override void Update() {
            base.Update();
        }
    }

}
