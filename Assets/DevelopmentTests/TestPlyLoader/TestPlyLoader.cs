using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class PlyLoader : MonoBehaviour {
    ComputeBuffer computeBuffer;
    public string fileName = "Venice/longdress_vox10_1062";
    public float  pointSize = 0.004f;
    public Material material;
    public Color color = new Color(0.20f, 0.20f, 0.20f);
    int vertexCount;

    public struct Point {
        public float x;
        public float y;
        public float z;
        public Color32 color;
    };

    // Start is called before the first frame update
    void Start() {
        byte[] bytes = System.IO.File.ReadAllBytes($"{Application.streamingAssetsPath}/{fileName}");
        vertexCount = bytes.Length / 16;
        computeBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 4);
        computeBuffer.SetData(bytes);

        if (material == null) {
            material = new Material(Shader.Find("Entropy/PointCloud"));
            material.SetFloat("_PointSize", pointSize);
            material.SetColor("_Tint", color);
            material.hideFlags = HideFlags.DontSave;
        }

    }


    void OnRenderObject() {
        var camera = Camera.current;
        if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
        if (camera.name == "Preview Scene Camera") return;

        // TODO: Do view frustum culling here.
        material.SetBuffer("_PointBuffer", computeBuffer);
        material.SetMatrix("_Transform", transform.localToWorldMatrix);
        material.SetPass(0);

        Graphics.DrawProceduralNow(MeshTopology.Points, vertexCount, 1);
    }

    public void OnDestroy() {
        computeBuffer?.Release();
        if (material != null) { Destroy(material); material = null; }
    }
}
