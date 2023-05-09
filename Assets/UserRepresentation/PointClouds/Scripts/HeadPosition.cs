using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
    /// <summary>
    /// Script to supply head bounding boxes to PointCloudHeadFilter.
    /// </summary>
    public class HeadPosition : MonoBehaviour
    {
        static public HeadPosition Instance;
        [Tooltip("In the editor scene view draw the box around the head (in head orientation)")]
        public bool drawGizmoHeadcube;
        [Tooltip("In the editor scene view, draw the resulting bounding box (in world orientation)")]
        public bool drawGizmoBbox;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void OnDrawGizmos()
        {
            if (drawGizmoBbox)
            {
                float[] boundingbox = GetBoundingBox(null);
                Gizmos.color = Color.yellow;
                Vector3[] corners = new Vector3[8];
                int i = 0;
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 2; y < 4; y++)
                    {
                        for (int z = 4; z < 6; z++)
                        {
                            corners[i++] = new Vector3(boundingbox[x], boundingbox[y], boundingbox[z]);
                        }
                    }
                }
                foreach (var p1 in corners)
                {
                    foreach (var p2 in corners)
                    {
                        if (p1 != p2)
                        {
                            Gizmos.DrawLine(p1, p2);
                        }
                    }
                }
            }
            if (drawGizmoHeadcube)
            {
                Vector3[] corners = GetCorners(null);
                Gizmos.color = Color.yellow;
                foreach (var p1 in corners)
                {
                    foreach (var p2 in corners)
                    {
                        if (p1 != p2)
                        {
                            Gizmos.DrawLine(p1, p2);
                        }
                    }
                }
            }
        }

        public Vector3[] GetCorners(Transform destinationTransform)
        {
            if (destinationTransform == null)
            {
                return new Vector3[8]
                {
                transform.TransformPoint(new Vector3(0, 0, 0)),
                transform.TransformPoint(new Vector3(0, 0, 1)),
                transform.TransformPoint(new Vector3(0, 1, 0)),
                transform.TransformPoint(new Vector3(0, 1, 1)),
                transform.TransformPoint(new Vector3(1, 0, 0)),
                transform.TransformPoint(new Vector3(1, 0, 1)),
                transform.TransformPoint(new Vector3(1, 1, 0)),
                transform.TransformPoint(new Vector3(1, 1, 1))
                };
            }
            else
            {
                return new Vector3[8]
                {
                destinationTransform.InverseTransformPoint(transform.TransformPoint(new Vector3(0, 0, 0))),
                destinationTransform.InverseTransformPoint(transform.TransformPoint(new Vector3(0, 0, 1))),
                destinationTransform.InverseTransformPoint(transform.TransformPoint(new Vector3(0, 1, 0))),
                destinationTransform.InverseTransformPoint(transform.TransformPoint(new Vector3(0, 1, 1))),
                destinationTransform.InverseTransformPoint(transform.TransformPoint(new Vector3(1, 0, 0))),
                destinationTransform.InverseTransformPoint(transform.TransformPoint(new Vector3(1, 0, 1))),
                destinationTransform.InverseTransformPoint(transform.TransformPoint(new Vector3(1, 1, 0))),
                destinationTransform.InverseTransformPoint(transform.TransformPoint(new Vector3(1, 1, 1)))
                };

            }
        }

        public float[] GetBoundingBox(Transform destinationTransform)
        {
            Vector3[] corners = GetCorners(destinationTransform);
            float[] rv = new float[6];
            rv[0] = rv[1] = corners[0].x;
            rv[2] = rv[3] = corners[0].y;
            rv[4] = rv[5] = corners[0].z;
            foreach (var corner in corners)
            {
                if (corner.x < rv[0]) rv[0] = corner.x;
                if (corner.x > rv[1]) rv[1] = corner.x;
                if (corner.y < rv[2]) rv[2] = corner.y;
                if (corner.y > rv[3]) rv[3] = corner.y;
                if (corner.z < rv[4]) rv[4] = corner.z;
                if (corner.z > rv[5]) rv[5] = corner.z;
            }
            return rv;
        }
    }
}
