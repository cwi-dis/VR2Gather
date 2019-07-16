using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO;

namespace Workers
{
    public class SocketIOReader : BaseWorker, ISocketReader {
        MonoBehaviour monoBehaviour;
        Coroutine coroutine;

        byte[][] pending = new byte[10][];
        int read = 0;
        int write = 0;

        int userID;
        public SocketIOReader(SocketIOConnection  socketIOConnection, int userID) : base(WorkerType.Init) {            
            socketIOConnection.registerReader(this, (byte)userID);
            this.userID = userID;
            Start();
        }

        protected override void Update() {
            base.Update();
            if (token != null && read<write) {
               // lock (pending)
                {
                    byte[] tmp = pending[read%10];
                    read++;
                    token.currentByteArray = tmp;
                    token.currentSize = tmp.Length;
                    Next();
                }
            }
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("SocketIOReader Sopped");
        }

        public void OnData(byte[] data) {
            pending[write%10]= data;
            write++;
        }
    }
}