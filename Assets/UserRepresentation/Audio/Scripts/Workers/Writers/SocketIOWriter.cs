using System;
using UnityEngine;

namespace Workers {
    public class SocketIOWriter : BaseWorker {
        private byte userID;
        private SocketIOConnection socketIOConnection = null;
        private Action<byte[]> packetSender;

        public SocketIOWriter(SocketIOConnection socketIOConnection, int userID) : base(WorkerType.End) {
            this.userID = (byte)userID;
            this.socketIOConnection = socketIOConnection;
            Start();
        }
        public SocketIOWriter(Action<byte[]> pSenderDelegate) : base(WorkerType.End) {
            packetSender = pSenderDelegate;
            Start();
        }
        protected override void Update() {
            base.Update();
            if (token != null) {
                byte[] tmp = token.currentByteArray;
                tmp[0] = userID;
                token.latency.GetByteArray(tmp, 1);

                if (socketIOConnection != null) {
                    socketIOConnection.socket.Emit("dataChannel", (object)tmp);
                }

                packetSender?.Invoke(tmp);

                Next();
            }
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("[SocketIOWriter][OnStop] SocketIOWriter Sopped.");
        }
    }
}