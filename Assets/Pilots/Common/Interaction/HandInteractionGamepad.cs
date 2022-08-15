using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
    //
    // This script emulates HandInteraction for use when you have a gamepad input device.
    //
    // When you press the grope key you will see an indication whether the object in the center of the screen is
    // touchable. If so you can press touchKey and touch it.
    //
    // Grabbing not implemented, because it doesn't seem to useful (without hands). But doable
    // if we want to.
    //
    public class HandInteractionGamepad : HandInteractionEmulation
    {
        [Tooltip("Axis that controls 2D horizontal position of hand")]
        public string leftRightAxisName = "Gamepad_Axis_1";
        [Tooltip("Axis that controls 2D vertical position of hand")]
        public string upDownAxisName = "Gamepad_Axis_2";
        [Tooltip("Use inverted upDownAxis value")]
        public bool invertUpDown = true;
        [Tooltip("How fast hand moves with axis values")]
        public float sensitivity = 0.1f;

        protected float xHand, yHand;

        protected override void StartGroping()
        {
            xHand = yHand = 0f;
            base.StartGroping();
        }

        protected override void showCursor()
        {
        }

        protected override void hideCursor()
        {
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
            float xPos = (xHand + 1.0f) * VRConfig.Instance.getMainCamera().pixelWidth / 2;
            float yPos = (yHand + 1.0f) * VRConfig.Instance.getMainCamera().pixelHeight / 2;
            return new Vector3(xPos, yPos, 0);
        }

    }
}
