using VRTDash;
using Orchestrator;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRTCore;

namespace VRTSocketIO
{
    public class SocketIOReader : BaseReader, ISocketReader
    {
        PCSubReader.TileDescriptor[] descriptors;

        User user;

        public SocketIOReader(User user, string remoteStream, PCSubReader.TileDescriptor[] descriptors) : base(WorkerType.End)
        {
            this.user = user;
            if (descriptors == null)
            {
                throw new System.Exception($"{Name()}: descriptors is null");
            }
            this.descriptors = descriptors;
            try
            {
                for (int i = 0; i < this.descriptors.Length; ++i)
                {
                    this.descriptors[i].name = $"{user.userId}{remoteStream}#{i}";
                    Debug.Log($"[FPA] RegisterForDataStream userId {user.userId} StreamType {this.descriptors[i].name}");
                    OrchestratorWrapper.instance.RegisterForDataStream(user.userId, this.descriptors[i].name);
                }
                OrchestratorWrapper.instance.OnDataStreamReceived += OnDataPacketReceived;

                Start();
                Debug.Log($"{Name()}: Started {remoteStream}.");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: Exception: {e.Message}");
                throw e;
            }
        }

        public SocketIOReader(User user, string remoteStream, QueueThreadSafe outQueue)
        : this(user,
            remoteStream,
              new PCSubReader.TileDescriptor[]
              {
                  new PCSubReader.TileDescriptor()
                  {
                      outQueue = outQueue
                  }
              }
            )
        {
        }

        public override string Name()
        {
            return $"{GetType().Name}";
        }


        public override void Stop()
        {
            base.Stop();
            for (int i = 0; i < descriptors.Length; ++i)
            {
                descriptors[i].outQueue?.Close();
                Debug.Log($"[FPA] {Name()}: Stopped.");
                if (OrchestratorWrapper.instance != null && OrchestratorController.Instance.SelfUser != null)
                    OrchestratorWrapper.instance.UnregisterFromDataStream(OrchestratorController.Instance.SelfUser.userId, descriptors[i].name);
            }
        }
        private void OnDataPacketReceived(UserDataStreamPacket pPacket)
        {
            int id = 0;
            string strID = pPacket.dataStreamType.Substring(pPacket.dataStreamType.LastIndexOf('#') + 1);
            if (int.TryParse(strID, out id))
            {
                if (pPacket.dataStreamType == descriptors[id].name)
                {
                    BaseMemoryChunk chunk = new NativeMemoryChunk(pPacket.dataStreamPacket.Length);
                    System.Runtime.InteropServices.Marshal.Copy(pPacket.dataStreamPacket, 0, chunk.pointer, chunk.length);
                    descriptors[id].outQueue.Enqueue(chunk);
                }
            }
            else
            {
                Debug.Log($"[FPA] ERROR parsing {strID}.");
            }
        }

        public void OnData(byte[] data)
        {
        }

        protected override void Update()
        {
            base.Update();
        }
    }

}
