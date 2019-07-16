using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO;

public class SocketIOServer {
    public static SocketIOServer Instance { get; private set; }
    Socket socket;
    public bool isConnected = false;

    private SocketManager Manager;
    // Start is called before the first frame update
    public SocketIOServer(bool useEcho) {
        SocketIOServer.Instance = this;

        // Change an option to show how it should be done
        SocketOptions options = new SocketOptions();
        options.AutoConnect = false;
        options.ConnectWith = BestHTTP.SocketIO.Transports.TransportTypes.WebSocket;

        // Create the Socket.IO manager
        Manager = new SocketManager(new System.Uri("https://poor-echo-server.glitch.me/socket.io/"), options);
//        Manager = new SocketManager(new System.Uri("http://localhost:3000/socket.io/"), options);

        var connection = Manager.Socket;

        connection.On("soundData", OnSoundData, false);

        // The argument will be an Error object.
        connection.On(SocketIOEventTypes.Error, (socket, packet, args) => {
            if(args!=null && args.Length>0 ) Debug.Log(string.Format("Error: {0}", args[0].ToString()));
            else Debug.Log("Error: ???" );
        });
        connection.On(SocketIOEventTypes.Connect, (socket, packet, args) => {
            this.socket = socket;
            socket.Emit("setEcho", useEcho);
            isConnected = true;
        });

        connection.On("disconnect", (socket, packet, args) => {
            byte id = packet.Attachments[0][0];
            Debug.Log($" disconnect {id}");
            if (player[id] != null)
            {
                GameObject.Destroy(player[id].gameObject);
                player[id] = null;
            }
        });
        // We set SocketOptions' AutoConnect to false, so we have to call it manually.
        Manager.Open();
    }

    float[] floatBuffer;
    static public VoicePlayer[] player = new VoicePlayer[32];
    


    NTPTools.NTPTime tempTime;
    void OnSoundData(Socket socket, Packet packet, params object[] args) {
        if (packet != null && packet.Attachments!=null) {
            var data = packet.Attachments[0];
            int userID = data[0];
            tempTime.SetByteArray(data, 1);
            
            var lat = NTPTools.GetNTPTime().time - tempTime.time;
            player[userID].name = $"Player_{userID} Lat ({lat})";
            player[userID].receiver.ReceiveBuffer( BaseCodec.Instance.Uncompress(data, 1+8) );
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
