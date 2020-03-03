using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO;
using OrchestratorWrapping;
using System;

namespace Workers
{
    public class SocketIOReader : BaseWorker, ISocketReader {
        MonoBehaviour monoBehaviour;
        Coroutine coroutine;

        byte[][] pending = new byte[10][];
        int read = 0;
        int write = 0;

        string userID;

        public SocketIOReader(SocketIOConnection  socketIOConnection, string userID) : base(WorkerType.Init)
        {
            this.userID = userID;

            if (socketIOConnection != null)
            {
                socketIOConnection.registerReader(this, (byte)(int.Parse(userID)));
            }
            else
            {
                OrchestratorWrapper.instance.OnAudioSent += OnUserAudioPacketReceived;
            }
            
            Start();
        }

        public SocketIOReader() : base(WorkerType.Init) {
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

        public void OnUserAudioPacketReceived(UserAudioPacket userAudioPacket)
        {
            if(userAudioPacket.userID == userID)
            {
                pending[write % 10] = userAudioPacket.audioPacket;
                write++;
            }
        }
    }
}