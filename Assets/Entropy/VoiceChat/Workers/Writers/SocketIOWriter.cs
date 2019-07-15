using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO;

namespace Workers
{
    public class SocketIOWriter : BaseWorker
    {
        Socket socket;
        public SocketIOWriter(Socket socket) : base(WorkerType.End)
        {
            this.socket = socket;
            Start();
        }

        protected override void Update()
        {
            base.Update();
            if (token != null ) {
                byte[] tmp = token.currentByteArray;
                if (token.currentSize != tmp.Length) {
                    tmp = new byte[token.currentSize];
                    System.Array.Copy(token.currentByteArray, tmp, token.currentSize);
                }
                socket.Emit("soundData", (object)tmp);
                //// 
                Next();
            }
        }

        public override void OnStop()
        {
            base.OnStop();
            Debug.Log("SocketIOReader Sopped");
        }
   }
}