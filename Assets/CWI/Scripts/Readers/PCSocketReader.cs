using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Linq;
using System.Runtime.InteropServices;

public class PCSocketReader : PCBaseReader
{
    string hostname;
    int port;
    bool failed;
    IntPtr decoder;

    public PCSocketReader(string _hostname, int _port) {
        hostname = _hostname;
        port = _port;
        failed = false;
        decoder = API_cwipc_codec.cwipc_new_decoder();
        if (decoder == IntPtr.Zero)
        {
            Debug.LogError("Cannot create PCSocketReader");
        }

    }

    public void free() {
    }

    public bool eof() {
        return failed;
    }

    public bool available(bool wait) {
        return !failed;
    }

    public PointCloudFrame get() {
        if (failed) return null;
        TcpClient clt = null;
        try
        {
            clt = new TcpClient(hostname, port);
        }
        catch (SocketException)
        {
            Debug.LogError("connection error");
        }
        if (clt == null)
        {
            failed = true;
            return null;
        }
        List<byte> allData = new List<byte>();
        using (NetworkStream stream = clt.GetStream())
        {
            Byte[] data = new Byte[1024];

            do
            {
                int numBytesRead = stream.Read(data, 0, data.Length);

                if (numBytesRead == data.Length)
                {
                    allData.AddRange(data);
                }
                else if (numBytesRead > 0)
                {
                    allData.AddRange(data.Take(numBytesRead));
                }
            } while (stream.DataAvailable);
        }
        byte[] bytes = allData.ToArray();

        var ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);
        API_cwipc_codec.cwipc_decoder_feed(decoder, ptr, bytes.Length);
        bool ok = API_cwipc_util.cwipc_source_available(decoder, true);
        if (!ok)
        {
            Debug.LogError("cwipc_decoder: no pointcloud available");
            return null;
        }
        var pc = API_cwipc_util.cwipc_source_get(decoder);
        if (pc == null)
        {
            Debug.LogError("cwipc_decoder: did not return a pointcloud");
            return null;
        }


        return new PointCloudFrame(pc);

    }
}
