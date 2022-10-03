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
        [Tooltip("Name of (button) action that enables groping")]
        public string ModeGropingActionName;
        [Tooltip("Name of (button) action that enables teleporting")]
        public string ModeTeleportingActionName;
        [Tooltip("Name of action (button) that activates teleport")]
        public string TeleportGoActionName;
        [Tooltip("Name of action (button) that activates home teleport")]
        public string TeleportHomeActionName;
        [Tooltip("Name of action (button) that activates groping touch")]
        public string GropeTouchActionName;
        [Tooltip("Name of action (2D) that moves player or hand")]
        public string MovingTurningDeltaActionName;
        [Tooltip("Name of action (axis) that moves player camera height")]
        public string MovingTurningHeightActionName;

        [Tooltip("Object responsible for implementing touching and teleporting")]
        public HandInteractionEmulation handInteraction;

        [Header("Introspection objects for debugging")]
        public PlayerInput MyPlayerInput;
        public bool modeMovingActive = false;
        public bool modeTurningActive = false;
        public bool modeGropingActive = false;
        public bool modeTeleportingActive = false;

        private Vector2 delta = new Vector2(0,0);
        private float deltaHeight = 0;

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
            InputAction MyModeMovingAction = MyPlayerInput.actions[ModeMovingActionName];
            InputAction MyModeGropingAction = MyPlayerInput.actions[ModeGropingActionName];
            InputAction MyModeTeleportingAction = MyPlayerInput.actions[ModeTeleportingActionName];
            InputAction MyTeleportGoAction = MyPlayerInput.actions[TeleportGoActionName];
            InputAction MyTeleportHomeAction = MyPlayerInput.actions[TeleportHomeActionName];
            InputAction MyGropeTouchAction = MyPlayerInput.actions[GropeTouchActionName];
            InputAction MyMovingTurningDeltaAction = MyPlayerInput.actions[MovingTurningDeltaActionName];
            InputAction MyMovingTurningHeightAction = MyPlayerInput.actions[MovingTurningHeightActionName];

            //
            // Determine what mode we are in
            //
            modeMovingActive = false;
            modeTurningActive = false;
            modeGropingActive = false;
            modeTeleportingActive = false;

            if (MyModeMovingAction.IsPressed())
            {
                modeMovingActive = true;
            }
            else
            if (MyModeGropingAction.IsPressed())
            {
                modeGropingActive = true;
            }
            else
            if (MyModeTeleportingAction.IsPressed())
            {
                modeTeleportingActive = true;
            }
            else
            {
                modeTurningActive = true;
            }
            handInteraction?.InputModeChange(modeGropingActive, modeTeleportingActive);

            //
            // Get move/height deltas
            //
            delta = MyMovingTurningDeltaAction.ReadValue<Vector2>();
            float deltaHeight = MyMovingTurningHeightAction.ReadValue<float>();

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
            if (modeGropingActive)
            {
                if (delta != Vector2.zero)
                {
                    handInteraction?.InputModeUpdate(delta);
                }
                if (MyGropeTouchAction.triggered)
                {
                    handInteraction?.InputModeGropingTouch();
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