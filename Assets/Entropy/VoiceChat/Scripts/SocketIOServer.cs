using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO;

public class SocketIOServer {
    public static SocketIOServer Instance { get; private set; }
    VoiceReceiver receiver;
    Socket socket;

    private SocketManager Manager;
    // Start is called before the first frame update
    public SocketIOServer(VoiceReceiver receiver) {
        SocketIOServer.Instance = this;
        this.receiver = receiver;

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
    void OnSoundData(Socket socket, Packet packet, params object[] args)
    {
        if (packet != null && packet.Attachments!=null) {
            byte[] data = packet.Attachments[0];

            if (floatBuffer == null) floatBuffer = new float[data.Length / 4];
            System.Buffer.BlockCopy(data, 0, floatBuffer, 0, data.Length);
            receiver.ReceiveBuffer(floatBuffer);
        }
        else { }

    }

    void OnNewMessage(Socket socket, Packet packet, params object[] args)
    {
        Debug.Log((float)args[0] );
    }

    byte[] byteBuffer;
    // Update is called once per frame
    public void Send(float[] buffer) {
        if (this.socket != null)
        {
            if (byteBuffer == null) byteBuffer = new byte[buffer.Length * 4];
            System.Buffer.BlockCopy(buffer, 0, byteBuffer, 0, byteBuffer.Length);
            this.socket.Emit("soundData", byteBuffer);
        }

        //        if(socket!=null) socket.Emit("chat", (object)buffer);
    }

    public void Close() {
        Manager.Close();
    }
}
