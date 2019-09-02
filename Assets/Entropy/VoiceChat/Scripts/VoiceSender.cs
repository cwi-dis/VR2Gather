using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class VoiceSender {
    public int userID = 0;

    SocketIOServer socketIOServer;

    bin2dash.connection handle;
    System.IntPtr buffer;

    ushort frequency;
    BaseCodec codec;
    public VoiceSender(int userID, BaseCodec codec, bool useEcho, bool useSocket = true)
    {
        this.userID = userID;
        this.codec = codec;
        if (useSocket)
            socketIOServer = new SocketIOServer(useEcho);
        else {
            //string url = "https://vrt-evanescent.viaccess-orca.com/audio/";
            string url = "http://localhost:9000/";
            handle = bin2dash.create("player_" + userID, bin2dash.VRT_4CC('R', 'A', 'W', 'W'), url, 500, 500);
            if (handle == null) Debug.Log($">>> HANDLE ERROR ");
        }
    }
    int cnt = 0;
    // Multy-threader function
    public void Send(float[] data) {
        byte[] tmp = codec.Compress(data, 1 + 8);
        tmp[0] = (byte)userID;
        NTPTools.GetNTPTime().GetByteArray(tmp, 1);
        if(socketIOServer!=null)
            socketIOServer.Send(tmp);
        if (handle != null) {
            // xxxjack this code looks suspect. It seems we take the address of a local variable (tmp) and store
            // it in an instance variable (buffer)
            if (buffer == System.IntPtr.Zero) buffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tmp, 0);
            handle.push_buffer(buffer, (uint)tmp.Length);
        }
    }

    public void Close(){
        if (socketIOServer != null)
            socketIOServer.Close();
        handle = null;
        buffer = System.IntPtr.Zero;
    }
}
