using OrchestratorWrapping;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Workers
{
    public class SocketIOReader : BaseWorker, ISocketReader
    {
        QueueThreadSafe outQueue;
        string userID;

        public SocketIOReader(string _userID, QueueThreadSafe _outQueue) : base(WorkerType.End) {
            userID = _userID;
            if (_outQueue == null) {
                throw new System.Exception($"{Name()}: outQueue is null");
            }
            outQueue = _outQueue;
            try {
                Debug.Log($"{Name()}: Started {userID}.");

                OrchestratorWrapper.instance.RegisterForDataStream(userID, "AUDIO");
                OrchestratorWrapper.instance.OnDataStreamReceived += OnAudioPacketReceived;
//                OrchestratorWrapper.instance.OnAudioSent += OnAudioPacketReceived2;

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
            outQueue?.Close();
            Debug.Log($"{Name()}: Stopped.");
            OrchestratorWrapper.instance.UnregisterFromDataStream(userID, "AUDIO");
        }
        /*
        private void OnAudioPacketReceived2(UserAudioPacket pPacket ) {
            //if (pPacket.dataStreamUserID == userID) 
            {
                Debug.Log($"SocketOIReader {pPacket.audioPacket.Length}");
                BaseMemoryChunk chunk = new NativeMemoryChunk(pPacket.audioPacket.Length);
                System.Runtime.InteropServices.Marshal.Copy(pPacket.audioPacket, 0, chunk.pointer, chunk.length);
                outQueue.Enqueue(chunk);
                OnData(pPacket.audioPacket);
            }
        }
        */
        private void OnAudioPacketReceived(UserDataStreamPacket pPacket) {
            //if (pPacket.dataStreamUserID == userID) 
                {
                BaseMemoryChunk chunk = new NativeMemoryChunk(pPacket.dataStreamPacket.Length);
                System.Runtime.InteropServices.Marshal.Copy(pPacket.dataStreamPacket, 0, chunk.pointer, chunk.length);
                outQueue.Enqueue(chunk);
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
