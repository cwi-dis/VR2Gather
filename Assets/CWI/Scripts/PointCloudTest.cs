using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;


public class PointCloudTest : MonoBehaviour {

    ComputeBuffer _pointBuffer;
    void OnDisable() {
        if (_pointBuffer != null) {
            _pointBuffer.Release();
            _pointBuffer = null;
        }
    }

    public float pointSize = 0.05f;

    void Start() {
        var pcs = cwipc_util_pinvoke.GetPointCludStream();
        cwipc_util_pinvoke.UpdatePointBuffer( pcs, ref _pointBuffer );
    }

    public Shader   _pointShader    = null;
    public Color    _pointTint      = new Color(0.5f, 0.5f, 0.5f, 1);
    Material        _pointMaterial;

    void OnRenderObject() {
        if (_pointBuffer!=null && !_pointBuffer.IsValid()) return;

        var camera = Camera.current;
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;

//        if (camera.name == "Preview Scene Camera") return;

        // TODO: Do view frustum culling here.

        if (_pointMaterial == null) {
            _pointMaterial = new Material(_pointShader);
            _pointMaterial.hideFlags = HideFlags.DontSave;
        }

        _pointMaterial.SetPass(0);
        _pointMaterial.SetColor("_Tint", _pointTint);
        _pointMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
        _pointMaterial.SetBuffer("_PointBuffer", _pointBuffer);
        _pointMaterial.SetFloat("_PointSize", pointSize);
        Graphics.DrawProcedural(MeshTopology.Points, _pointBuffer.count, 1);
    }

}
