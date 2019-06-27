using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class PCSyntheticReader : PCBaseReader
{
    protected System.IntPtr reader;

    public PCSyntheticReader()
    {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        reader = API_cwipc_util.cwipc_synthetic(ref errorPtr);
        if (reader == System.IntPtr.Zero)
        {
            string errorMessage = Marshal.PtrToStringAnsi(errorPtr);
            Debug.LogError("PCSyntheticReader: Error: " + errorMessage);
        }
    }

    public virtual bool eof() {
        if (reader == System.IntPtr.Zero) return true;
        return API_cwipc_util.cwipc_source_eof(reader);
    }

    public virtual bool available(bool wait) {
        if (reader == System.IntPtr.Zero) return false;
        return API_cwipc_util.cwipc_source_available(reader, wait);
    }

    public virtual void free()
    {
        if (reader == System.IntPtr.Zero) return;
        API_cwipc_util.cwipc_source_free(reader);
        reader = System.IntPtr.Zero;
        if (pointCloudFrame != null) { pointCloudFrame.Release(); pointCloudFrame = null; }

    }

    protected PointCloudFrame pointCloudFrame = new PointCloudFrame();

    public virtual PointCloudFrame get() {
        if (reader == System.IntPtr.Zero) {
            Debug.LogError("PCSyntheticReader: cwipc.reader == NULL");
            return null;
        }
        var rv = API_cwipc_util.cwipc_source_get(reader);
        if (rv == System.IntPtr.Zero) return null;
        pointCloudFrame.SetData(rv);
        return pointCloudFrame;
    }

    public virtual void update() { }
}
