using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class VoiceSender {
    SocketIOServer socketIOServer;

    byte UseEcho;
    ushort frequency;
    BaseCodec codec;
    public VoiceSender(bool UseEcho, BaseCodec codec) {
        this.codec = codec;
        this.UseEcho = (byte)(UseEcho?1:0);
        socketIOServer = new SocketIOServer();
    }

    // Multy-threader function
    public void Send(float[] data) {
        byte[] tmp = codec.Compress(data, 1 + 8);
        tmp[0] = UseEcho;
        var time = NTPTools.GetNTPTime();
        tmp[1] = time.T0; tmp[2] = time.T1; tmp[3] = time.T2; tmp[4] = time.T3; tmp[5] = time.T4; tmp[6] = time.T5; tmp[7] = time.T6; tmp[8] = time.T7;
        socketIOServer.Send(tmp);
    }

    public void Close(){
        socketIOServer.Close();
    }
}
