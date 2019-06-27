using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class PCRealSense2Reader : PCSyntheticReader
{
    protected System.IntPtr encoder;
    protected System.IntPtr uploader;

    // Start is called before the first frame update
    public PCRealSense2Reader(Config._PCs cfg)
    {
        encoder = System.IntPtr.Zero;
        uploader = System.IntPtr.Zero;
        System.IntPtr errorPtr = System.IntPtr.Zero;
        reader = API_cwipc_realsense2.cwipc_realsense2(cfg.Realsense2Config.configFilename, ref errorPtr);
        if (reader == System.IntPtr.Zero) {
            string errorMessage = Marshal.PtrToStringAnsi(errorPtr);
            Debug.LogError("PCRealSense2Reader: cwipc_realsense2: " + errorMessage);
            return;
        }
        Debug.Log("xxxjack encName is " + cfg.Bin2Dash.streamName);
        if (cfg.Bin2Dash.streamName != "") {
            API_cwipc_codec.cwipc_encoder_params parms = new API_cwipc_codec.cwipc_encoder_params { octree_bits = cfg.Encoder.octreeBits, do_inter_frame = false, exp_factor = 0, gop_size = 1, jpeg_quality = 75, macroblock_size = 0, tilenumber = 0, voxelsize = 0 };
            encoder = API_cwipc_codec.cwipc_new_encoder(API_cwipc_codec.CWIPC_ENCODER_PARAM_VERSION, ref parms, ref errorPtr);
            if (encoder == System.IntPtr.Zero)
            {
                string errorMessage = Marshal.PtrToStringAnsi(errorPtr);
                Debug.LogError("PCRealSense2Reader: cwipc_new_encoder: " + errorMessage);
                API_cwipc_util.cwipc_source_free(reader);
                reader = System.IntPtr.Zero;
                return;
            }
            Debug.Log($"xxxjack encoder URL {cfg.Bin2Dash.url+ cfg.Bin2Dash.streamName}.mdp segmentSize {cfg.Bin2Dash.segmentSize} segmentLife {cfg.Bin2Dash.segmentLife}");
            // xxxjack allocate bin2dash
            signals_unity_bridge_pinvoke.SetPaths("bin2dash");
            uploader = bin2dash_pinvoke.vrt_create(cfg.Bin2Dash.streamName, bin2dash_pinvoke.VRT_4CC('c', 'w', 'i', '1'), cfg.Bin2Dash.url, cfg.Bin2Dash.segmentSize, cfg.Bin2Dash.segmentLife);
            if (uploader == System.IntPtr.Zero) {
                Debug.LogError("PCRealSense2Reader: vrt_create: failed to create uploader");
                API_cwipc_util.cwipc_source_free(reader);
                reader = System.IntPtr.Zero;
                API_cwipc_codec.cwipc_encoder_free(encoder);
                encoder = System.IntPtr.Zero;
                return;             
            }
//            Debug.Log("xxxjack uploader is" + uploader );
        }
    }

    public override PointCloudFrame get() {
        if (reader == System.IntPtr.Zero) {
            Debug.LogError("PCRealSense2Reader: cwipc.reader == NULL");
            return null;
        }
        var rv = API_cwipc_util.cwipc_source_get(reader);
//        Debug.Log("xxxjack pc=" + rv + ", encoder=" + encoder);
        if (rv == System.IntPtr.Zero) return null;
        //if (encoder != System.IntPtr.Zero) PushToEncoder(rv);
        pointCloudFrame.SetData(rv);
        return pointCloudFrame;
    }

    System.IntPtr encoderPtr;
    int dampedSize = 0;
    System.IntPtr senderPtr;
    int senderSize = 0;
    protected void PushToEncoder(System.IntPtr pc) {
//        Debug.Log("xxxjack PushToEncoder " + pc);
        API_cwipc_codec.cwipc_encoder_feed(encoder, pc);
        if (API_cwipc_codec.cwipc_encoder_available(encoder, true)) {
            unsafe {
                int size = (int)API_cwipc_codec.cwipc_encoder_get_encoded_size(encoder);
                if (dampedSize < size) {
                    dampedSize = (int)(size * Config.Instance.memoryDamping);
                    if (encoderPtr != System.IntPtr.Zero) {
                        Marshal.FreeHGlobal(encoderPtr);
                        Marshal.FreeHGlobal(senderPtr);
                    }
                    encoderPtr = Marshal.AllocHGlobal(dampedSize);
                    senderPtr = Marshal.AllocHGlobal(dampedSize);
                }
                bool ok = API_cwipc_codec.cwipc_encoder_copy_data(encoder, encoderPtr, (System.IntPtr)dampedSize);
                if (ok) {
                    lock(this) {
                        while (senderSize > 0) { Task.Delay(1); }
                        System.Buffer.MemoryCopy(encoderPtr.ToPointer(), senderPtr.ToPointer(), dampedSize, senderSize);
                        senderSize = size;
                    }
                    // xxxjack pass 2 bin2dash
                    //Debug.Log("xxxjack Pushing " + size);
                    if (!ok)  Debug.LogError("PCRealSense2Reader: vrt_push_buffer returned false");
                } else {
                    Debug.LogError("PCRealSense2Reader: cwipc_encoder_copy_data returned false");
                }
                
            }
        }
    }

    public override void update() { 
        lock (this) {
            if (senderSize == 0) return;
//            Debug.Log($"bin2dash Send {senderSize}");
        }
        bool ok = bin2dash_pinvoke.vrt_push_buffer(uploader, senderPtr, (uint)senderSize);
        senderSize = 0;
        if (!ok)
        {
            Debug.LogError("PCRealSense2Reader: vrt_push_buffer returned false");
        }
    }

    public override void free() {
        if (encoderPtr != System.IntPtr.Zero) { Marshal.FreeHGlobal(encoderPtr); encoderPtr = System.IntPtr.Zero; }
        if (senderPtr != System.IntPtr.Zero) { Marshal.FreeHGlobal(senderPtr); senderPtr = System.IntPtr.Zero; }
        if (encoder != System.IntPtr.Zero) { API_cwipc_codec.cwipc_encoder_free(encoder); encoder= System.IntPtr.Zero; }
        base.free();
    }


}
