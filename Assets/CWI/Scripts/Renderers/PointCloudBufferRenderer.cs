using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using System.Threading.Tasks;
using System.Threading;

public class PointCloudBufferRenderer : PointCloudBaseRenderer {
    ComputeBuffer   pointBuffer;
    int             pointCount = 0;

    public override void Init(Config._PCs cfg) { InternalInit(cfg, Resources.Load<Shader>("PointCloudBuffer")); }

    protected override void OnData() { frameReady.getByteArray(); }

    protected override void OnUpdate() { pointCount = frameReady.load_to_pointbuffer(ref pointBuffer); }

    void OnRenderObject() {
        if (pointCount == 0 || pointBuffer ==null || !pointBuffer.IsValid() ) return;
        var camera = Camera.current;
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
        if (camera.name == "Preview Scene Camera") return;

        // TODO: Do view frustum culling here.
        material.SetBuffer("_PointBuffer", pointBuffer);
        material.SetMatrix("_Transform", transform.localToWorldMatrix);
        material.SetPass(0);
        
        Graphics.DrawProcedural(MeshTopology.Points, pointCount, 1);
    }

    public override void OnDisable() {
        base.OnDisable();

        if (pointBuffer != null) {
            pointBuffer.Release();
            pointBuffer = null;
        }
    }
}
