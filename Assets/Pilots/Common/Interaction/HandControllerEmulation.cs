using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
    //
    // This script emulates HandController for use when you have no HMD or controllers,
    // only keyboard and mouse.
    // When you press shift you will see an indication whether the object under the mouse is
    // touchable. If so you can press left-click and touch it.
    //
    // Grabbing not implemented, because it doesn't seem to useful (without hands). But doable
    // if we want to.
    //
    public class HandControllerEmulation : MonoBehaviour
    {
        [Tooltip("Mouse cursor to use while looking for touchable items")]
        public Texture2D gropingCursorTexture;
        [Tooltip("Mouse cursor to use when over a touchable item")]
        public Texture2D touchingCursorTexture;
        [Tooltip("Maximum distance of touchable objects")]
        public float maxDistance = Mathf.Infinity;
        [Tooltip("Key to press to start looking for touchable items")]
        public KeyCode gropeKey = KeyCode.LeftShift;
        [Tooltip("Key to press to touch an item")]
        public KeyCode touchKey = KeyCode.Mouse0;
        [Tooltip("Collider that actually presses the button")]
        public Collider touchCollider = new SphereCollider();
        protected bool isGroping;
        protected bool isTouching;
        protected virtual bool alwaysShowGrope { get { return false; } }

        void OnDestroy()
        {
            showGropeNone();
        }

        // Update is called once per frame
        void Update()
        {
            bool mustShow = false;
            bool isGropingingNow = Input.GetKey(gropeKey);
            if (isGroping != isGropingingNow)
            {
                isGroping = isGropingingNow;
                if (isGroping)
                {
                    mustShow = true;
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
            // Check whether we are hitting any elegible object
            //
            bool isTouchingNow = false;
            // xxxjack using the layerMask here allows users to touch objects behind other objects.
            // It may be better to do a two-step raycast, one with and one without layerMask, and only
            // touch if they both return the same object.
            int layerMask = LayerMask.GetMask("TouchableObject");
            Ray ray = Camera.main.ScreenPointToRay(getRayDestination(), Camera.MonoOrStereoscopicEye.Mono);
            RaycastHit firstHit = new RaycastHit();
            RaycastHit correctHit = new RaycastHit();
            float handDistance = maxDistance;
            bool gotFirstHit = Physics.Raycast(ray, out firstHit, maxDistance);
            bool gotCorrectHit = Physics.Raycast(ray, out correctHit, maxDistance, layerMask);
            if (gotFirstHit)
            {
                handDistance = firstHit.distance;
            }
            if (gotFirstHit && gotCorrectHit && firstHit.distance >= correctHit.distance)
            {
                //Debug.Log($"xxxjack mouse-hit {hit.collider.gameObject.name}");
                isTouchingNow = true;
            }
            if (mustShow || alwaysShowGrope || isTouching != isTouchingNow)
            {
                isTouching = isTouchingNow;
                if (isTouching)
                {
                    showGropeTouching(ray, handDistance);
                }
                else
                {
                    showGropeNotTouching(ray, handDistance);
                }
            }
            if (!isTouching)
            {
                touchCollider.enabled = false;
                return;
            }
            //
            // Now check whether the left mouse is clicked and perform the action.
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

        }

        protected virtual void showGropeNotTouching(Ray ray, float distance)
        {
            Cursor.SetCursor(gropingCursorTexture, Vector2.zero, CursorMode.Auto);
        }

        protected virtual void showGropeTouching(Ray ray, float distance)
        {
            Cursor.SetCursor(touchingCursorTexture, Vector2.zero, CursorMode.Auto);
        }

        protected virtual void showGropeNone()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        protected virtual Vector3 getRayDestination()
        {
            return Input.mousePosition;
        }
    }
}