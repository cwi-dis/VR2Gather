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
                material = new Material(Resources.Load<Shader>("PointCloudBuffer"));
                material.SetFloat("_PointSize", 0.008f );
                material.SetColor("_Tint", Color.gray);
                material.hideFlags = HideFlags.DontSave;
            }
        }


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

            Graphics.DrawProcedural(MeshTopology.Points, pointCount, 1);
        }

        public void OnDestroy() {
            if (pointBuffer != null) { pointBuffer.Release(); pointBuffer = null; }
            if (material != null) { Destroy(material); material = null; }
        }
    }

}