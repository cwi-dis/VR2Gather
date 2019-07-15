using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO;

namespace Workers
{
    public class SocketIOReader : BaseWorker {
        MonoBehaviour monoBehaviour;
        Coroutine coroutine;

        Stack<byte[]> free = new Stack<byte[]>();
        Stack<byte[]> pending = new Stack<byte[]>();
        byte[] buffer;

        public SocketIOReader(Socket socket) : base(WorkerType.Init) {
            socket.On("soundData", OnSoundData, false);
            Start();
        }

        protected override void Update() {
            base.Update();
            if (token != null) {
                lock (pending) {
                    if (pending.Count > 0){
                        byte[] tmp = pending.Pop();
                        if( buffer== null || buffer.Length< tmp.Length)
                            buffer = new byte[(int)(tmp.Length * 1.3f)];
                        System.Array.Copy(tmp, buffer, tmp.Length);
                        free.Push(tmp);
                        token.currentByteArray = buffer;
                        token.currentSize = tmp.Length;
                        Next();
                    }
                }
            }
        }

        public override void OnStop() {
            base.OnStop();
            Debug.Log("SocketIOReader Sopped");
        }

        NTPTools.NTPTime tempTime;
        void OnSoundData(Socket socket, Packet packet, params object[] args) {
            if (packet != null && packet.Attachments != null) {
                var data = packet.Attachments[0];
                lock (pending) {
                    byte[] tmp;
                    if (free.Count == 0) {
                        tmp = new byte[data.Length];
                    }
                    else {
                        tmp = free.Pop();
                    }
                    System.Array.Copy(data, tmp, tmp.Length);
                    pending.Push(tmp);
                }
            }
        }
    }
}