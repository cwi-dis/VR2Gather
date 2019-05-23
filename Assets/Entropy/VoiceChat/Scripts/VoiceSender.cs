using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class VoiceSender {
    SocketIOServer socketIOServer;

    byte playerID;
    ushort frequency;
    public VoiceSender(byte playerID, ushort frequency ) {
        this.playerID = playerID;
        this.frequency = frequency;
        socketIOServer = new SocketIOServer();
    }

    byte[] buffer;
    // Multy-threader function
    public void Send(float[] data) {        
        if (buffer == null) {
            buffer = new byte[data.Length * 4 + 3];
            buffer[0] = playerID;
            buffer[1] = (byte)(frequency >> 8);
            buffer[2] = (byte)(frequency & 255);
        }
        // Time stamp!
        System.Buffer.BlockCopy(data, 0, buffer, 3, buffer.Length-3);
        socketIOServer.Send(buffer);
    }

    public void Close(){
        socketIOServer.Close();
    }
}
