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
            try {
                for (int i = 0; i < this.descriptors.Length; ++i) {
                    this.descriptors[i].name = $"{remoteURL}{remoteStream}#{i}";
                    Debug.Log($"{Name()}: Started {this.descriptors[i].name}.");
                    OrchestratorWrapper.instance.RegisterForDataStream(OrchestratorController.Instance.SelfUser.userId, this.descriptors[i].name);
                }
                OrchestratorWrapper.instance.OnDataStreamReceived += OnDataPacketReceived;

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
            for (int i = 0; i < descriptors.Length; ++i) {
                descriptors[i].outQueue?.Close();
                Debug.Log($"{Name()}: Stopped.");
                OrchestratorWrapper.instance.UnregisterFromDataStream(OrchestratorController.Instance.SelfUser.userId, descriptors[i].name);
            }
        }
        private void OnDataPacketReceived(UserDataStreamPacket pPacket) {
            //if (pPacket.dataStreamUserID == userID) 
            {
                BaseMemoryChunk chunk = new NativeMemoryChunk(pPacket.dataStreamPacket.Length);
                System.Runtime.InteropServices.Marshal.Copy(pPacket.dataStreamPacket, 0, chunk.pointer, chunk.length);
                int id = 0;
                if( int.TryParse( pPacket.dataStreamType.Substring(pPacket.dataStreamType.LastIndexOf('#')+1), out id) )
                    descriptors[id].outQueue.Enqueue(chunk);
                // OnData(pPacket.dataStreamPacket);
            }
        }

        public void OnData(byte[] data) {
        }

        protected override void Update() {
            base.Update();
        }
    }

}
