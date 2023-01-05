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
        [Tooltip("Use inverted upDown value")]
        public bool invertUpDown = true;
        [Tooltip("How fast hand moves with axis values")]
        public float sensitivity = 0.1f;

        protected float xHand, yHand;

        protected override void startTouching()
        {
            xHand = yHand = 0f;
        }

        public override void InputModeUpdate(Vector2 magnitude)
        {
            float x = magnitude.x;
            float y = magnitude.y;
            if (invertUpDown) y = -y;
            xHand += x * sensitivity;
            yHand += y * sensitivity;
            if (xHand < -1) xHand = -1;
            if (xHand > 1) xHand = 1;
            if (yHand < -1) yHand = -1;
            if (yHand > 1) yHand = 1;
            Debug.Log($"HandInteractionGamepad: update {magnitude} to {xHand},{yHand}");
        }

        protected override Vector3 getRayDestination()
        {
            float xPos = (xHand + 1.0f) * Camera.main.pixelWidth / 2;
            float yPos = (yHand + 1.0f) * Camera.main.pixelHeight / 2;
            return new Vector3(xPos, yPos, 0);
        }

    }
}
