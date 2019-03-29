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
    float _pointSize = 0;
    public Color pointTint = Color.white;
    Color _pointTint = Color.clear;

    IEnumerator Start() {
        yield return null;
        var pcs = cwipc_util_pinvoke.GetPointCloudFromCWICPC(Config.Instance.PCs.filename);
        cwipc_util_pinvoke.UpdatePointBuffer(pcs, ref _pointBuffer);
    }

    void Update() {
        if ( Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public Shader   _pointShader    = null;
    Material _pointMaterial;

    void OnRenderObject() {
        if (_pointBuffer==null || !_pointBuffer.IsValid()) return;

        var camera = Camera.current;
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;

        if (camera.name == "Preview Scene Camera") return;

        // TODO: Do view frustum culling here.

        if (_pointMaterial == null) {
            _pointMaterial = new Material(_pointShader);
            _pointMaterial.hideFlags = HideFlags.DontSave;
            _pointMaterial.SetBuffer("_PointBuffer", _pointBuffer);
        }

        _pointMaterial.SetPass(0);
        _pointMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
        if (_pointTint != pointTint) { _pointTint = pointTint; _pointMaterial.SetColor("_Tint", _pointTint); }
        if (_pointSize != pointSize) { _pointSize = pointSize; _pointMaterial.SetFloat("_PointSize", _pointSize); }
        Graphics.DrawProcedural(MeshTopology.Points, _pointBuffer.count, 1);
    }

}
