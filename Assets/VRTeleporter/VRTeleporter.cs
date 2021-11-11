using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Teleporter
{
    public class VRTeleporter : MonoBehaviour
    {

        public GameObject positionMarker; // marker for display ground position

        public Transform bodyTransforn; // target transferred by teleport

        public LayerMask excludeLayers; // excluding for performance

        public float angle = 45f; // Arc take off angle

        public float strength = 10f; // Increasing this value will increase overall arc length, ORIGINAL IS 10

        public int maxVertexcount = 100; // limitation of vertices for performance.  ORIGINAL IS 100

        private float vertexDelta = 0.08f; // Delta between each Vertex on arc. Decresing this value may cause performance problem.

        private LineRenderer arcRenderer;

        private Vector3 velocity; // Velocity of latest vertex

        private Vector3 groundPos; // detected ground position

        private Vector3 lastNormal; // detected surface normal

        private bool groundDetected = false;

        private List<Vector3> vertexList = new List<Vector3>(); // vertex on arc

        public bool displayActive = false; // don't update path when it's false.

        public Material lineTeleportableMat;
        public Material lineNotTeleportableMat;


        // Teleport target transform to ground position
        public void Teleport()
        {
            if (groundDetected)
            {
                Vector3 playerWorldPosition = bodyTransforn.position;
                Vector3 cameraWorldPosition = transform.parent.parent.parent.Find("Camera").position;
                Vector3 cameraOffset = new Vector3(cameraWorldPosition.x - playerWorldPosition.x, 0.0f, cameraWorldPosition.z - playerWorldPosition.z);
                Vector3 newPosition = groundPos + (lastNormal * 0.1f);
                newPosition -= cameraOffset;
                bodyTransforn.position = newPosition;// - cameraOffset;
                ToggleDisplay(false);
            }
            else
            {
                //Debug.Log("Ground wasn't detected");
                ToggleDisplay(false);
            }
        }

        // Active Teleporter Arc Path
        public void ToggleDisplay(bool active)
        {
            arcRenderer.enabled = active;
            positionMarker.SetActive(active);
            displayActive = active;
        }





        private void Awake()
        {
            arcRenderer = GetComponent<LineRenderer>();
            arcRenderer.sharedMaterial = lineNotTeleportableMat;
            arcRenderer.enabled = false;
            positionMarker.SetActive(false);
        }

        private void FixedUpdate()
        {
            //if (displayActive)
            //{
            //    UpdatePath();
            //}
        }


        private void UpdatePath()
        {
            groundDetected = false;

            vertexList.Clear(); // delete all previouse vertices


            velocity = Quaternion.AngleAxis(-angle, transform.right) * transform.forward * strength;

            RaycastHit hit;

            Vector3 pos = transform.position; // take off position

            vertexList.Add(pos);

            while (!groundDetected && vertexList.Count < maxVertexcount)
            {
                Vector3 newPos = pos + velocity * vertexDelta
                    + 0.5f * Physics.gravity * vertexDelta * vertexDelta;

                velocity += Physics.gravity * vertexDelta;

                vertexList.Add(newPos); // add new calculated vertex

                // linecast between last vertex and current vertex
                if (Physics.Linecast(pos, newPos, out hit, ~excludeLayers))
                {
                    groundDetected = true;
                    groundPos = hit.point;
                    lastNormal = hit.normal;
                }
                pos = newPos; // update current vertex as last vertex
            }


            positionMarker.SetActive(groundDetected);

            if (groundDetected)
            {
                positionMarker.transform.position = groundPos + lastNormal * 0.1f;
                positionMarker.transform.LookAt(groundPos);
            }

            // Update Line Renderer

            arcRenderer.positionCount = vertexList.Count;
            arcRenderer.SetPositions(vertexList.ToArray());
        }

        public void CustomUpdatePath(Vector3 _pos, float _str)
        {
            groundDetected = false;

            vertexList.Clear(); // delete all previouse vertices


            velocity = Quaternion.AngleAxis(-angle, transform.right) * _pos * _str;

            RaycastHit hit;

            Vector3 pos = transform.position; // take off position

            vertexList.Add(pos);

            while (!groundDetected && vertexList.Count < maxVertexcount)
            {
                Vector3 newPos = pos + velocity * vertexDelta
                    + 0.5f * Physics.gravity * vertexDelta * vertexDelta;

                velocity += Physics.gravity * vertexDelta;

                vertexList.Add(newPos); // add new calculated vertex
                arcRenderer.sharedMaterial = lineNotTeleportableMat;
                // linecast between last vertex and current vertex
                if (Physics.Linecast(pos, newPos, out hit, ~excludeLayers))
                {
                    if (hit.collider.gameObject.tag == "Teleportable")
                    {
                        groundDetected = true;
                        groundPos = hit.point;
                        lastNormal = hit.normal;
                        arcRenderer.sharedMaterial = lineTeleportableMat;
                    }
                }
                pos = newPos; // update current vertex as last vertex
            }


            positionMarker.SetActive(groundDetected);

            if (groundDetected)
            {
                positionMarker.transform.position = groundPos + lastNormal * 0.1f;
                positionMarker.transform.LookAt(groundPos);
            }

            // Update Line Renderer

            arcRenderer.positionCount = vertexList.Count;
            arcRenderer.SetPositions(vertexList.ToArray());
        }


    }
}