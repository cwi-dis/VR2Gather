using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class PointBufferRenderer : MonoBehaviour
    {
        ComputeBuffer   pointBuffer;
        int             pointCount = 0;
        Material        material;
        public Workers.BufferPreparer preparer;

        // Start is called before the first frame update
        void Start() {
            if (material == null) {
                material = new Material(Shader.Find("Entropy/PointCloud"));
                //material = new Material(Resources.Load<Shader>("PointCloudBuffer"));
                material.SetFloat("_PointSize", 0.008f );
                material.SetColor("_Tint", Color.gray);
                material.hideFlags = HideFlags.DontSave;
            }
        }

        private void Update() {
            material.SetFloat("_PointSize", preparer.GetPointSize());
            pointCount = preparer.GetComputeBuffer(ref pointBuffer);
            if (pointCount == 0 || pointBuffer == null || !pointBuffer.IsValid()) return;

            // TODO: Do view frustum culling here.
            material.SetBuffer("_PointBuffer", pointBuffer);
            material.SetMatrix("_Transform", transform.localToWorldMatrix);

            Graphics.DrawProcedural(material, new Bounds(transform.position, Vector3.one*2), MeshTopology.Points, pointCount, 1);
            statsUpdate(pointCount);
        }
        /*
        void OnRenderObject() {
            material.SetFloat("_PointSize", preparer.GetPointSize());
            pointCount = preparer.GetComputeBuffer(ref pointBuffer);
            if (pointCount == 0 || pointBuffer == null || !pointBuffer.IsValid()) return;
            var camera = Camera.current;
            if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
            if (camera.name == "Preview Scene Camera") return;

            // TODO: Do view frustum culling here.
            material.SetBuffer("_PointBuffer", pointBuffer);
            material.SetMatrix("_Transform", transform.localToWorldMatrix);
            material.SetPass(0);

            Graphics.DrawProceduralNow(MeshTopology.Points, pointCount, 1);
            statsUpdate(pointCount);
        }
        */
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
