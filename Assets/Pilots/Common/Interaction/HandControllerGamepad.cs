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
        public string leftRightAxisName = "Gamepad_Axis_1";
        public string upDownAxisName = "Gamepad_Axis_2";
        public bool invertUpDown = true;
        public float sensitivity = 0.1f;
        public GameObject hand;
        protected override bool alwaysShowGrope { get { return true; } }
        private Animator _Animator = null;
        protected float xHand, yHand;

        void Start()
        {
            _Animator = GetComponentInChildren<Animator>();
        }

        protected override void startGroping()
        {
            xHand = yHand = 0f;
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
            UpdateAnimation("");
            Invoke("hideHand", 0.5f);
        }

        private void hideHand()
        {
            hand.SetActive(false);
        }

        protected override Vector3 getRayDestination()
        {
            float x = Input.GetAxis(leftRightAxisName);
            float y = Input.GetAxis(upDownAxisName);
            if (invertUpDown) y = -y;
            xHand += x * sensitivity;
            yHand += y * sensitivity;
            if (xHand < -1) xHand = -1;
            if (xHand > 1) xHand = 1;
            if (yHand < -1) yHand = -1;
            if (yHand > 1) yHand = 1;
            float xPos = (xHand + 1.0f) * Camera.main.pixelWidth / 2;
            float yPos = (yHand + 1.0f) * Camera.main.pixelHeight / 2;
            return new Vector3(xPos, yPos, 0);
        }

        private void UpdateAnimation(string state)
        {
            if (_Animator == null) return;
            _Animator.SetBool("IsGrabbing", state == "IsGrabbing");
            _Animator.SetBool("IsPointing", state == "IsPointing");
        }
    }
}
