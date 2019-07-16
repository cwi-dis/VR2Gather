using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class VoiceSender {
    public int userID = 0;

    SocketIOServer socketIOServer;

    System.IntPtr handle;
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
            signals_unity_bridge_pinvoke.SetPaths();
            //string url = "https://vrt-evanescent.viaccess-orca.com/audio/";
            string url = "http://localhost:9000/";
            handle = bin2dash_pinvoke.vrt_create("player_" + userID, bin2dash_pinvoke.VRT_4CC('R', 'A', 'W', 'W'), url, 500, 500);
            if (handle == System.IntPtr.Zero) Debug.Log($">>> HANDLE ERROR ");
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
        if (handle != System.IntPtr.Zero) {
            if (buffer == System.IntPtr.Zero) buffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tmp, 0);
            bin2dash_pinvoke.vrt_push_buffer(handle, buffer, (uint)tmp.Length);
        }
    }

    public void Close(){
        if (socketIOServer != null)
            socketIOServer.Close();
        if (handle != System.IntPtr.Zero) {
            bin2dash_pinvoke.vrt_destroy(handle);
            handle = System.IntPtr.Zero;
            buffer = System.IntPtr.Zero;
        }
    }
}
