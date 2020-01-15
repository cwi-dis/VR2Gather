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
                material = new Material(Resources.Load<Shader>("PointCloudMesh"));
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

        void OnRenderObject() {
            if (preparer == null) return;
            material.SetFloat("_PointSize", preparer.GetPointSize());
            preparer.GetMesh(ref mesh); // <- Bottleneck

            if (mesh == null ) return;

            var camera = Camera.current;
            if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
            if (camera.name == "Preview Scene Camera") return;
            // TODO: Do view frustum culling here.
            material.SetPass(0);
            Graphics.DrawMeshNow(mesh, transform.localToWorldMatrix);
        }


        public void OnDestroy() {
            if (material != null) { Destroy(material); material = null; }
        }
    }

}