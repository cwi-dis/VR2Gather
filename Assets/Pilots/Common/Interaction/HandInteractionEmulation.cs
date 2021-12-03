using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
using VRT.Teleporter;

namespace VRT.Pilots.Common
{
    //
    // This script emulates HandInteraction for use when you have no HMD or controllers,
    // only keyboard and mouse.
    // When you press shift you will see an indication whether the object under the mouse is
    // touchable. If so you can press left-click and touch it. 
    //
    // Grabbing not implemented, because it doesn't seem to useful (without hands). But doable
    // if we want to.
    //
    public class HandInteractionEmulation : MonoBehaviour
    {
        [Tooltip("Maximum distance of touchable objects")]
        public float maxDistance = Mathf.Infinity;
        [Tooltip("Key to press to start looking for touchable items")]
        public KeyCode gropeKey = KeyCode.LeftShift;
        [Tooltip("Key to press to touch an item")]
        public KeyCode touchKey = KeyCode.Mouse0;
        [Tooltip("Key to press to start looking for teleportable locations")]
        public KeyCode teleportGropeKey = KeyCode.LeftControl;
        [Tooltip("Key to press to teleport (with teleportGropeKey also pressed)")]
        public KeyCode teleportKey = KeyCode.Mouse0;
        [Tooltip("Key to press to teleport home (with teleportGropeKey also pressed)")]
        public KeyCode teleportHomeKey = KeyCode.Alpha0;
        [Tooltip("Teleporter to use")]
        public BaseTeleporter teleporter;
        [Tooltip("Where the hitpoint is in 3D space")]
        public GameObject hotspot;
        [Tooltip("The hand that is used to touch objects")]
        public GameObject hand;
        [Tooltip("The unused hand, which is hidden")]
        public GameObject unusedHand;
        [Tooltip("The transform that governs the hand's idle position")]
        public Transform handHomeTransform;
        [Tooltip("Collider that actually presses the button")]
        public Collider touchCollider = new SphereCollider();
        protected bool isGroping;
        protected bool isTouchable;
        protected Animator _Animator = null;
        private LineRenderer _Line;

        const float handLineDelta = 0.3f;     // Hand line stops 20cm before touching point
        const float handGropingDelta = 0.2f;  // Groping hand position is 20cm before touching point
        const float handTouchingDelta = 0.1f; // Touching hand position is 10cm before touching point

        void Start()
        {
            _Animator = GetComponentInChildren<Animator>();
            _Line = GetComponent<LineRenderer>();
            showGropeNone();
            if (unusedHand != null)
            {
                unusedHand.SetActive(false);
            }
        }

        void OnDestroy()
        {
            showGropeNone();
        }

