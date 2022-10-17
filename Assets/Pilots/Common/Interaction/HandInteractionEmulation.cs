using UnityEngine;
using UnityEngine.InputSystem;
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
        protected bool inTeleportingMode = false;
        protected bool inTouchingMode = false;
        protected bool isTouchable = false;
        protected bool didTouch = false;
        protected Animator _Animator = null;
        private LineRenderer _Line;

        const float handLineDelta = 0.25f;     // Hand line stops 20cm before touching point
        const float handGrabbingDelta = 0.1f;  // Grabbing hand position is 20cm before touching point
        const float handTouchingDelta = 0f; // Touching hand position is 10cm before touching point

        
        void Start()
        {
            _Animator = hand.GetComponentInChildren<Animator>();
            _Line = GetComponent<LineRenderer>();
            stopTouching();
            if (unusedHand != null)
            {
                unusedHand.SetActive(false);
            }
        }

        void OnDestroy()
        {
            stopTouching();
        }

        public void InputModeChange(bool grabbing, bool teleporting)
        {
            Debug.Log($"HandInteractionEmulation: grabbing={grabbing}, teleporting={teleporting}");
            if (grabbing)
            {
                if (!inTouchingMode)
                {
                    // Start Grope
                    touchCollider.SetActive(false);
                    isTouchable = false;
                    didTouch = false;
                    startTouching();
                }
            } else
            {
                if (inTouchingMode)
                {
                    // Stop touching mode (i.e. don't extend index finger)
                    stopTouching();
                    touchCollider.SetActive(false);
                }
            }
            inTouchingMode = grabbing;
            
            inTeleportingMode = teleporting;
            teleporter.SetActive(inTeleportingMode);

        }

        virtual public void InputModeUpdate(Vector2 magnitude)
        {
            Debug.Log($"HandInteractionEmulation: update {magnitude} ignored");
            if (inTouchingMode)
            {

            }
            if (inTeleportingMode)
            {

            }
        }

        public void InputModeTeleportGo()
        {
            if (inTeleportingMode)
            {
                Debug.Log("HandInteractionEmulation: Teleport go");
                if (teleporter.canTeleport())
                {
                    teleporter.Teleport();
                }
                teleporter.SetActive(false);
                inTeleportingMode = false;
            }
        }

        public void InputModeTeleportHome()
        {
            if (inTeleportingMode)
            {
                Debug.Log("HandInteractionEmulation: Teleport home");
                teleporter.TeleportHome();
                teleporter.SetActive(false);
                inTeleportingMode = false;
            }
        }

        public void InputModeTouchingTouch()
        {
            if (inTouchingMode)
            {
                Debug.Log("HandInteractionEmulation: touching touch");
                didTouch = true;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // First check teleporter, if enabled
            if (teleporter != null && teleporter.teleporterActive)
            {
                Ray teleportRay = VRConfig.Instance.getMainCamera().ScreenPointToRay(getRayDestination(), Camera.MonoOrStereoscopicEye.Mono);
                // We have the ray starting at our "hand" going in the direction
                // of our gaze.
                Vector3 pos = transform.position;
                Vector3 dir = teleportRay.direction;
                teleporter.CustomUpdatePath(pos, dir, 10f);
            }
            if (inTouchingMode)
            {
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
                bool isTouchingNow = isTouchable && didTouch;
                showGrope(hitPoint, isTouchable, isTouchingNow);
                if (isTouchingNow)
                {
                    touchCollider.transform.position = hitPoint;
                }
                touchCollider.SetActive(isTouchingNow);
            }
            didTouch = false;

        }

        protected virtual void startTouching()
        {
        }

        protected void showGrope(Vector3 touchPoint, bool isTouchable, bool isTouching)
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
                var handPoint = homePoint + direction * (distance - (isTouching ? handTouchingDelta : handGrabbingDelta));
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

        protected void stopTouching()
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
        }

        protected void UpdateAnimation(string state)
        {
            if (_Animator == null) return;
            _Animator.SetBool("IsGrabbing", state == "IsGrabbing");
            _Animator.SetBool("IsPointing", state == "IsPointing");
        }

        protected virtual Vector3 getRayDestination()
        {
            var rv = Mouse.current.position.ReadValue();
            return rv;
        }

        protected virtual Ray getRay()
        {
            return VRConfig.Instance.getMainCamera().ScreenPointToRay(getRayDestination(), Camera.MonoOrStereoscopicEye.Mono);
        }
    }
}