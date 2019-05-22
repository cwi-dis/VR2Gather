using System.Runtime.InteropServices;
using UnityEngine;

public class PCRealSense2Reader : PCSyntheticReader
{
    // Start is called before the first frame update
    public PCRealSense2Reader(string configFilename)
    {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        obj = API_cwipc_realsense2.cwipc_realsense2(configFilename, ref errorPtr);
        if (obj == System.IntPtr.Zero)
        {
            string errorMessage = Marshal.PtrToStringAuto(errorPtr);
            Debug.LogError("PCRealSense2Reader: Error: " + errorMessage);
        }
    }
}
