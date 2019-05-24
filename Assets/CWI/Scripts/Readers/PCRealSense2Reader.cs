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
            Debug.LogError("PCRealSense2Reader: cwipc_realsense2: " + errorMessage);
            return;
        }
        Debug.Log("xxxjack encName is " + encName);
        if (encName != "")
        {
            encoder = API_cwipc_codec.cwipc_new_encoder(API_cwipc_codec.CWIPC_ENCODER_PARAM_VERION, System.IntPtr.Zero, ref errorPtr);
            if (encoder == System.IntPtr.Zero)
            {
                string errorMessage = Marshal.PtrToStringAuto(errorPtr);
                Debug.LogError("PCRealSense2Reader: cwipc_new_encoder: " + errorMessage);
                API_cwipc_util.cwipc_source_free(reader);
                reader = System.IntPtr.Zero;
            }
            Debug.Log("xxxjack encoder is " + encoder);
            // xxxjack allocate bin2dash
        }
    }

    public override PointCloudFrame get()
    {
        if (reader == System.IntPtr.Zero)
        {
            Debug.LogError("PCRealSense2Reader: cwipc.reader == NULL");
            return null;
        }
        var rv = API_cwipc_util.cwipc_source_get(reader);
        Debug.Log("xxxjack pc=" + rv + ", encoder=" + encoder);
        if (rv == System.IntPtr.Zero) return null;
        if (encoder != System.IntPtr.Zero) PushToEncoder(rv);
        return new PointCloudFrame(rv);
    }

    protected void PushToEncoder(System.IntPtr pc)
    {
        Debug.Log("xxxjack PushToEncoder " + pc);
        API_cwipc_codec.cwipc_encoder_feed(encoder, pc);
        if (API_cwipc_codec.cwipc_encoder_available(encoder, true))
        {
            unsafe
            {
                int size = (int)API_cwipc_codec.cwipc_encoder_get_encoded_size(encoder);
                Debug.Log("xxxjack encoded size would be " + size);
                Unity.Collections.NativeArray<byte> byteArray;
                byteArray = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.TempJob);
                System.IntPtr ptr = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(byteArray);
                bool ok = API_cwipc_codec.cwipc_encoder_copy_data(encoder, ptr, (System.IntPtr)size);
                // xxxjack pass 2 bin2dash
                byteArray.Dispose();
            }
        }
    }
}
