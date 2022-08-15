using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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
        public GameObject touchCollider = null;
        protected bool isTeleporting;
        protected bool isGroping;
        protected bool isTouching;
        protected bool isTouchable;
        protected Animator _Animator = null;
        private LineRenderer _Line;

        const float handLineDelta = 0.25f;     // Hand line stops 20cm before touching point
        const float handGropingDelta = 0.1f;  // Groping hand position is 20cm before touching point
        const float handTouchingDelta = 0f; // Touching hand position is 10cm before touching point

        void Start()
        {
            _Animator = hand.GetComponentInChildren<Animator>();
            _Line = GetComponent<LineRenderer>();
            if (unusedHand != null)
            {
                unusedHand.SetActive(false);
            }
        }

        void OnDestroy()
        {
            FinishGroping();
            FinishTeleporting(true);
        }

        void StartTeleporting()
        {
            if (teleporter == null) return;
            isTeleporting = true;
            teleporter.SetActive(isTeleporting);
        }

        void FinishTeleporting(bool abort)
        {
            if (teleporter == null || !isTeleporting) return;
            if (abort)
            {
                teleporter.TeleportHome();
            } else
            {
                if (teleporter.canTeleport())
                {
                    teleporter.Teleport();
                }
            }
            isTeleporting = false;
            teleporter.SetActive(isTeleporting);
        }

        void UpdateTeleporting()
        {
            if (teleporter == null || !isTeleporting) return;
            Ray teleportRay = VRConfig.Instance.getMainCamera().ScreenPointToRay(getRayDestination(), Camera.MonoOrStereoscopicEye.Mono);
            // We have the ray starting at our "hand" going in the direction
            // of our gaze.
            Vector3 pos = transform.position;
            Vector3 dir = teleportRay.direction;
            teleporter.CustomUpdatePath(pos, dir, 10f);
        }

        // Update is called once per frame
        void Update()
        {
            if (teleportGropeKey != KeyCode.None)
            {
                if (Input.GetKeyDown(teleportGropeKey))
                {
                    StartTeleporting();
                }
                if (Input.GetKeyUp(teleportGropeKey))
                {
                    FinishTeleporting(false);
                }
                if (Input.GetKeyUp(teleportHomeKey))
                {
                    FinishTeleporting(true);
                }
            }
            UpdateTeleporting();

            if (gropeKey != KeyCode.None)
            {
                if (Input.GetKeyDown(gropeKey))
                {
                    StartGroping();
                }
                if (Input.GetKeyUp(gropeKey))
                {
                    FinishGroping();
                }
            }
            if (touchKey != KeyCode.None)
            {
                if (Input.GetKeyDown(touchKey))
                {
                    StartTouching();
                }
                if (Input.GetKeyUp(touchKey))
                {
                    FinishTouching();
                }
            }
            UpdateGroping();

        }

        protected virtual void StartGroping()
        {
            hideCursor();
            isGroping = true;
            isTouching = false;
        }
        protected virtual void FinishGroping()
        {
            // xxxjack should we check isGroping?
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
            isGroping = false;
            isTouching = false;
        }

        public void StartTouching()
        {
            if (!isGroping) return;
            isTouching = true;
        }

        public void FinishTouching()
        {
            isTouching = false;
        }

        protected virtual void UpdateGroping()
        {
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
            Vector3 hitPoint;
            float handDistance = maxDistance;
            bool gotFirstHit = Physics.Raycast(ray, out firstHit, maxDistance, firstLayerMask);
            bool gotCorrectHit = Physics.Raycast(ray, out correctHit, maxDistance, layerMask);
            if (gotFirstHit)
            {
                handDistance = firstHit.distance;
                hitPoint = firstHit.point;
            }
            else
            {
                hitPoint = ray.GetPoint(maxDistance);
            }
            isTouchable = gotFirstHit && gotCorrectHit && firstHit.distance >= correctHit.distance;
            //
            // If hand is touching something check whether the left mouse is clicked and perform the action.
            // Note that when isTouching is set the hand is moved so the TouchCollider (or grabcollider, when implemented)
            // should do the touch magic.
            //
            showGrope(hitPoint);
            if (isTouching)
            {
                touchCollider.transform.position = hitPoint;
            }
            touchCollider.SetActive(isTouching);
        }

        protected void showGrope(Vector3 touchPoint)
        {
            Vector3 homePoint = handHomeTransform.position;
            Vector3 distance3 = touchPoint - homePoint;
            float distance = distance3.magnitude;
            Vector3 direction = distance3 / distance;

            if (hotspot != null)
            {
                hotspot.transform.position = touchPoint;
                hotspot.SetActive(true);
            }
            if (hand != null)
            {
                var handPoint = homePoint + direction * (distance - (isTouching ? handTouchingDelta : handGropingDelta));
                hand.transform.position = handPoint;
                hand.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                hand.SetActive(true);
                UpdateAnimation(isTouchable ? "IsPointing" : "");
            }
            if (_Line != null)
            {
                var linePoint = homePoint + direction * (distance - handLineDelta);
                var points = new Vector3[2] { handHomeTransform.position, linePoint };
                _Line.SetPositions(points);
                _Line.enabled = true;
            }
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