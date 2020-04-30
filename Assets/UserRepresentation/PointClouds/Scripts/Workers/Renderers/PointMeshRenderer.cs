using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class PointMeshRenderer : MonoBehaviour
    {
        Material        material;
        Mesh            mesh;

        public Workers.MeshPreparer preparer;

        // Start is called before the first frame update
        void Start() {
            if (material == null) {
                material = new Material(Shader.Find("Entropy/PointCloud40"));
                //material = new Material(Resources.Load<Shader>("PointCloudMesh"));
                material.SetFloat("_PointSize", 0.008f );
                material.SetColor("_Tint", Color.gray);
                material.hideFlags = HideFlags.DontSave;
            }
            var mf = gameObject.AddComponent<MeshFilter>();
            var mr = gameObject.AddComponent<MeshRenderer>();
            mesh = mf.mesh = new Mesh();
            mf.mesh.MarkDynamic();
            
            mf.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mr.material = material;

        }

        private void Update() {
            if (preparer == null) return;
            material.SetFloat("_PointSize", preparer.GetPointSize());
            if (mesh == null) return;
            preparer.GetMesh(ref mesh); // <- Bottleneck
            Graphics.DrawMesh(mesh, transform.localToWorldMatrix, material, 0);
        }


        public void OnDestroy() {
            if (material != null) { Destroy(material); material = null; }
        }
    }

}