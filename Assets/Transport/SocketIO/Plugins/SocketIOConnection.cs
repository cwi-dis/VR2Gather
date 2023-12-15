using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Best.SocketIO;
using Best.SocketIO.Events;
using VRT.Core;

namespace VRT.Transport.SocketIO
{
    public interface ISocketReader
    {
        void OnData(byte[] data);
    }


    public class SocketIOConnection : MonoBehaviour
    {
        public Socket socket;
        public string socketIO_URL = "https://poor-echo-server.glitch.me/socket.io/";
        public bool useEcho = true;
        private SocketManager manager;
        bool isConnected = false;

        // Start is called before the first frame update
        void Start()
        {
            SocketOptions options = new SocketOptions();
            options.AutoConnect = false;
            options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

            // Create the Socket.IO manager
            manager = new SocketManager(new System.Uri(socketIO_URL), options);
            //        manager = new SocketManager(new System.Uri("http://127.0.0.1:3000/socket.io/"), options);
            socket = manager.Socket;

            socket.On<Error>(SocketIOEventTypes.Error, (error) =>
            {
                Debug.Log(string.Format("Error: {0}", error));
            });

            socket.On(SocketIOEventTypes.Connect, () =>
            {
                this.socket = manager.Socket;
                socket.Emit("setEcho", useEcho);
                isConnected = true;
            });

            socket.On<IncomingPacket>("disconnect", (packet) =>
            {
                isConnected = false;
            });

            socket.On<IncomingPacket>("dataChannel", OnData);
            manager.Open();
        }

        NTPTools.NTPTime tempTime;
        void OnData(IncomingPacket packet)
        {
            if (packet.Attachements != null)
            {
                var data = packet.Attachements[0];
                // readers[data[0]]?.OnData(data);
            }
        }

        ISocketReader[] readers = new ISocketReader[16];
        public void registerReader(ISocketReader reader, byte id)
        {
            readers[id] = reader;
        }

        public IEnumerator WaitConnection()
        {
            while (!isConnected) yield return null;
            Debug.Log("Connected!!!");
        }
    }
}