using System.Runtime.InteropServices;
using UnityEngine;

public class PCRealSense2Reader : PCSyntheticReader
{
    // Start is called before the first frame update
    public PCRealSense2Reader(string configFilename)
    {
        System.IntPtr errorPtr = System.IntPtr.Zero;
        obj = API_cwipc_realsense2.cwipc_realsense2(configFilename, ref errorPtr);
        Debug.Log("xxxjack realsense2 configFile " + configFilename + ", obj " + obj + "errorPtr " + errorPtr);
        int maxTile = API_cwipc_util.cwipc_tiledsource_maxtile(obj);
        Debug.Log("xxxjack realsense2 maxtile=" + maxTile);
        for (int i=0; i<maxTile; i++)
        {
            Debug.Log("xxxjack tileinfo " + i + " is " + API_cwipc_util.cwipc_tiledsource_get_tileinfo(obj, i, System.IntPtr.Zero));
        }
        if (errorPtr != System.IntPtr.Zero)
        {
            string errorMessage = Marshal.PtrToStringAuto(errorPtr);
            Debug.LogError("cwipc_realsense2 returned error: " + errorMessage);
        }     
    }
}
