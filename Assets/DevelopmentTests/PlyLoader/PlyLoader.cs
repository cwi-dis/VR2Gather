using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class PlyLoader : MonoBehaviour {
    ComputeBuffer computeBuffer;
    public string fileName = "Venice/longdress_vox10_1062";
    public float  pointSize = 0.004f;
    public Material material;
    int vertexCount;

    public struct Point {
        public float x;
        public float y;
        public float z;
        public Color32 color;
    };

    // Start is called before the first frame update
    void Start() {
        string filename = $"{Application.streamingAssetsPath}/{fileName}";
        /*
        string[] lines = System.IO.File.ReadAllLines(filename+".ply");
        string[] values = lines[3].Split(' ');
        float scale = float.Parse(values[2], CultureInfo.InvariantCulture);
        values = lines[4].Split(' ');
        Vector3 traslation = new Vector3(float.Parse(values[2], CultureInfo.InvariantCulture), float.Parse(values[3], CultureInfo.InvariantCulture), float.Parse(values[4], CultureInfo.InvariantCulture));
        traslation *= scale;
        values = lines[6].Split(' ');
        vertexCount = int.Parse(values[2]);
        Debug.Log($"elements {vertexCount} lines {lines.Length}");
        Point[] points = new Point[vertexCount];
        for (int i = 0; i < vertexCount; ++i) {
            try {
                values = lines[i + 14].Split(' ');
                points[i].x = ((float.Parse(values[0], CultureInfo.InvariantCulture) * scale) + traslation.x) * 0.01f;
                points[i].y = ((float.Parse(values[1], CultureInfo.InvariantCulture) * scale) + traslation.y) * 0.01f;
                points[i].z = ((float.Parse(values[2], CultureInfo.InvariantCulture) * scale) + traslation.z) * 0.01f;
                points[i].color = new Color32(byte.Parse(values[3]), byte.Parse(values[4]), byte.Parse(values[5]), 1);
            } catch{
                Debug.Log($"lines[{i+14}/{vertexCount}] {lines[i + 14]} total elements {points.Length}");
            }
        }
        int offset = 0;
        byte[] bytes = new byte[vertexCount * 16];
        for (int i = 0; i < vertexCount; ++i) {
            System.BitConverter.GetBytes(points[i].x).CopyTo(bytes, offset); offset += 4;
            System.BitConverter.GetBytes(points[i].y).CopyTo(bytes, offset); offset += 4;
            System.BitConverter.GetBytes(points[i].z).CopyTo(bytes, offset); offset += 4;
            bytes[offset++] = points[i].color.r;
            bytes[offset++] = points[i].color.g;
            bytes[offset++] = points[i].color.b;
            bytes[offset++] = points[i].color.a;
        }
        System.IO.File.WriteAllBytes(filename+".pcs", bytes);
        */

        byte[] bytes = System.IO.File.ReadAllBytes(filename + ".pcs");
        vertexCount = bytes.Length / 16;
        computeBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 4);
        computeBuffer.SetData(bytes);

        if (material == null) {
            material = new Material(Shader.Find("Entropy/PointCloud"));
            //material = new Material(Resources.Load<Shader>("PointCloudBuffer"));
            material.SetFloat("_PointSize", pointSize);
            material.SetColor("_Tint", Color.gray);
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
