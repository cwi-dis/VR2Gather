using System.Runtime.InteropServices;
using UnityEngine;

public class PCRealSense2Reader : PCSyntheticReader
{
    // Start is called before the first frame update
    public PCRealSense2Reader()
    {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        var rv = API_cwipc_realsense2.cwipc_realsense2(null, ref errorPtr);
        if (errorPtr != System.IntPtr.Zero)
        {
            string errorMessage = Marshal.PtrToStringAuto(errorPtr);
            Debug.LogError("cwipc_realsense2 returned error: " + errorMessage);
        }        
    }
}
