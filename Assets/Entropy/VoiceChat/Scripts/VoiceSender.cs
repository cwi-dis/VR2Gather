//#define USE_SOCKETS

using System.Net;
using System.Net.Sockets;
using UnityEngine;



public class VoiceSender {
#if USE_SOCKETS
    SocketIOServer socketIOServer;
#else
    System.IntPtr handle;
    System.IntPtr buffer;
#endif
    byte UseEcho;
    ushort frequency;
    BaseCodec codec;
    public VoiceSender(bool UseEcho, BaseCodec codec) {
        this.codec = codec;
        this.UseEcho = (byte)(UseEcho?1:0);
#if USE_SOCKETS
        socketIOServer = new SocketIOServer();
#else
        handle = bin2dash_pinvoke.vrt_create("vrtogether", bin2dash_pinvoke.VRT_4CC('R','A','W','W'), "http://vrt-evanescent.viaccess-orca.com/fernando@entropy-audio.mpd", 0, 0);
#endif
    }
    int cnt = 0;
    // Multy-threader function
    public void Send(float[] data) {
        byte[] tmp = codec.Compress(data, 1 + 8);
        tmp[0] = UseEcho;
        var time = NTPTools.GetNTPTime();
        tmp[1] = time.T0; tmp[2] = time.T1; tmp[3] = time.T2; tmp[4] = time.T3; tmp[5] = time.T4; tmp[6] = time.T5; tmp[7] = time.T6; tmp[8] = time.T7;
#if USE_SOCKETS
        socketIOServer.Send(tmp);
#else
        if (handle != System.IntPtr.Zero) {
            if (buffer == System.IntPtr.Zero) buffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tmp, 0);
            Debug.Log($">>> Buffer.Length {tmp.Length} {cnt++}");
            bin2dash_pinvoke.vrt_push_buffer(handle, buffer, (uint)tmp.Length);
        }
#endif
    }

    public void Close(){
#if USE_SOCKETS
        socketIOServer.Close();
#else
        bin2dash_pinvoke.vrt_destroy(handle);
        handle = System.IntPtr.Zero;
        buffer = System.IntPtr.Zero;
#endif
    }
}
