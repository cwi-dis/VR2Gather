using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO;

public class SocketIOServer {
    public static SocketIOServer Instance { get; private set; }
    Socket socket;

    private SocketManager Manager;
    // Start is called before the first frame update
    public SocketIOServer() {
        SocketIOServer.Instance = this;

        // Change an option to show how it should be done
        SocketOptions options = new SocketOptions();
        options.AutoConnect = false;
        options.ConnectWith = BestHTTP.SocketIO.Transports.TransportTypes.WebSocket;

        // Create the Socket.IO manager
        Manager = new SocketManager(new System.Uri("https://poor-echo-server.glitch.me/socket.io/"), options);

        var connection = Manager.Socket;

        connection.On("soundData", OnSoundData, false);

        // The argument will be an Error object.
        connection.On(SocketIOEventTypes.Error, (socket, packet, args) =>
        {
            if(args!=null && args.Length>0 ) Debug.Log(string.Format("Error: {0}", args[0].ToString()));
            else Debug.Log("Error: ???" );
        });
        connection.On(SocketIOEventTypes.Connect, (socket, packet, args) => {
            this.socket = socket;
        });
        // We set SocketOptions' AutoConnect to false, so we have to call it manually.
        Manager.Open();
    }

    float[] floatBuffer;
    VoicePlayer[] player = new VoicePlayer[4];
    void OnSoundData(Socket socket, Packet packet, params object[] args) {
        if (packet != null && packet.Attachments!=null) {
            var data = packet.Attachments[0];
            int userID = data[0];
            if (player[userID] == null) {
                player[userID] = new GameObject("Player_" + userID).AddComponent<VoicePlayer>();
                player[userID].Init((data[1] << 8) | data[2]);
            }
            player[userID].receiver.ReceiveBuffer( data );
        }
    }

    // Update is called once per frame
    public void Send(byte[] buffer) {
        if(socket!=null) socket.Emit("soundData", (object)buffer);
    }

    public void Close() {
        Manager.Close();
    }
}
