using System.Runtime.InteropServices;
using UnityEngine;

public class PCRealSense2Reader : PCSyntheticReader
{
    protected System.IntPtr encoder;
    protected System.IntPtr uploader;

    // Start is called before the first frame update
    public PCRealSense2Reader(string configFilename, string encName="", string encURL="")
    {
        encoder = System.IntPtr.Zero;
        uploader = System.IntPtr.Zero;

        System.IntPtr errorPtr = System.IntPtr.Zero;
        reader = API_cwipc_realsense2.cwipc_realsense2(configFilename, ref errorPtr);
        if (reader == System.IntPtr.Zero)
        {
            string errorMessage = Marshal.PtrToStringAuto(errorPtr);
            Debug.LogError("PCRealSense2Reader: Error: " + errorMessage);
        }
    }

    public PointCloudFrame get()
    {
        if (reader == System.IntPtr.Zero)
        {
            Debug.LogError("PCRealSense2Reader: cwipc.reader == NULL");
            return null;
        }
        var rv = API_cwipc_util.cwipc_source_get(reader);
        if (rv == System.IntPtr.Zero) return null;
        PushToEncoder(rv);
        return new PointCloudFrame(rv);
    }

    protected void PushToEncoder(System.IntPtr pc)
    {
        Debug.Log("xxxjack PushToEncoder" + pc);
    }
}
