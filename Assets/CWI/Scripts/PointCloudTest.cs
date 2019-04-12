using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;


public class PointCloudTest : MonoBehaviour {

    ComputeBuffer pointBuffer;
    Mesh mesh;
    bool useMesh = true;

    void OnDisable() {
        if (pointBuffer != null) {
            pointBuffer.Release();
            pointBuffer = null;
        }
    }

    public float pointSize = 0.05f;
    float _pointSize = 0;
    public Color pointTint = Color.white;
    Color _pointTint = Color.clear;

    IEnumerator Start() {
        // useMesh = SystemInfo.graphicsShaderLevel < 50;
        if (useMesh)
        {
            var mf = gameObject.AddComponent<MeshFilter>();
            var mr = gameObject.AddComponent<MeshRenderer>();
            mf.mesh = mesh = new Mesh();
            if (pointMaterial == null)
            {
                pointMaterial = new Material(pointShader40);
                pointMaterial.hideFlags = HideFlags.DontSave;
            }
            mr.material = pointMaterial;

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        yield return null;
        var pcs = cwipc_util_pinvoke.GetPointCloudFromPly();
        //var pcs = cwipc_util_pinvoke.GetPointCloudFromCWICPC(Config.Instance.PCs.filename);

        if (useMesh)
            cwipc_util_pinvoke.UpdatePointBuffer(pcs, ref mesh);
        else
            cwipc_util_pinvoke.UpdatePointBuffer(pcs, ref pointBuffer);
    }

    void Update() {
        if ( Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public Shader pointShader = null;
    public Shader pointShader40 = null;
    Material pointMaterial;

    void OnRenderObject() {
        if (useMesh) return;

        if (pointBuffer==null || !pointBuffer.IsValid()) return;

        var camera = Camera.current;
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;

        if (camera.name == "Preview Scene Camera") return;

        // TODO: Do view frustum culling here.

        if (pointMaterial == null) {
            pointMaterial = new Material(pointShader);
            pointMaterial.hideFlags = HideFlags.DontSave;
            pointMaterial.SetBuffer("_PointBuffer", pointBuffer);
        }

        pointMaterial.SetPass(0);
        pointMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
        if (_pointTint != pointTint) { _pointTint = pointTint; pointMaterial.SetColor("_Tint", _pointTint); }
        if (_pointSize != pointSize) { _pointSize = pointSize; pointMaterial.SetFloat("_PointSize", _pointSize); }
        Graphics.DrawProcedural(MeshTopology.Points, pointBuffer.count, 1);
    }

}
