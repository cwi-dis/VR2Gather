using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
    /// <summary>
    /// Component that crops point clouds captured, to enable sending head-only point clouds.
    /// Probably together with some other (partial body) representation.
    /// </summary>
    public class PointCloudHeadFilter : MonoBehaviour
    {
        [Tooltip("If true head-only point cloud capture is enabled (otherwise full point clouds)")]
        public bool headOnly;
        [Tooltip("The GameObject with the point cloud pipeline")]
        public GameObject pc;
        [Tooltip("The GameObject with the bounding box (to determine point cloud cropping)")]
        public HeadPosition head;
        [Tooltip("True if point clouds have left-hand coordinates and X should be inverted")]
        public bool invertX = true;
        [Tooltip("True if point clouds have left-hand coordinates and Z should be inverted")]
        public bool invertZ = false;
         
        // Start is called before the first frame update
        void Start()
        {

        }


        private void OnDisable()
        {
            PointCloudPipelineSelf pipeline = pc?.GetComponent<PointCloudPipelineSelf>();
            if (pipeline != null )
            {
                pipeline.SetCrop(null);
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
            PointCloudPipelineSelf pipeline = pc?.GetComponent<PointCloudPipelineSelf>();
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
               
                pipeline.SetCrop(bbox);
            } else
            {
                pipeline.SetCrop(null);
            }
        }
    }
}