        // Update is called once per frame
        void Update()
        {
            // First check teleporter, if enabled
            if (teleporter != null && teleportGropeKey != KeyCode.None)
            {
                bool isTeleportingNow = Input.GetKey(teleportGropeKey);
                teleporter.SetActive(isTeleportingNow);
                if (teleporter.teleporterActive)
                {
                    Ray teleportRay = VRConfig.Instance.getMainCamera().ScreenPointToRay(getRayDestination(), Camera.MonoOrStereoscopicEye.Mono);
                    // We have the ray starting at our "hand" going in the direction
                    // of our gaze.
                    Vector3 pos = transform.position;
                    Vector3 dir = teleportRay.direction;
                    teleporter.CustomUpdatePath(pos, dir, 10f);
                    if (Input.GetKeyDown(teleportKey))
                    {
                        if (teleporter.canTeleport())
                        {
                            teleporter.Teleport();
                        }
                        teleporter.SetActive(false);
                    }
                    if (teleportHomeKey != KeyCode.None && Input.GetKeyDown(teleportHomeKey))
                    {
                        teleporter.TeleportHome();
                    }
                    return;
                }
            }
            bool isGropingingNow = Input.GetKey(gropeKey);
            if (isGroping != isGropingingNow)
            {
                isGroping = isGropingingNow;
                if (isGroping)
                {
                    touchCollider.enabled = false;
                    isTouchable = false;
                    startGroping();
                }
                else
                {
                    showGropeNone();
                    touchCollider.enabled = false;
                }
            }
            if (!isGroping) return;

            //
            // Check whether we are hitting any elegible object.
            // We look both for the first hit (where we show the hand) and
            // the first touchable hit.
            //
            int firstLayerMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("TouchCollider", "GrabCollider");
            int layerMask = LayerMask.GetMask("TouchableObject");
            Ray ray = getRay();
            Debug.DrawRay(ray.origin, ray.direction, Color.green);
            RaycastHit firstHit = new RaycastHit();
            RaycastHit correctHit = new RaycastHit();
            float handDistance = maxDistance;
            bool gotFirstHit = Physics.Raycast(ray, out firstHit, maxDistance, firstLayerMask);
            bool gotCorrectHit = Physics.Raycast(ray, out correctHit, maxDistance, layerMask);
            if (gotFirstHit)
            {
#if XXXJACK_DEBUG_RAYCAST
                // There is an issue with the raycast sometimes hitting the 3D avatar head. Need to investigate.
                if (firstHit.rigidbody)
                {
                    Debug.Log($"xxxjack firstHit gameObject {firstHit.rigidbody.gameObject.name} layer {firstHit.rigidbody.gameObject.layer} distance {firstHit.distance} name {firstHit.rigidbody.gameObject.name}");
                } else
                {
                    if (firstHit.collider) Debug.Log($"xxxjack firsthit collider {firstHit.collider.name} on {firstHit.collider.gameObject.name} distance {firstHit.distance}");
                }
#endif
                handDistance = firstHit.distance;
            }
            isTouchable = gotFirstHit && gotCorrectHit && firstHit.distance >= correctHit.distance;
            //
            // Show that hand, either touching or not touching.
            // If not touching we are done.
            //

#if xxx
            if (isTouchable)
            {
                showGropeTouching(ray, handDistance);
            }
            else
            {
                showGropeNotTouching(ray, handDistance);
                touchCollider.enabled = false;
                return;
            }
#endif
            //
            // If hand is touching something check whether the left mouse is clicked and perform the action.
            //
            bool isTouching = isTouchable && Input.GetKey(touchKey);
            showGrope(ray, handDistance, isTouchable, isTouching);
            if (isTouching)
            {
                GameObject objHit = correctHit.collider.gameObject;
                Debug.Log($"xxxjack Moving touchCollider to {objHit.name} at {objHit.transform.position}");
                touchCollider.enabled = true;
                touchCollider.transform.position = correctHit.collider.transform.position;
            }
            if (Input.GetKeyUp(touchKey))
            {
                touchCollider.enabled = false;
            }
        }

        protected virtual void startGroping()
        {
            hideCursor();
        }

        protected void showGrope(Ray ray, float distance, bool isTouchable, bool isTouching)
        {
            if (hotspot != null)
            {
                var hotSpotPoint = ray.GetPoint(distance);
                hotspot.transform.position = hotSpotPoint;
                hotspot.SetActive(true);
            }
            if (hand != null)
            {
                var handPoint = ray.GetPoint(distance - (isTouching ? handTouchingDelta : handGropingDelta));
                hand.transform.position = handPoint;
                hand.transform.rotation = Quaternion.LookRotation(ray.direction, Vector3.up);
                hand.SetActive(true);
                UpdateAnimation(isTouchable ? "IsPointing" : "");
            }
            if (_Line != null && handHomeTransform != null)
            {
                var linePoint = ray.GetPoint(distance - handLineDelta);
                var points = new Vector3[2] { handHomeTransform.position, linePoint };
                _Line.SetPositions(points);
                _Line.enabled = true;
            }
        }

        protected void showGropeNone()
        {
            if (hotspot != null)
            {
                hotspot.SetActive(false);
            }
            if (hand != null)
            {
                UpdateAnimation("");
                if (handHomeTransform == null)
                {
                    hand.transform.localPosition = Vector3.zero;
                    hand.transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                }
                else
                {
                    hand.transform.position = handHomeTransform.position;
                    hand.transform.rotation = handHomeTransform.rotation;
                }
            }
            if (_Line != null)
            {
                _Line.enabled = false;
            }
            showCursor();
        }

        private void hideHand()
        {
            hand.SetActive(false);
        }

        protected virtual void showCursor()
        {
            Cursor.visible = true;
        }

        protected virtual void hideCursor()
        {
            Cursor.visible = false;
        }

        protected void UpdateAnimation(string state)
        {
            if (_Animator == null) return;
            _Animator.SetBool("IsGrabbing", state == "IsGrabbing");
            _Animator.SetBool("IsPointing", state == "IsPointing");
        }

        protected virtual Vector3 getRayDestination()
        {
            return Input.mousePosition;
        }

        protected virtual Ray getRay()
        {
            return VRConfig.Instance.getMainCamera().ScreenPointToRay(getRayDestination(), Camera.MonoOrStereoscopicEye.Mono);
        }
    }
}