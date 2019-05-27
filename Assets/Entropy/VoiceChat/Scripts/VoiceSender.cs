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
            buffer = new byte[data.Length * 4 + 1 + 2 + 8];
            buffer[0] = playerID;
            buffer[1] = (byte)(frequency >> 8);
            buffer[2] = (byte)(frequency & 255);
        }
        var time = NTPTools.GetNTPTime();
        buffer[3] = time.T0; buffer[4] = time.T1; buffer[5] = time.T2; buffer[6] = time.T3; buffer[7] = time.T4; buffer[8] = time.T5; buffer[9] = time.T6; buffer[10] = time.T7;
        // Time stamp!
//        int len;
//        byte[] ret = SpeedX.EncodeToSpeex(data, out len);


        System.Buffer.BlockCopy(data, 0, buffer, (1+2+8), buffer.Length-(1+2+8));
        socketIOServer.Send(buffer);
    }

    public void Close(){
        socketIOServer.Close();
    }
}
