using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEditor;

#if XXXJACK_DOES_NOT_WORK
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class NegateProcessor : InputProcessor<float>
{

#if UNITY_EDITOR
    static NegateProcessor()
    {
        Debug.Log("xxxjack NegateProcessor: static constructor called");
        Initialize();
    }
#endif

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        Debug.Log("xxxjack NegateProcessor: static initializer called");
        InputSystem.RegisterProcessor<NegateProcessor>();
    }

    public override float Process(float value, InputControl control)
    {
        return 1-value;
    }
}
#endif

namespace VRT.Pilots.Common
{
    public class InputSystemHandling : MonoBehaviour
    {
        [Tooltip("The character controller for the thing to be moved")]
        public CharacterController controller;
        [Tooltip("How fast moves go")]
        public float moveSpeed = 0.1f;
        [Tooltip("How fast the viewpoint turns")]
        public float turnSpeed = 1;
        [Tooltip("How fast the viewpoint turns up/down")]
        public float pitchSpeed = 1;
        [Tooltip("How fast the viewpoint moves up/down")]
        public float heightSpeed = 1; // 5 Centimeters

        [Tooltip("The camera attached to the head that turns (Usually found automatically)")]
        public Transform cameraTransformToControl = null;
        [Tooltip("The player body that turns horizontally")]
        public Transform playerBody;
        [Tooltip("The player head that tilts")]
        public Transform avatarHead;
        [Tooltip("Current tilt angle of the head")]
        public float pitchAngle = 0;

        [Header("Input Actions")]

        [Tooltip("Name of (button) action that enables moving")]
        public string ModeMovingActionName;
        [Tooltip("Name of (button) action that enables turning")]
        public string ModeTurningActionName;
        [Tooltip("Name of (button) action that enables touching")]
        public string ModeTouchingActionName;
        [Tooltip("Name of (button) action that enables grabbing (default: always enabled)")]
        public string ModeGrabbingActionName;
        [Tooltip("Name of (button) action that enables teleporting")]
        public string ModeTeleportingActionName;
        [Tooltip("Name of action (button) that activates teleport")]
        public string TeleportGoActionName;
        [Tooltip("Name of action (button) that activates home teleport")]
        public string TeleportHomeActionName;
        [Tooltip("Name of action (button) that activates touch (default: automatic when in touch mode)")]
        public string TouchingTouchActionName;
        [Tooltip("Name of action (button) that activates grab (default: not implemented)")]
        public string GrabbingGrabActionName;
        [Tooltip("Name of action (2D) that moves player or hand or head")]
        public string MovingTurningDeltaActionName;
        [Tooltip("Name of action (2D) that always moves")]
        public string ModelessMoveActionName;
        [Tooltip("Name of action (2D) that always moves player head")]
        public string ModelessTurnActionName;
        [Tooltip("Name of action (axis) that always moves camera height")]
        public string ModelessMoveHeightActionName;
        [Tooltip("Object responsible for implementing touching and teleporting")]
        public HandInteractionEmulation handInteraction;

        [Header("Introspection objects for debugging")]
        public PlayerInput MyPlayerInput;
        public bool modeMovingActive = false;
        public bool modeTurningActive = false;
        public bool modeTouchingActive = false;
        public bool modeGrabbingActive = false;
        public bool modeTeleportingActive = false;


        private void Awake()
        {
            if (cameraTransformToControl != null) return;
            PlayerManager player = GetComponentInParent<PlayerManager>();
            cameraTransformToControl = player.getCameraTransform();
        }

        void Start()
        {

        }


