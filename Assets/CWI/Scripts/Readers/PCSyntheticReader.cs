using UnityEngine;

public class PCSyntheticReader : PCBaseReader
{
    protected System.IntPtr obj;

    public PCSyntheticReader()
    {
        obj = API_cwipc_util.cwipc_synthetic();
    }

    public bool eof() {
        if (obj == System.IntPtr.Zero) return true;
        return API_cwipc_util.cwipc_source_eof(obj);
    }

    public bool available(bool wait) {
        if (obj == System.IntPtr.Zero) return false;
        return API_cwipc_util.cwipc_source_available(obj, wait);
    }

    public void free()
    {
        if (obj == System.IntPtr.Zero) return;
        API_cwipc_util.cwipc_source_free(obj);
        obj = System.IntPtr.Zero;
    }

    public PointCloudFrame get() {
        if (obj == System.IntPtr.Zero) {
            Debug.LogError("cwipc.obj == NULL");
            return null;
        }
        var rv = API_cwipc_util.cwipc_source_get(obj);
        if (rv == System.IntPtr.Zero) return null;
        return new PointCloudFrame(rv);
    }
}
