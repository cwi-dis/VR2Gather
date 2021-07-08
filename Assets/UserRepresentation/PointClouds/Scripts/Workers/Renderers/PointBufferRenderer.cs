using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
    public class PointBufferRenderer : MonoBehaviour
    {
        ComputeBuffer pointBuffer;
        int pointCount = 0;
        public Material material;
        MaterialPropertyBlock block;
        BufferPreparer preparer;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }


        // Start is called before the first frame update
        void Start()
        {
            if (material == null)
            {
                var _material = Resources.Load<Material>("PointCloudsBuffer");
                material = new Material(_material);
            }
            block = new MaterialPropertyBlock();
            stats = new Stats(Name());
        }

        public void SetPreparer(BufferPreparer _preparer)
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
            bool fresh = preparer.LatchFrame();
            pointCount = preparer.GetComputeBuffer(ref pointBuffer);
            float pointSize = preparer.GetPointSize();
            if (pointCount == 0 || pointBuffer == null || !pointBuffer.IsValid()) return;
            block.SetBuffer("_PointBuffer", pointBuffer);
            block.SetFloat("_PointSize", pointSize);
            block.SetMatrix("_Transform", transform.localToWorldMatrix);

            Graphics.DrawProcedural(material, new Bounds(transform.position, Vector3.one * 2), MeshTopology.Points, pointCount, 1, null, block);
            stats.statsUpdate(pointCount, pointSize, preparer.currentTimestamp, preparer.getQueueSize(), fresh);
        }

        public void OnDestroy()
        {
            if (pointBuffer != null) { pointBuffer.Release(); pointBuffer = null; }
            if (material != null) { material = null; }
        }


        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalPointcloudCount = 0;
            double statsTotalDisplayCount = 0;
            double statsTotalPointCount = 0;
            double statsTotalDisplayPointCount = 0;
            double statsTotalPointSize = 0;
            int statsMaxQueueSize = 0;

            public void statsUpdate(int pointCount, float pointSize, ulong timestamp, int queueSize, bool fresh)
            {
    
                statsTotalDisplayPointCount += pointCount;
                statsTotalDisplayCount += 1;
                if (!fresh) return; //remember to commit with this tag backport candidate
                if (fresh)
                {
                    statsTotalPointcloudCount += 1;
                    statsTotalPointCount += pointCount;
                }
                statsTotalPointSize += pointSize;
                if (queueSize > statsMaxQueueSize) statsMaxQueueSize = queueSize;
 
                if (ShouldOutput())
                {
                    System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                    Output($"fps={statsTotalPointcloudCount / Interval():F2}, fps_display={statsTotalDisplayCount / Interval():F2}, points_per_cloud={(int)(statsTotalPointCount / (statsTotalPointcloudCount == 0 ? 1 : statsTotalPointcloudCount))}, points_per_display={(int)(statsTotalDisplayPointCount / (statsTotalDisplayCount == 0 ? 1 : statsTotalDisplayCount))}, avg_pointsize={(statsTotalPointSize / (statsTotalPointcloudCount == 0 ? 1 : statsTotalPointcloudCount)):G4}, max_queuesize={statsMaxQueueSize}, framenumber={UnityEngine.Time.frameCount},  pc_timestamp={timestamp}, pc_latency_ms={(long)sinceEpoch.TotalMilliseconds - (long)timestamp}");
                  }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPointcloudCount = 0;
                    statsTotalDisplayCount = 0;
                    statsTotalDisplayPointCount = 0;
                    statsTotalPointCount = 0;
                    statsTotalPointSize = 0;
                    statsMaxQueueSize = 0;
                }
            }
        }

        protected Stats stats;
    }
}
