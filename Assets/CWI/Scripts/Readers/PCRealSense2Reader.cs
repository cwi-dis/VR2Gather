using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class PCRealSense2Reader : PCSyntheticReader
{
    cwipc.encoder encoder;
    protected bin2dash.connection uploader;

    // Start is called before the first frame update
    public PCRealSense2Reader(Config._User cfg)
    {
        var b2d = cfg.PCSelfConfig.Bin2Dash;
        encoder = null;
        uploader = null;
        System.IntPtr errorPtr = System.IntPtr.Zero;
        reader = cwipc.realsense2(cfg.PCSelfConfig.configFilename);
        if (reader == null) {
            Debug.LogError("PCRealSense2Reader: cwipc_realsense2: cannot create reader"); // Should not happen, should thorw an exception
            return;
        }
        if (b2d.streamName != "") {
            cwipc.encoder_params parms = new cwipc.encoder_params { octree_bits = cfg.PCSelfConfig.Encoder.octreeBits, do_inter_frame = false, exp_factor = 0, gop_size = 1, jpeg_quality = 75, macroblock_size = 0, tilenumber = 0, voxelsize = 0 };
            encoder = cwipc.new_encoder(parms); 
            if (encoder == null)
            {
                Debug.LogError("PCRealSense2Reader: cwipc_new_encoder: could not create"); // Should not happen, should throw exception
                return;
            }
            uploader = bin2dash.create(b2d.streamName, bin2dash.VRT_4CC('c', 'w', 'i', '1'), b2d.url, b2d.segmentSize, b2d.segmentLife);
            if (uploader == null) {
                Debug.LogError("PCRealSense2Reader: vrt_create: failed to create uploader");
                // If we have no uploader we need no encoder either...
                encoder = null;
                return;             
            }
        }
    }

    ~PCRealSense2Reader()
    {
        reader = null;
        encoder = null;
        uploader = null;
        if (encoderPtr != System.IntPtr.Zero) { Marshal.FreeHGlobal(encoderPtr); encoderPtr = System.IntPtr.Zero; }
    }

    public override PointCloudFrame get() {
        if (reader == null) {
            Debug.LogError("PCRealSense2Reader: cwipc.reader == NULL");
            return null;
        }
        cwipc.pointcloud pc = reader.get();
        if (pc == null) return null;
        PushToEncoder(pc);
        PointCloudFrame pointCloudFrame = new PointCloudFrame();
        pointCloudFrame.SetData(pc);
        return pointCloudFrame;
    }

    System.IntPtr encoderPtr;
    int dampedSize = 0;
    cwipc.pointcloud pcWaitingForUpload;

    protected void PushToEncoder(cwipc.pointcloud pc)
    {
        if (encoder == null || uploader == null) return;
        lock (this)
        {
            if (pcWaitingForUpload != null)
            {
                // An earlier pointcloud was still waiting for upload.
                Debug.LogWarning("PCRealSense2Reader: overriding old pointcloud waiting for upload with new one");
            }
            pcWaitingForUpload = pc;

        }
    }

    void encodeAndUpload()
    {
        cwipc.pointcloud pc;
        lock (this)
        {
            if (pcWaitingForUpload == null) return;
            pc = pcWaitingForUpload;
            pcWaitingForUpload = null;
        }
        encoder.feed(pc);
        if (!encoder.available(true))
        {
            Debug.LogError("PCRealSense2Reader: pushed pointcloud to the encoder but it did not produce encoded data");
            return;
        }
        unsafe
        {
            int size = encoder.get_encoded_size();
            if (dampedSize < size)
            {
                dampedSize = (int)(size * Config.Instance.memoryDamping);
                if (encoderPtr != System.IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(encoderPtr);
                }
                encoderPtr = Marshal.AllocHGlobal(dampedSize);
            }
            bool ok = encoder.copy_data(encoderPtr, size);
            if (!ok) {
                Debug.LogError("PCRealSense2Reader: encoder.available() returned true but copy_data returned false");
                return;
            }
            ok = uploader.push_buffer(encoderPtr, (uint)size);
            if (!ok)
            {
                Debug.LogError("PCRealSense2Reader: vrt_push_buffer returned false");
                return;
            }
        }
    }

    public override void update() {
        encodeAndUpload();
    }
}
