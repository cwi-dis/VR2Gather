using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
    //
    // This script emulates HandController for use when you have a gamepad controller.
    //
    // When you press the grope key you will see an indication whether the object in the center of the screen is
    // touchable. If so you can press touchKey and touch it.
    //
    // Grabbing not implemented, because it doesn't seem to useful (without hands). But doable
    // if we want to.
    //
    public class HandControllerGamepad : HandControllerEmulation
    {
        new const Texture2D gropingCursorTexture = null;
        new const Texture2D touchingCursorTexture = null;
        public GameObject hand;
        protected override bool alwaysShowGrope { get { return true; } }
        private Animator _Animator = null;

        void Start()
        {
            _Animator = GetComponentInChildren<Animator>();
        }

        protected override void showGropeNotTouching(Ray ray, float distance)
        {
            //Debug.Log($"showGropeNotTouching {ray.origin} to {ray.direction}");
            //Debug.DrawRay(ray.origin, ray.direction, Color.cyan, 1f);
            var point = ray.GetPoint(distance-0.1f);
            hand.transform.position = point;
            hand.SetActive(true);
            UpdateAnimation("");
        }

        protected override void showGropeTouching(Ray ray, float distance)
        {
            //Debug.Log($"showGropeTouching {ray.origin} to {ray.direction}");
            //Debug.DrawRay(ray.origin, ray.direction, Color.magenta, 1f);
            var point = ray.GetPoint(distance-0.1f);
            hand.transform.position = point;
            hand.SetActive(true);
            UpdateAnimation("IsPointing");
        }

        protected override void showGropeNone()
        {
            hand.SetActive(false);
            UpdateAnimation("");
        }

        protected override Vector3 getRayDestination()
        {
            return new Vector3(Camera.main.pixelWidth/2, Camera.main.pixelHeight/2, 0);
        }

        private void UpdateAnimation(string state)
        {
            if (_Animator == null) return;
            _Animator.SetBool("IsGrabbing", state == "IsGrabbing");
            _Animator.SetBool("IsPointing", state == "IsPointing");
        }
    }
}
