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
        [Tooltip("The virtual hand that is used to touch objects")]
        public GameObject hand;
        [Tooltip("Collider that actually presses the button")]
        public Collider touchCollider = new SphereCollider();
        protected bool isGroping;
        protected bool isTouching;
        protected Animator _Animator = null;

        void Start()
        {
            _Animator = GetComponentInChildren<Animator>();
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
                    isTouching = false;
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
            bool isTouchingNow = false;
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
            if (gotFirstHit && gotCorrectHit && firstHit.distance >= correctHit.distance)
            {
                isTouchingNow = true;
            }
            //
            // Show that hand, either touching or not touching.
            // If not touching we are done.
            //
            isTouching = isTouchingNow;
            if (isTouching)
            {
                showGropeTouching(ray, handDistance);
            }
            else
            {
                showGropeNotTouching(ray, handDistance);
                touchCollider.enabled = false;
                return;
            }
            //
            // Hand is touching something. Check whether the left mouse is clicked and perform the action.
            //
            if (Input.GetKey(touchKey))
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

        protected void showGropeNotTouching(Ray ray, float distance)
        {
            //Debug.Log($"showGropeNotTouching {ray.origin} to {ray.direction}");
            //Debug.DrawRay(ray.origin, ray.direction, Color.cyan, 1f);
            var point = ray.GetPoint(distance - 0.01f);
            hand.transform.position = point;
            hand.transform.rotation = Quaternion.LookRotation(ray.direction, Vector3.up);
            hand.SetActive(true);
            UpdateAnimation("");
        }

        protected void showGropeTouching(Ray ray, float distance)
        {
            //Debug.Log($"showGropeTouching {ray.origin} to {ray.direction}");
            //Debug.DrawRay(ray.origin, ray.direction, Color.magenta, 1f);
            var point = ray.GetPoint(distance - 0.01f);
            hand.transform.position = point;
            hand.transform.rotation = Quaternion.LookRotation(ray.direction, Vector3.up);
            hand.SetActive(true);
            UpdateAnimation("IsPointing");
        }

        protected void showGropeNone()
        {
            UpdateAnimation("");
            hand.transform.localPosition = Vector3.zero;
            hand.transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            Invoke("hideHand", 0.5f);
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