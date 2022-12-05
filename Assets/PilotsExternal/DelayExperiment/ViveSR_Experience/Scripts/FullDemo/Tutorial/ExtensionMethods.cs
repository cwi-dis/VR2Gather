using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Vive.Plugin.SR.Experience
{
    public static class ExtensionMethods
    {
        public static string Format(this float str)
        {
            return string.Format("{0:0.##}", str);
        }

        public static TouchpadDirection ToTouchpadDirection(this ControllerInputIndex controllerInputIndex)
        {
            Dictionary<ControllerInputIndex, TouchpadDirection> dir =
                new Dictionary<ControllerInputIndex, TouchpadDirection>
                {
                    { ControllerInputIndex.none, TouchpadDirection.None},
                    { ControllerInputIndex.up, TouchpadDirection.Up},
                    { ControllerInputIndex.down, TouchpadDirection.Down },
                    { ControllerInputIndex.left, TouchpadDirection.Left },
                    { ControllerInputIndex.right, TouchpadDirection.Right },
                    { ControllerInputIndex.mid, TouchpadDirection.Mid }
                };
                            
            return dir[controllerInputIndex];
        }
        public static string _ToString(this ControllerInputIndex controllerInputIndex)
        {
            Dictionary<ControllerInputIndex, string> dir =
                new Dictionary<ControllerInputIndex, string>
                {
                    { ControllerInputIndex.right, "Right"},
                    { ControllerInputIndex.left, "Left"},
                    { ControllerInputIndex.up, "Up" },
                    { ControllerInputIndex.down, "Down" },
                    { ControllerInputIndex.mid, "Mid"},
                    { ControllerInputIndex.trigger, "Trigger" },
                    { ControllerInputIndex.grip, "Grip"},
                };

            return dir[controllerInputIndex];  
        }

        public static void Delay(this MonoBehaviour mono, Action done, float delayTime)
        {
            mono.StartCoroutine(ExecuteAfterTime(done, delayTime));
        }

        static IEnumerator ExecuteAfterTime(Action done, float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            done();
        }

        public static void DelayOneFrame(this MonoBehaviour mono, Action done)
        {
            mono.StartCoroutine(ExecuteAfterOneFrame(done));
        }

        static IEnumerator ExecuteAfterOneFrame(Action done)
        {
            yield return new WaitForEndOfFrame();
            done();
        }
    }
}