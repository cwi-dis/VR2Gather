using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class PointMeshRenderer : MonoBehaviour
    {
        Material        material;
        Mesh            mesh;
        Workers.MeshPreparer preparer;

        // Start is called before the first frame update
        void Start() {
            if (material == null) material = Resources.Load<Material>("PointCloudsMesh");
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        public void AddPreparer(Workers.MeshPreparer _preparer)
        {
            if (preparer != null)
            {
                Debug.LogWarning("PointMeshRenderer: ignoring second preparer (temporary)");
            }
            preparer = _preparer;
        }

        private void Update() {
            if (preparer == null) return;
            material.SetFloat("_PointSize", preparer.GetPointSize());
            if (mesh == null) return;
            preparer.GetMesh(ref mesh); // <- Bottleneck

            statsUpdate(mesh.vertexCount);
        }

        public void OnRenderObject()
        {
            if (material.SetPass(0))
            {
                Graphics.DrawMeshNow(mesh, transform.localToWorldMatrix);
            }
        }

        public void OnDestroy() {
            if (material != null) { material = null; }
        }

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        System.DateTime statsLastTime;
        double statsTotalMeshCount;
        double statsTotalVertexCount;

        public void statsUpdate(int vertexCount)
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            if (statsLastTime == null)
            {
                statsLastTime = System.DateTime.Now;
                statsTotalMeshCount = 0;
                statsTotalVertexCount = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(10))
            {
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: PointMeshRenderer#{instanceNumber}: {statsTotalMeshCount / 10} fps, {(int)(statsTotalVertexCount / statsTotalMeshCount)} vertices per mesh");
                statsTotalMeshCount = 0;
                statsTotalVertexCount = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalVertexCount += vertexCount;
            statsTotalMeshCount += 1;
        }
    }
}
