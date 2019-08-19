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
    cwipc.decoder decoder;

    public PCSocketReader(string _hostname, int _port) {
        hostname = _hostname;
        port = _port;
        failed = false;
        decoder = cwipc.new_decoder();
        if (decoder == null)
        {
            Debug.LogError("PCSocketReader: Error allocating cwipc_decoder"); // Shoulnd't happen, should throw an exception
        }

    }

    public void free() {
        if (pointCloudFrame != null) { pointCloudFrame.Release(); pointCloudFrame = null; }

    }

    public bool eof() {
        return failed;
    }

    public bool available(bool wait) {
        return !failed;
    }
    PointCloudFrame pointCloudFrame = new PointCloudFrame();

    public PointCloudFrame get() {
        if (failed) return null;
        TcpClient clt = null;
        try
        {
            clt = new TcpClient(hostname, port);
        }
        catch (SocketException)
        {
            Debug.LogError("PCSocketReader: cannot connect to host " + hostname + " port " + port);
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
        decoder.feed(ptr, bytes.Length);
        bool ok = decoder.available(true);
        if (!ok)
        {
            Debug.LogError("PCSocketReader: cwipc_decoder: no pointcloud available");
            return null;
        }
        cwipc.pointcloud pc = decoder.get();
        if (pc == null)
        {
            Debug.LogError("PCSocketReader: cwipc_decoder: did not return a pointcloud");
            return null;
        }

        pointCloudFrame.SetData(pc);
        return pointCloudFrame;

    }

    public virtual void update() { }
}
