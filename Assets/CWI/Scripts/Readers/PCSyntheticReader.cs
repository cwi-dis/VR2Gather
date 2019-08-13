using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class PCSyntheticReader : PCBaseReader
{
    protected cwipc.source reader;

    public PCSyntheticReader()
    {
        reader = cwipc.synthetic();
        if (reader == null)
        {
            Debug.LogError("PCSyntheticReader: Error: could not create synthetic source"); // Should not happen, should throw exception
        }
    }

    public virtual bool eof() {
        if (reader == null) return true;
        return reader.eof(); 
    }

    public virtual bool available(bool wait) {
        if (reader == null) return false;
        return reader.available(wait);
    }

    public virtual PointCloudFrame get() {
        if (reader == null) return null;
        cwipc.pointcloud pc = reader.get();
        if (pc == null) return null;
        PointCloudFrame rv = new PointCloudFrame();
        rv.SetData(pc);
        return rv;
    }

    public virtual void update() { }
}
