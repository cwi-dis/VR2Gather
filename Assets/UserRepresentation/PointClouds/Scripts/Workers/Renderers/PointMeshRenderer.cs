using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
    public class PointMeshRenderer : MonoBehaviour
    {
        public Material material;
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
            stats = new Stats(Name());
        }

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
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
            preparer.Synchronize();
        }
        private void LateUpdate()
        {
            if (preparer == null) return;
            preparer.LatchFrame();
            float pointSize = preparer.GetPointSize();
            material.SetFloat("_PointSize", pointSize);
            if (mesh == null) return;
            preparer.GetMesh(ref mesh); // <- Bottleneck
            stats.statsUpdate(mesh.vertexCount, pointSize, preparer.currentTimestamp);
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

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalPointcloudCount = 0;
            double statsTotalVertexCount = 0;
            double statsTotalPointSize = 0;

            public void statsUpdate(int vertexCount, float pointSize, ulong timestamp)
            {
     
                statsTotalVertexCount += vertexCount;
                statsTotalPointcloudCount += 1;
                statsTotalPointSize += pointSize;

                if (ShouldOutput())
                {
                    System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                    Output($"fps={statsTotalPointcloudCount / Interval():F2}, points_per_cloud={(int)(statsTotalVertexCount / (statsTotalPointcloudCount == 0 ? 1 : statsTotalPointcloudCount))}, avg_pointsize={(statsTotalPointSize / (statsTotalPointcloudCount == 0 ? 1 : statsTotalPointcloudCount)):G4}, framenumber={UnityEngine.Time.frameCount}, pc_timestamp={timestamp}, pc_latency_ms={(long)sinceEpoch.TotalMilliseconds - (long)timestamp}");
                 }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPointcloudCount = 0;
                    statsTotalVertexCount = 0;
                    statsTotalPointSize = 0;
                }
            }

        }
        protected Stats stats;
    }
}
