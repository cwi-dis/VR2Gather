using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Teleporter
{
    public class FreeTeleporter : BaseTeleporter
    {
        [Tooltip("Marker for display ground position")]
        public GameObject positionMarker;

        [Tooltip("Target transferred by teleport")]
        public Transform bodyTransforn;

        [Tooltip("Adjust teleport destination for camera position within body")]
        public Transform adjustForPositionOfCamera = null;

        [Tooltip("Ignore these layers (for performance)")]
        public LayerMask excludeLayers;

        [Tooltip("If not empty, only objects with this tag are acceptable as teleport target")]
        public string onlyTagged = "Teleportable";

        [Tooltip("Ignore untagged objects (otherwise they block the teleport line)")]
        public bool ignoreUntagged = true;

        [Tooltip("Arc take off angle")]
        public float angle = 45f;

        [Tooltip("Increasing this value will increase overall arc length")]
        public float strength = 10f;

        [Tooltip("limitation of vertices for performance")]
        public int maxVertexcount = 100; 

        private float vertexDelta = 0.08f; // Delta between each Vertex on arc. Decresing this value may cause performance problem.

        private LineRenderer arcRenderer; // The renderer for the teleport arc

        private Vector3 velocity; // Velocity of latest vertex

        private Vector3 groundPos; // detected ground position

        private Vector3 lastNormal; // detected surface normal

        private bool groundDetected = false;

        private List<Vector3> vertexList = new List<Vector3>(); // vertex on arc

        [Tooltip("don't update path when it's false.")]
        public bool displayActive = false;

        public override bool teleporterActive { 
            get { return displayActive; } 
        } 

        [Tooltip("Material for teleport arc")]
        public Material lineTeleportableMat;
        [Tooltip("Material for Teleport arc")]
        public Material lineNotTeleportableMat;


        // Teleport target transform to ground position
        public override void Teleport()
        {
            if (groundDetected)
            {
                Vector3 playerWorldPosition = bodyTransforn.position;
                Vector3 newPosition = groundPos + (lastNormal * 0.1f);
                if (adjustForPositionOfCamera != null)
                {
                    Vector3 cameraWorldPosition = adjustForPositionOfCamera.position;
                    Vector3 cameraOffset = new Vector3(cameraWorldPosition.x - playerWorldPosition.x, 0.0f, cameraWorldPosition.z - playerWorldPosition.z);
                    newPosition -= cameraOffset;
                }
                bodyTransforn.position = newPosition;
                SetActive(false);
            }
            else
            {
                Debug.LogError("VRTeleporter: Teleport() called but ground wasn't detected");
            }
        }

        public override void TeleportHome()
        {
            Vector3 newPosition = Vector3.zero;
            bodyTransforn.localPosition = newPosition;
            SetActive(false);
        }

        public override bool canTeleport()
        {
            return groundDetected;
        }

        // Enable (or disable) the teleport ray
        public override void SetActive(bool active)
        {
            arcRenderer.enabled = active;
            positionMarker.SetActive(active);
            if (active && !displayActive)
            {
                // Becoming active. Reset.
                arcRenderer.sharedMaterial = lineNotTeleportableMat;
                groundDetected = false;
            }
            displayActive = active;
        }

        private void Awake()
        {
            arcRenderer = GetComponent<LineRenderer>();
            arcRenderer.sharedMaterial = lineNotTeleportableMat;
            arcRenderer.enabled = false;
            positionMarker.SetActive(false);
        }

#if XXXJACK_UNUSED
        private void FixedUpdate()
        {
            //if (displayActive)
            //{
            //    UpdatePath();
            //}
        }
#endif


        public override void UpdatePath()
        {
            CustomUpdatePath(null, null, strength);
        }

        public override void CustomUpdatePath(Vector3? _origin, Vector3? _direction, float _strength)
        {
            groundDetected = false;

            vertexList.Clear(); // delete all previouse vertices

            Vector3 dir = _direction ?? transform.forward;

            velocity = Quaternion.AngleAxis(-angle, transform.right) * dir * _strength;

            RaycastHit hit;

            Vector3 pos = _origin ?? transform.position; // take off position

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
                    if (onlyTagged == "" || hit.collider.gameObject.tag == onlyTagged)
                    {
                        groundDetected = true;
                        groundPos = hit.point;
                        lastNormal = hit.normal;
                        arcRenderer.sharedMaterial = lineTeleportableMat;
                    }
                    // We stop at the first hit, whether or not we reached a teleportable
                    // destination, unless we ignore untagged objects (basically resulting
                    // in other objects being transparent to our teleport ray)
                    if (!ignoreUntagged)
                    {
                        break;
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