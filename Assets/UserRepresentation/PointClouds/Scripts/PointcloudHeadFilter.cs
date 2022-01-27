using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.UserRepresentation.PointCloud;

namespace VRT.UserRepresentation.PointCloud
{
     public class PointcloudHeadFilter : MonoBehaviour
    {
        public bool headOnly;
        public GameObject pc;
        public HeadPosition head;
       
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            
            if (head == null || pc == null)
            {
                headOnly = false;
                return;
            }
            PointCloudPipeline pipeline = pc?.GetComponent<PointCloudPipeline>();
            if (pipeline == null)
            {
                headOnly = false;
                return;
            }
            if (headOnly)
            {
                float[] bbox = head.GetBoundingBox(pc.transform);
                pipeline.SetCrop(bbox);
            } else
            {
                pipeline.SetCrop(null);
            }
        }
    }
}