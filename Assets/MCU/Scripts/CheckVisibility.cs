using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.MCU
{
    public class CheckVisibility : MonoBehaviour
    {
        Camera cam;
        public int fov;
        public int lod;
        float[] lodLevels = { 4.0f, 6.0f, 8.0f, 10.0f }; // 0-1-2-3

        // Start is called before the first frame update
        void Start()
        {
            cam = GameObject.Find("Main Camera").GetComponent<Camera>();
            fov = 1;
            lod = 0;
        }

        // Update is called once per frame
        void Update()
        {
            if (GetComponent<Renderer>().IsVisibleFrom(cam))
            {
                // Calculate LOD Level
                float dist = CalculateDistance();
                if (dist <= lodLevels[0]) lod = 0;
                else if (dist < lodLevels[0] && dist <= lodLevels[1]) lod = 1;
                else if (dist > lodLevels[1] && dist <= lodLevels[2]) lod = 2;
                else if (dist > lodLevels[2] && dist <= lodLevels[3]) lod = 3;

                // Assign FOV
                fov = 1;
                Debug.Log("Visible");
            }
            else
            {
                // Assign FOV
                fov = 0;
                Debug.Log("Not visible");
            }
        }

        float CalculateDistance()
        {
            return Vector3.Distance(cam.transform.position, transform.position);
        }
    }
}