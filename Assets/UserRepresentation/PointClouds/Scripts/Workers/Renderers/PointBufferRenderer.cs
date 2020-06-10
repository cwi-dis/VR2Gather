using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class PointBufferRenderer : MonoBehaviour
    {
        ComputeBuffer           pointBuffer;
        int                     pointCount = 0;
        Material                material;
        MaterialPropertyBlock   block;
        public Workers.BufferPreparer preparer;

        // Start is called before the first frame update
        void Start() {
            if (material == null)  material = Resources.Load<Material>("PointClouds");
            block = new MaterialPropertyBlock();
        }

        private void Update() {
            pointCount = preparer.GetComputeBuffer(ref pointBuffer);
            if (pointCount == 0 || pointBuffer == null || !pointBuffer.IsValid()) return;
            block.SetBuffer("_PointBuffer", pointBuffer);
            block.SetFloat("_PointSize", preparer.GetPointSize());
            block.SetMatrix("_Transform", transform.localToWorldMatrix);

            Graphics.DrawProcedural(material, new Bounds(transform.position, Vector3.one*2), MeshTopology.Points, pointCount, 1, null, block);
            statsUpdate(pointCount);
        }
     
        public void OnDestroy() {
            if (pointBuffer != null) { pointBuffer.Release(); pointBuffer = null; }
            if (material != null) { Destroy(material); material = null; }
        }

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        System.DateTime statsLastTime;
        double statsTotalPointcloudCount;
        double statsTotalPointCount;

        public void statsUpdate(int pointCount)
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            if (statsLastTime == null)
            {
                statsLastTime = System.DateTime.Now;
                statsTotalPointcloudCount = 0;
                statsTotalPointCount = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(10))
            {
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: PointBufferRenderer#{instanceNumber}: {statsTotalPointcloudCount / 10} fps, {(int)(statsTotalPointCount / statsTotalPointcloudCount)} points per cloud");
                statsTotalPointcloudCount = 0;
                statsTotalPointCount = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalPointCount += pointCount;
            statsTotalPointcloudCount += 1;
        }
    }
}
