using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using UnityEngine;

public class PCSUBReader : PCBaseReader { 

    string url;
    int streamNumber;
    bool failed;
    sub.connection subHandle;
    cwipc.decoder decoder;
    byte[] currentBuffer;
    IntPtr currentBufferPtr;

    public PCSUBReader(Config._User._SUBConfig cfg) {
        failed = true;
        url = cfg.url;
        streamNumber = cfg.streamNumber;

        System.Threading.Thread.Sleep(2000);

        subHandle = sub.create("source_from_sub");
        if (subHandle == null) {
            Debug.LogError("PCSUBReader: sub.create failed");
            return;
        }else
            Debug.Log("PCSUBReader: sub.create ok!");

        bool ok = subHandle.play(url);
        if (!ok) {
            Debug.LogError("PCSUBReader: sub.play failed for " + url);
            return;
        }
        decoder = cwipc.new_decoder();
        if (decoder == null) {
            Debug.LogError("PCSUBReader: cwipc_new_decoder: failed to create decoder"); // Should not happen, should throw an exception
            return;
        }

        failed = false;
    }

    public void free()
    {
        subHandle = null;
        failed = true; // Not really failed, but reacts the same (nothing will work anymore)
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
        sub.FrameInfo info = new sub.FrameInfo();
        if (failed) return null;

        int bytesNeeded = subHandle.grab_frame(streamNumber, IntPtr.Zero, 0, ref info);
        if (bytesNeeded == 0) {
            Debug.Log("No data");
            return null;
        }else
            Debug.Log("data "+ bytesNeeded);

        if (currentBuffer == null || bytesNeeded > currentBuffer.Length)
        {
            Debug.Log("PCSUBReader: allocating more memory");
            currentBuffer = new byte[(int)(bytesNeeded * Config.Instance.memoryDamping)]; // Reserves 30% more.
            currentBufferPtr = Marshal.UnsafeAddrOfPinnedArrayElement(currentBuffer, 0);
        }

        int bytesRead = subHandle.grab_frame(streamNumber, currentBufferPtr, bytesNeeded, ref info);
        if (bytesRead != bytesNeeded) {
            Debug.LogError("PCSUBReader: sub.grab_frame returned " + bytesRead + " bytes after promising " + bytesNeeded);
            return null;
        }
        decoder.feed(currentBufferPtr, bytesRead);
        bool ok = decoder.available(true);
        if (!ok) {
            Debug.LogError("PCSUBReader: cwipc_decoder: no pointcloud available");
            return null;
        }
        var pc = decoder.get();
        if (pc == null)
        {
            Debug.LogError("PCSUBReader: cwipc_decoder: did not return a pointcloud");
            return null;
        }
        pointCloudFrame.SetData(pc);
        return pointCloudFrame;
    }

    public virtual void update() { }

}
