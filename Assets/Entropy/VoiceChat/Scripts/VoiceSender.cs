#define USE_SOCKETS

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
            handle = bin2dash_pinvoke.vrt_create("player_" + userID, bin2dash_pinvoke.VRT_4CC('R', 'A', 'W', 'W'), "https://vrt-evanescent.viaccess-orca.com/audio/"/*"http://localhost:9000/"*/, 100000, 100000);
            if (handle == System.IntPtr.Zero) Debug.Log($">>> HANDLE ERROR ");
        }
    }
    int cnt = 0;
    // Multy-threader function
    public void Send(float[] data) {
        byte[] tmp = codec.Compress(data, 1 + 8);
        tmp[0] = (byte)userID;
        var time = NTPTools.GetNTPTime();
        tmp[1] = time.T0; tmp[2] = time.T1; tmp[3] = time.T2; tmp[4] = time.T3; tmp[5] = time.T4; tmp[6] = time.T5; tmp[7] = time.T6; tmp[8] = time.T7;
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
