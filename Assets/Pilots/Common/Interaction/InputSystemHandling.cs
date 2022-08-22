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
        public float moveSpeed = 1f;

        [Tooltip("The camera attached to the head that turns (Usually found automatically)")]
        public Transform cameraTransformToControl = null;
        [Tooltip("The player body that turns horizontally")]
        public Transform playerBody;
        [Tooltip("The player head that tilts")]
        public Transform avatarHead;
        [Tooltip("How fast the viewpoint turns")]
        public float xySensitivity = 1;
        [Tooltip("How fast the viewpoint moves up/down")]
        public float heightSensitivity = 1; // 5 Centimeters

        [Tooltip("Object responsible for implementing touching and teleporting")]
        public HandInteractionEmulation handInteraction;
        [Tooltip("xxxjack Teleport destination")]
        public Vector2 teleportPosition = new Vector2(0, 0);
        [Tooltip("xxxjack Grope destination")]
        public Vector2 gropePosition = new Vector2(0, 0);

        public bool modeMovingActive = false;
        public bool modeTurningActive = false;
        public bool modeGropingActive = false;
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


        public void OnDelta(InputValue value)
        {
            Vector2 delta = value.Get<Vector2>();
            if (modeMovingActive)
            {

                Vector3 move = transform.right * delta.x + transform.forward * delta.y;
                move = move * moveSpeed * Time.deltaTime;
                Debug.Log($"InputSystemHandling: move {move}");
                controller.Move(move);
            }
            if (modeTurningActive)
            {
                float xRotation = delta.x * xySensitivity * Time.deltaTime;
                float yRotation = delta.y * xySensitivity * Time.deltaTime;

                xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                Debug.Log($"OnDelta: Turn({delta}) to x={xRotation}, y={yRotation}");

                cameraTransformToControl.localRotation = cameraTransformToControl.localRotation * Quaternion.Euler(xRotation, 0f, 0f);
                adjustBodyHead(xRotation, -yRotation);
            }
            if (modeGropingActive)
            {
                handInteraction?.InputModeUpdate(delta);
            }
            if (modeTeleportingActive)
            {
                handInteraction?.InputModeUpdate(delta);
            }
        }

        public void OnHeightDelta(InputValue value)
        {
            float deltaHeight = value.Get<float>();

            if (modeTurningActive)
            {
                // Note by Jack: spectators and no-representation users should be able to move their viewpoint up and down.
                // with the current implementation all users have this ability, which may or may not be a good idea.
                if (deltaHeight != 0)
                {
                    // Do Camera movement for up/down.
                    cameraTransformToControl.localPosition = new Vector3(
                        cameraTransformToControl.localPosition.x,
                        cameraTransformToControl.localPosition.y + deltaHeight * heightSensitivity,
                        cameraTransformToControl.localPosition.z);
                }
            }
        }

        public void OnTeleportGo()
        {
            if (!modeTeleportingActive) return;
            handInteraction?.InputModeTeleportGo();
        }

        public void OnTeleportHome()
        {
            if (!modeTeleportingActive) return;
            handInteraction?.InputModeTeleportHome();
        }

        public void OnGropingTouch()
        {
            if (!modeGropingActive) return;
            handInteraction?.InputModeGropingTouch();
        }


        public void OnModeMoving(InputValue value)
        {
            bool onOff = value.Get<float>() != 0;
            modeMovingActive = onOff;
            Debug.Log($"InputSystemHandling: ModeMoving({onOff})");
            if (modeMovingActive)
            {
                modeTurningActive = modeGropingActive = modeTeleportingActive = false;
            }
            handInteraction?.InputModeChange(modeGropingActive, modeTeleportingActive);
        }

        public void OnModeTurning(InputValue value)
        {
            bool onOff = value.Get<float>() != 0;
            modeTurningActive = onOff;
            Debug.Log($"InputSystemHandling: ModeTurning({onOff})");
            if (modeTurningActive)
            {
                modeMovingActive = modeGropingActive = modeTeleportingActive = false;
            }
            handInteraction?.InputModeChange(modeGropingActive, modeTeleportingActive);
        }

        public void OnModeGroping(InputValue value)
        {
            bool onOff = value.Get<float>() != 0;
            modeGropingActive = onOff;
            Debug.Log($"InputSystemHandling: ModeGroping({onOff})");
            gropePosition = new Vector2(0, 0);
            if (modeGropingActive)
            {
                modeTurningActive = modeMovingActive = modeTeleportingActive = false;
            }
            handInteraction?.InputModeChange(modeGropingActive, modeTeleportingActive);
        }

        public void OnModeTeleporting(InputValue value)
        {
            bool onOff = value.Get<float>() != 0;
            modeTeleportingActive = onOff;
            Debug.Log($"ModeTeleporting({onOff})");
            teleportPosition = new Vector2(0, 0);
            if (modeTeleportingActive)
            {
                modeTurningActive = modeMovingActive = modeGropingActive = false;
            }
            handInteraction?.InputModeChange(modeGropingActive, modeTeleportingActive);
        }

        // Update is called once per frame
        void Update()
        {

        }

        protected void adjustBodyHead(float hAngle, float vAngle)
        {
            playerBody.Rotate(Vector3.up, hAngle);
            avatarHead.Rotate(Vector3.right, vAngle);
        }
    }
}