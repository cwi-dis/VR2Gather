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

        protected override void showGropeNotTouching(Ray ray, float distance)
        {
            var point = ray.GetPoint(distance);
            hand.transform.position = point;
            hand.SetActive(true);
        }

        protected override void showGropeTouching(Ray ray, float distance)
        {
            var point = ray.GetPoint(distance);
            hand.transform.position = point;
            hand.SetActive(true);
        }

        protected override void showGropeNone()
        {
            hand.SetActive(false);
        }

        protected override Vector3 getRayDestination()
        {
            return Input.mousePosition; //  new Vector3(Camera.main.pixelWidth/2, Camera.main.pixelHeight/2, 0);
        }
    }
}