        // Update is called once per frame
        void Update()
        {
            //
            // Find all the actions that we need
            //
            InputAction MyModeMovingAction = MyPlayerInput.actions.FindAction(ModeMovingActionName, false);
            InputAction MyModeTurningAction = MyPlayerInput.actions.FindAction(ModeTurningActionName, false);
            InputAction MyModeTouchingAction = MyPlayerInput.actions[ModeTouchingActionName];
            InputAction MyModeGrabbingAction = MyPlayerInput.actions.FindAction(ModeGrabbingActionName, false);
            InputAction MyModeTeleportingAction = MyPlayerInput.actions[ModeTeleportingActionName];
            InputAction MyTeleportGoAction = MyPlayerInput.actions.FindAction(TeleportGoActionName, false);
            InputAction MyTeleportHomeAction = MyPlayerInput.actions[TeleportHomeActionName];
            InputAction MyTouchingTouchAction = MyPlayerInput.actions.FindAction(TouchingTouchActionName, false);
            InputAction MyMovingTurningDeltaAction = MyPlayerInput.actions.FindAction(MovingTurningDeltaActionName, false);
            InputAction MyModelessMoveAction = MyPlayerInput.actions.FindAction(ModelessMoveActionName, false);
            InputAction MyModelessTurnAction = MyPlayerInput.actions.FindAction(ModelessTurnActionName, false);
            InputAction MyModelessMoveHeightAction = MyPlayerInput.actions.FindAction(ModelessMoveHeightActionName, false);

            //
            // Get move/height deltas
            //
            Vector2 delta = Vector2.zero;
            float deltaHeight = 0;
            Vector2 modelessMoveDelta = Vector2.zero;
            Vector2 modelessTurnDelta = Vector2.zero;
            float modelessMoveDeltaHeight = 0;
            if (MyMovingTurningDeltaAction != null) delta = MyMovingTurningDeltaAction.ReadValue<Vector2>();
            if (MyModelessMoveAction != null) modelessMoveDelta = MyModelessMoveAction.ReadValue<Vector2>();
            if (MyModelessTurnAction != null) modelessTurnDelta = MyModelessTurnAction.ReadValue<Vector2>();
            if (MyModelessMoveHeightAction != null) modelessMoveDeltaHeight = MyModelessMoveHeightAction.ReadValue<float>();

            //
            // Determine what mode we are in
            //
            modeMovingActive = false;
            modeTurningActive = false;
            modeTouchingActive = false;
            modeTeleportingActive = false;
            modeGrabbingActive = false;

            //
            // First check whether any of the modeless move/turn are active.
            // Then we check for any of the mode-activating buttons.
            // Finally we go to the default mode, which is grabbing if that is enabled, otherwise turning.
            //
            if (modelessMoveDelta != Vector2.zero || modelessMoveDeltaHeight != 0)
            {
                modeMovingActive = true;
                delta = modelessMoveDelta;
                deltaHeight = modelessMoveDeltaHeight;
            }
            else
            if (modelessTurnDelta != Vector2.zero)
            {
                modeTurningActive = true;
                delta = modelessTurnDelta;
            }
            else
            if (MyModeMovingAction != null && MyModeMovingAction.IsPressed())
            {
                modeMovingActive = true;
            }
            else
            if (MyModeTouchingAction != null && MyModeTouchingAction.IsPressed())
            {
                modeTouchingActive = true;
            }
            else
            if (MyModeTeleportingAction != null && MyModeTeleportingAction.IsPressed())
            {
                modeTeleportingActive = true;
            }
            else
            if (MyModeTurningAction != null && MyModeTurningAction.IsPressed())
            {
                modeTurningActive = true;
            } 
            else
            if (MyModeGrabbingAction == null)
            {
                modeGrabbingActive = true; 
            }
            else
            {
                modeTurningActive = true;
            }
            handInteraction?.InputModeChange(modeTouchingActive, modeTeleportingActive);

            //
            // Implement current mode
            //
            if (modeMovingActive)
            {
                if (delta != Vector2.zero)
                {
                    Vector3 move = transform.right * delta.x * Time.deltaTime + transform.forward * delta.y * Time.deltaTime;
                    move = move * moveSpeed;
                    Debug.Log($"InputSystemHandling: move {move}");
                    controller.Move(move);
                }
                if (deltaHeight != 0)
                {
                    // Do Camera movement for up/down.
                    Debug.Log($"InputSystemHandling: deltaHeight {deltaHeight}");
                    cameraTransformToControl.localPosition = new Vector3(
                        cameraTransformToControl.localPosition.x,
                        cameraTransformToControl.localPosition.y + deltaHeight * heightSpeed,
                        cameraTransformToControl.localPosition.z);
                }
            }
            else
            if (modeTurningActive)
            {
                if (delta != Vector2.zero)
                {
                    float turnRotation = delta.x * turnSpeed * Time.deltaTime;
                    float pitchRotation = delta.y * pitchSpeed * Time.deltaTime;

                    pitchAngle = pitchAngle - pitchRotation;
                    pitchAngle = Mathf.Clamp(pitchAngle, -90f, 90f);

                    Debug.Log($"InputSystemHandling: Turn({delta}) to turn={turnRotation}, pitch={pitchRotation} to {pitchAngle}");

                    cameraTransformToControl.localRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
                    adjustBodyHead(turnRotation, -pitchRotation);
                }
            }
            else
            if (modeTeleportingActive)
            {
                if (delta != Vector2.zero)
                {
                    handInteraction?.InputModeUpdate(delta);
                }
                if (MyTeleportGoAction.triggered)
                {
                    handInteraction?.InputModeTeleportGo();
                }
                if (MyTeleportHomeAction.triggered)
                {
                    handInteraction?.InputModeTeleportHome();
                }
            }
            else
            if (modeTouchingActive)
            {
                if (delta != Vector2.zero)
                {
                    handInteraction?.InputModeUpdate(delta);
                }
                if (MyTouchingTouchAction.triggered)
                {
                    handInteraction?.InputModeTouchingTouch();
                }
            }
            else
            if (modeGrabbingActive)
            {
                if (MyModeGrabbingAction.IsPressed())
                {
                    Debug.Log($"xxxjack grab not yet implemented");
                }
            }
            
        }

        protected void adjustBodyHead(float hAngle, float vAngle)
        {
            playerBody.Rotate(Vector3.up, hAngle);
            avatarHead.Rotate(Vector3.right, vAngle);
        }
    }
}