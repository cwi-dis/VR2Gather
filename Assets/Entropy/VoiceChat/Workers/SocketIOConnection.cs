using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BestHTTP.SocketIO;


public interface ISocketReader
{
    void OnData(byte[] data);
}


public class SocketIOConnection : MonoBehaviour {
    public Socket socket;
    public bool useEcho = true;
    private SocketManager manager;
    bool isConnected = false;

    // Start is called before the first frame update
    void Start()
    {
        SocketOptions options = new SocketOptions();
        options.AutoConnect = false;
        options.ConnectWith = BestHTTP.SocketIO.Transports.TransportTypes.WebSocket;

        // Create the Socket.IO manager
        manager = new SocketManager(new System.Uri("https://poor-echo-server.glitch.me/socket.io/"), options);
        socket = manager.Socket;

        socket.On(SocketIOEventTypes.Error, (socket, packet, args) => {
            if (args != null && args.Length > 0) Debug.Log(string.Format("Error: {0}", args[0].ToString()));
            else Debug.Log("Error: ???");
        });

        socket.On(SocketIOEventTypes.Connect, (socket, packet, args) => {
            this.socket = socket;
            socket.Emit("setEcho", useEcho);
            isConnected = true;
        });

        socket.On("disconnect", (socket, packet, args) => {
            isConnected = false;
            byte id = packet.Attachments[0][0];
        });

        socket.On("dataChannel", OnData, false);
        manager.Open();
    }

    NTPTools.NTPTime tempTime;
    void OnData(Socket socket, Packet packet, params object[] args) {
        if (packet != null && packet.Attachments != null) {
            var data = packet.Attachments[0];
            readers[data[0]].OnData(data);
        }
    }

    ISocketReader[] readers = new ISocketReader[16];
    public void registerReader(ISocketReader reader, byte id) {
        readers[id] = reader;
    }

    public IEnumerator WaitConnection() {
        while(!isConnected) yield return null;
        Debug.Log("Connected!!!");
    }
}
