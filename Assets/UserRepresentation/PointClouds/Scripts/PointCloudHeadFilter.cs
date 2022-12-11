using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
     public class PointCloudHeadFilter : MonoBehaviour
    {
        public bool headOnly;
        public GameObject pc;
        public HeadPosition head;
        public bool invertX = true;
        public bool invertZ = false;
        public bool drawCenterSphere = false;
        Vector3 _debugCenter;
       
        // Start is called before the first frame update
        void Start()
        {

        }

        void OnDrawGizmos()
        {
            if (headOnly && drawCenterSphere)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(_debugCenter, 0.1f);
            }
        }

        // Update is called once per frame
        void Update()
        {
            
            if (head == null || pc == null)
            {
                headOnly = false;
                return;
            }
            PointCloudPipelineBase pipeline = pc?.GetComponent<PointCloudPipelineBase>();
            if (pipeline == null)
            {
                headOnly = false;
                return;
            }
            if (headOnly)
            {
                float[] bbox = head.GetBoundingBox(pc.transform);
                if (invertX)
                {
                    float minX = -bbox[1];
                    float maxX = -bbox[0];
                    bbox[0] = minX;
                    bbox[1] = maxX;
                }
                if (invertZ)
                {
                    float minZ = -bbox[5];
                    float maxZ = -bbox[4];
                    bbox[4] = minZ;
                    bbox[5] = maxZ;
                }
                if (drawCenterSphere)
                {
                    Vector3 pcCenter = new Vector3((bbox[0] + bbox[1]) / 2f, (bbox[2] + bbox[3]) / 2f, (bbox[4] + bbox[5]) / 2f);
                    _debugCenter = pc.transform.TransformPoint(pcCenter);
                }
                pipeline.SetCrop(bbox);
            } else
            {
                pipeline.SetCrop(null);
            }
        }
    }
}