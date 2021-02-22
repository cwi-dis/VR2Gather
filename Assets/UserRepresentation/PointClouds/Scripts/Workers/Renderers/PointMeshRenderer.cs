using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
    public class PointMeshRenderer : MonoBehaviour
    {
        Material material;
        Mesh mesh;
        MeshPreparer preparer;

        // Start is called before the first frame update
        void Start()
        {
            if (material == null)
            {
                var _material = Resources.Load<Material>("PointCloudsMesh");
                material = new Material(_material);
            }
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        public string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public void SetPreparer(MeshPreparer _preparer)
        {
            if (preparer != null)
            {
                Debug.LogError($"Programmer error: {Name()}: attempt to set second preparer");
            }
            preparer = _preparer;
        }

        private void Update()
        {
            if (preparer == null) return;
            float pointSize = preparer.GetPointSize();
            material.SetFloat("_PointSize", pointSize);
            if (mesh == null) return;
            preparer.GetMesh(ref mesh); // <- Bottleneck
            statsUpdate(mesh.vertexCount, pointSize, preparer.currentTimestamp);
        }

        public void OnRenderObject()
        {
            if (material.SetPass(0))
            {
                Graphics.DrawMeshNow(mesh, transform.localToWorldMatrix);
            }
        }

        public void OnDestroy()
        {
            if (material != null) { material = null; }
        }

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        System.DateTime statsLastTime;
        double statsTotalMeshCount = 0;
        double statsTotalVertexCount = 0;
        double statsTotalPointSize = 0;
        const int statsInterval = 10;

        public void statsUpdate(int vertexCount, float pointSize, ulong timestamp)
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            if (statsLastTime == null)
            {
                statsLastTime = System.DateTime.Now;
                statsTotalMeshCount = 0;
                statsTotalVertexCount = 0;
                statsTotalPointSize = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(statsInterval))
            {
                Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={Name()}, fps={statsTotalMeshCount / statsInterval}, vertices_per_mesh={(int)(statsTotalVertexCount / (statsTotalMeshCount == 0 ? 1 : statsTotalMeshCount))}, pc_timestamp={timestamp}, avg_pointsize={(statsTotalPointSize / (statsTotalMeshCount == 0 ? 1 : statsTotalMeshCount))}, pc_latency_ms={(ulong)sinceEpoch.TotalMilliseconds - timestamp}");
                statsTotalMeshCount = 0;
                statsTotalVertexCount = 0;
                statsTotalPointSize = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalVertexCount += vertexCount;
            statsTotalMeshCount += 1;
            statsTotalPointSize += pointSize;
        }
    }
}
