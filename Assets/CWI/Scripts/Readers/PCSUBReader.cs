using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using UnityEngine;

public class PCSUBReader : PCBaseReader { 

    string url;
    int streamNumber;
    bool failed;
    IntPtr subHandle;
    IntPtr decoder;
    byte[] currentBuffer;
    IntPtr currentBufferPtr;

    public PCSUBReader(Config._PCs._SUBConfig cfg) {
        failed = true;
        url = cfg.url;
        streamNumber = cfg.streamNumber;

        bool ok = setup_sub_environment();
        if (!ok) {
            Debug.LogError("PCSUBReader: setup_sub_environment failed");
            return;
        }

        System.Threading.Thread.Sleep(2000);

        subHandle = signals_unity_bridge_pinvoke.sub_create("source_from_sub");
        if (subHandle == IntPtr.Zero) {
            Debug.LogError("PCSUBReader: sub_create failed");
            return;
        }else
            Debug.Log("PCSUBReader: sub_create ok!");

        ok = signals_unity_bridge_pinvoke.sub_play(subHandle, url);
        if (!ok) {
            Debug.LogError("PCSUBReader: sub_play failed for " + url);
            return;
        }
        System.IntPtr errorPtr = System.IntPtr.Zero;
        decoder = API_cwipc_codec.cwipc_new_decoder(ref errorPtr);
        if (decoder == IntPtr.Zero) {
            string errorMessage = Marshal.PtrToStringAnsi(errorPtr);
            Debug.LogError("PCSUBReader: cwipc_new_decoder: " + errorMessage);
            return;
        }

        failed = false;
    }

    internal bool setup_sub_environment()
    {
        signals_unity_bridge_pinvoke.SetPaths();
        return true;
    }

    public void free()
    {
        if (subHandle != IntPtr.Zero)
        {
            signals_unity_bridge_pinvoke.sub_destroy(subHandle);
            subHandle = IntPtr.Zero;
            failed = true; // Not really failed, but reacts the same (nothing will work anymore)
        }
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
        signals_unity_bridge_pinvoke.FrameInfo info = new signals_unity_bridge_pinvoke.FrameInfo();
        if (failed) return null;

        int bytesNeeded = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, streamNumber, IntPtr.Zero, 0, ref info);
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

        int bytesRead = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, streamNumber, currentBufferPtr, bytesNeeded, ref info);
        if (bytesRead != bytesNeeded)
        {
            Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + bytesNeeded);
            return null;
        }


        API_cwipc_codec.cwipc_decoder_feed(decoder, currentBufferPtr, bytesNeeded);
        bool ok = API_cwipc_util.cwipc_source_available(decoder, true);
        if (!ok)
        {
            Debug.LogError("PCSUBReader: cwipc_decoder: no pointcloud available");
            return null;
        }
        var pc = API_cwipc_util.cwipc_source_get(decoder);
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
