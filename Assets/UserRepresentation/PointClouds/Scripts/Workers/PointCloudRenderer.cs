using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class PointCloudRenderer : MonoBehaviour
    {
        // For reasons I don't understand pointclouds need to be mirrored in the X direction.
        // Doing this on the GameObject.transform has the drawback that coordinate systems
        // become mirrored, for example when cropping a pointcloud. Therefore, we mirror here,
        // by adjusting the matrix.
        const bool pcMirrorX = true;
        ComputeBuffer pointBuffer;
        int pointCount = 0;
        static Material baseMaterial;
        public Material material;
        public bool paused = false;
        MaterialPropertyBlock block;
        PointCloudPreparer preparer;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public static bool isSupported()
        {
            if (baseMaterial != null) return true;
            baseMaterial = Resources.Load<Material>("PointCloud");
            if (baseMaterial == null) return false;
            return baseMaterial.shader.isSupported;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (!isSupported())
            {
                Debug.LogError($"{Name()}: uses shader that is not supported on this graphics card");
            }
            material = new Material(baseMaterial);
            block = new MaterialPropertyBlock();
            stats = new Stats(Name());
        }

        public void PausePlayback(bool _paused)
        {
            paused = _paused;
        }

        public void SetPreparer(PointCloudPreparer _preparer)
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
            if (paused) return;
            float pointSize = 0;
            if (fresh)
            {
                pointCount = preparer.GetComputeBuffer(ref pointBuffer);
                pointSize = preparer.GetPointSize();
                if (pointCount == 0 || pointBuffer == null || !pointBuffer.IsValid()) return;
                block.SetBuffer("_PointBuffer", pointBuffer);
                block.SetFloat("_PointSize", pointSize);
            }
            if (pointCount == 0 || pointBuffer == null || !pointBuffer.IsValid()) return;
            Matrix4x4 pcMatrix = transform.localToWorldMatrix;
            if (pcMirrorX)
            {
                pcMatrix = pcMatrix * Matrix4x4.Scale(new Vector3(-1, 1, 1));
            }
            block.SetMatrix("_Transform", pcMatrix);
            Graphics.DrawProcedural(material, new Bounds(transform.position, Vector3.one * 2), MeshTopology.Points, pointCount, 1, null, block);
            stats.statsUpdate(pointCount, pointSize, preparer.currentTimestamp, preparer.getQueueDuration(), fresh);
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
            double statsTotalQueueDuration = 0;

            public void statsUpdate(int pointCount, float pointSize, Timestamp timestamp, Timedelta queueDuration, bool fresh)
            {
    
                statsTotalDisplayPointCount += pointCount;
                statsTotalDisplayCount += 1;
                if (!fresh)
                {
                    // If this was just a re-display of a previously received pointcloud we don't need the rest of the data.
                    return;
                }
                statsTotalPointcloudCount += 1;
                statsTotalPointCount += pointCount;
                statsTotalPointSize += pointSize;
                statsTotalQueueDuration += queueDuration;

                if (ShouldOutput())
                {
                    System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                    double factor = statsTotalPointcloudCount == 0 ? 1 : statsTotalPointcloudCount;
                    double display_factor = statsTotalDisplayCount == 0 ? 1 : statsTotalDisplayCount;
                    Timedelta latency = timestamp > 0 ? (Timestamp)sinceEpoch.TotalMilliseconds - timestamp : 0;
                    Output($"fps={statsTotalPointcloudCount / Interval():F2}, latency_ms={latency}, fps_display={statsTotalDisplayCount / Interval():F2}, points_per_cloud={(int)(statsTotalPointCount / factor)}, points_per_display={(int)(statsTotalDisplayPointCount / display_factor)}, avg_pointsize={(statsTotalPointSize / factor):G4}, renderer_queue_ms={(int)(statsTotalQueueDuration / factor)}, framenumber={UnityEngine.Time.frameCount},  timestamp={timestamp}");
                  }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalPointcloudCount = 0;
                    statsTotalDisplayCount = 0;
                    statsTotalDisplayPointCount = 0;
                    statsTotalPointCount = 0;
                    statsTotalPointSize = 0;
                    statsTotalQueueDuration = 0;
                }
            }
        }

        protected Stats stats;
    }
}
