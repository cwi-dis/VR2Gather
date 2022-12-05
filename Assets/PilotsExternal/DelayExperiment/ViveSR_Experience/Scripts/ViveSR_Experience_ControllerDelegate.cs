using UnityEngine;
using System.Collections;
using Valve.VR.InteractionSystem;
using Valve.VR;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Vive.Plugin.SR.Experience
{
    public enum ButtonStage//ControlInputStatus
    {
        None,
        PressDown,
        Press,
        PressUp,
        TouchDown,
        TouchUp,
        Touch
    }     
    
    public enum TouchpadDirection
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Mid
    }

    public enum ViveControlType
    {
        Grip,
        Trigger,
        Touchpad,
        Max
    }

    [Serializable]
    public class ViveControlInput
    {
        [SerializeField]
        public SteamVR_Action_Boolean actionClick = null;
        public SteamVR_Action_Boolean actionTouch = null;
        public SteamVR_Action_Vector2 actionPosition = null;

        public ViveControlInput(SteamVR_Action_Boolean click)
        {
            actionClick = click;
        }

        public ViveControlInput(SteamVR_Action_Boolean click, SteamVR_Action_Boolean touch, SteamVR_Action_Vector2 position)
        {
            actionClick = click;
            actionTouch = touch;
            actionPosition = position;
        }
    }

    public class ViveSR_Experience_ControllerDelegate : MonoBehaviour
    {
        #region singleton
        private static ViveSR_Experience_ControllerDelegate _instance;
        public static ViveSR_Experience_ControllerDelegate instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ViveSR_Experience_ControllerDelegate>();
                }
                return _instance;
            }
        }
        #endregion

        public static Dictionary<ViveControlType, ViveControlInput> ViveControlInputs { get; private set; }

        Hand hand = null;

        public delegate void TriggerDelegate(ButtonStage buttonStage, Vector2 axis);
        public static TriggerDelegate triggerDelegate = null;
        public static TriggerDelegate triggerDelegate_Late = null;
        public delegate void TouchpadDelegate(ButtonStage buttonStage, Vector2 axis);
        public static TouchpadDelegate touchpadDelegate = null;
        public static TouchpadDelegate touchpadDelegate_Late = null;
        public delegate void GripDelegate(ButtonStage buttonStage, Vector2 axis);
        public static TouchpadDelegate gripDelegate = null;
        public static TouchpadDelegate gripDelegate_Late = null;

        public void Init()
        {
            // Get control input settings from script
            ViveControlInputs = new Dictionary<ViveControlType, ViveControlInput>();
            ViveControlInputs.Add(ViveControlType.Grip, new ViveControlInput(SteamVR_Input.GetAction<SteamVR_Action_Boolean>("srinput", "grip")));
            ViveControlInputs.Add(ViveControlType.Trigger, new ViveControlInput(SteamVR_Input.GetAction<SteamVR_Action_Boolean>("srinput", "trigger")));
            ViveControlInputs.Add(ViveControlType.Touchpad, new ViveControlInput(SteamVR_Input.GetAction<SteamVR_Action_Boolean>("srinput", "touchpadclick"), SteamVR_Input.GetAction<SteamVR_Action_Boolean>("srinput", "touchpadtouch"), SteamVR_Input.GetAction<SteamVR_Action_Vector2>("srinput", "touchpadposition")));

            //// Get control input settings from inspector
            //for (int i = 0; i < (int)ViveControlType.Max; ++i)
            //    ViveControlInputs[(ViveControlType)i] = ViveControlInputList[i];

            // Disable 2D
            Player.instance.allowToggleTo2D = false;

            // Get hand and add listener to each button
            hand = ViveSR_Experience.instance.targetHand;
            for (int i = 0; i < (int)ViveControlType.Max; ++i)
            {
                ViveControlType type = (ViveControlType)i;

                if (type == ViveControlType.Touchpad)
                    ViveControlInputs[type].actionTouch.AddOnUpdateListener(ClickListener, hand.handType);
                ViveControlInputs[type].actionClick.AddOnUpdateListener(ClickListener, hand.handType);
            }
        }

        void ClickListener(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, bool newState)
        {
            if (fromSource != SteamVR_Input_Sources.LeftHand && fromSource != SteamVR_Input_Sources.RightHand)
                return;

            // Get button according to control type
            ViveControlType controlType = ViveControlInputs.First(x => fromAction == x.Value.actionClick || fromAction == x.Value.actionTouch).Key;
            ViveControlInput input = ViveControlInputs[controlType];

            // Get button click status
            ButtonStage status = GetInputActionStatus(fromAction, input);
            if (status == ButtonStage.None) return;

            // Call each button's delegate behavior
            if (controlType == ViveControlType.Grip)
            {
                if (gripDelegate != null) gripDelegate(status, Vector2.zero);
                if (gripDelegate_Late != null) gripDelegate_Late(status, Vector2.zero);
            }
            else if (controlType == ViveControlType.Trigger)
            {
                if (triggerDelegate != null) triggerDelegate(status, Vector2.zero);
                if (triggerDelegate_Late != null) triggerDelegate_Late(status, Vector2.zero);
            }
            else if (controlType == ViveControlType.Touchpad)
            {
                if (touchpadDelegate != null) touchpadDelegate(status, input.actionPosition.GetAxis(hand.handType));
                if (touchpadDelegate_Late != null) touchpadDelegate_Late(status, input.actionPosition.GetAxis(hand.handType));
            }
        }

        private ButtonStage GetInputActionStatus(SteamVR_Action_Boolean actionIn, ViveControlInput input)
        {
            SteamVR_Action_Boolean actionClick = input.actionClick;
            SteamVR_Action_Boolean actionTouch = input.actionTouch;
            ButtonStage status = ButtonStage.None;

            if (actionIn == actionClick)
            {
                if (actionClick.GetStateDown(hand.handType))
                {
                    status = ButtonStage.PressDown;
                }
                else if (actionClick.GetStateUp(hand.handType))
                {
                    status = ButtonStage.PressUp;
                }
                else if (actionClick.GetState(hand.handType))
                {
                    status = ButtonStage.Press;
                }
            }
            else if (actionIn == actionTouch)
            {
                if (actionTouch.GetStateDown(hand.handType))
                {
                    status = ButtonStage.TouchDown;
                }
                else if (actionTouch.GetStateUp(hand.handType))
                {
                    status = ButtonStage.TouchUp;
                }
                else if (actionTouch.GetState(hand.handType))
                {
                    status = ButtonStage.Touch;
                }
            }

            return status;
        }

        public static TouchpadDirection GetTouchpadDirection(Vector2 axis, bool includeMid)
        {
            float deg;
            TouchpadDirection touchpadDirection = TouchpadDirection.None;

            if (includeMid & Vector2.Distance(axis, Vector2.zero) < 0.5f) return TouchpadDirection.Mid;

            if (axis.x == 0) deg = axis.y >= 0 ? 90 : -90;
            else deg = Mathf.Atan(axis.y / axis.x) * Mathf.Rad2Deg;

            if (axis.x >= 0)
            {
                if (deg >= 45f) touchpadDirection = TouchpadDirection.Up;
                else if (deg < 45f && deg > -45) touchpadDirection = TouchpadDirection.Right;
                else if (deg <= -45) touchpadDirection = TouchpadDirection.Down;
            }
            else
            {
                if (deg >= 45f) touchpadDirection = TouchpadDirection.Down;                                                                                                     
                else if (deg < 45f && deg > -45) touchpadDirection = TouchpadDirection.Left;
                else if (deg <= -45) touchpadDirection = TouchpadDirection.Up;
            }

            return touchpadDirection;
        }
    }
}
