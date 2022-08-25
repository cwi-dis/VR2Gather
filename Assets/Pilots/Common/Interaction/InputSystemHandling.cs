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
       

        [Tooltip("Object responsible for implementing touching and teleporting")]
        public HandInteractionEmulation handInteraction;
        
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


        public void OnDelta(InputValue value)
        {
            delta = value.Get<Vector2>();
          
            if (modeGropingActive || modeTeleportingActive)
            {
                handInteraction?.InputModeUpdate(delta);
            }
        }

        public void OnHeightDelta(InputValue value)
        {
            deltaHeight = value.Get<float>();
            Debug.Log($"xxxjack InputSystemHandling: deltaHeight={deltaHeight}");
            if ((modeMovingActive || modeTurningActive) && deltaHeight != 0)
            {
                // Do Camera movement for up/down.
                Debug.Log($"InputSystemHandling: deltaHeight {deltaHeight}");
                cameraTransformToControl.localPosition = new Vector3(
                    cameraTransformToControl.localPosition.x,
                    cameraTransformToControl.localPosition.y + deltaHeight * heightSpeed,
                    cameraTransformToControl.localPosition.z);
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
            delta = new Vector2(0, 0);
            deltaHeight = 0;
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
            delta = new Vector2(0, 0);
            deltaHeight = 0;
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
            if (modeTeleportingActive)
            {
                modeTurningActive = modeMovingActive = modeGropingActive = false;
            }
            handInteraction?.InputModeChange(modeGropingActive, modeTeleportingActive);
        }

        // Update is called once per frame
        void Update()
        {
            if (modeMovingActive)
            {

                Vector3 move = transform.right * delta.x * Time.deltaTime + transform.forward * delta.y * Time.deltaTime;
                move = move * moveSpeed;
                Debug.Log($"InputSystemHandling: move {move}");
                controller.Move(move);
            }
            if (modeTurningActive)
            {
                float xRotation = delta.x * pitchSpeed * Time.deltaTime;
                float yRotation = delta.y * turnSpeed * Time.deltaTime;


                xRotation = Mathf.Clamp(xRotation, -45f, 45f);

                Debug.Log($"InputSystemHandling: Turn({delta}) to x={xRotation}, y={yRotation}");

                var oldRotation = cameraTransformToControl.localRotation.x;
                cameraTransformToControl.localRotation = cameraTransformToControl.localRotation * Quaternion.Euler(xRotation, 0f, 0f);
                adjustBodyHead(xRotation, -yRotation);
            }
            
        }

        protected void adjustBodyHead(float hAngle, float vAngle)
        {
            playerBody.Rotate(Vector3.up, hAngle);
            avatarHead.Rotate(Vector3.right, vAngle);
        }
    }
}