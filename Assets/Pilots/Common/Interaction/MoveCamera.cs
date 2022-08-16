using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
using VRT.Core;

namespace VRT.Pilots.Common
{
    public class MoveCamera : MonoBehaviour
    {
        public float xySensitivity = 100.0f;
        public float heightSensitivity = 0.05f; // 5 Centimeters
        public bool allowHJKLforMouse = true;
        public bool spectator = false;
        [Tooltip("Keys that disable head movement (because they use these axes for pointing or teleporting)")]
#if ENABLE_INPUT_SYSTEM
        public string[] inhibitKeyPaths;
#else
        public KeyCode[] inhibitKeys;
#endif

        protected Transform cameraTransformToControl = null;
        protected float xRotation = 0f;

        public Transform playerBody;
        public Transform avatarHead;

        private void Awake()
        {
            PlayerManager player = GetComponentInParent<PlayerManager>();
            cameraTransformToControl = player.getCameraTransform();
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            foreach (var inhibitKeyPath in inhibitKeyPaths)
            {
                var k = InputSystem.FindControl(inhibitKeyPath) as ButtonControl;
                if (k == null) Debug.LogError($"MoveCamera: unknown keypath {inhibitKeyPath}");
                if (k != null && k.isPressed)
                {
                    return;
                }
            }
#else
            foreach(var inhibitKey in inhibitKeys)
            {
                if (inhibitKey != KeyCode.None && Input.GetKey(inhibitKey))
                {
                    return;
                }
            }
#endif

            float deltaHeight =
#if ENABLE_INPUT_SYSTEM
                Mouse.current.scroll.ReadValue().y

#else
                Input.mouseScrollDelta.y
#endif

                ;

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

#if ENABLE_INPUT_SYSTEM
            // xxxjack need to port to InputSystem
#else
            // Camera Rotation when primary mouse button is pressed
            if (Input.GetKey(KeyCode.Mouse0))
            {
                float mouseX = Input.GetAxis("Mouse X") * xySensitivity * Time.deltaTime;
                float mouseY = Input.GetAxis("Mouse Y") * xySensitivity * Time.deltaTime;

                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                cameraTransformToControl.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                adjustBodyHead(mouseX, -mouseY);
            }

            if (allowHJKLforMouse)
            {
                // Use HJKL keys to simulate mouse movement, mainly for debugging
                // because mouse doesn't work over screen sharing connections.
                float hAngle = 0;
                float vAngle = 0;
                if (Input.GetKey(KeyCode.J)) hAngle = 5;
                if (Input.GetKey(KeyCode.L)) hAngle = -5;
                if (Input.GetKey(KeyCode.I)) vAngle = -5;
                if (Input.GetKey(KeyCode.K)) vAngle = 5;
                if (hAngle != 0 || vAngle != 0)
                {
                    xRotation += vAngle;
                    xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                    cameraTransformToControl.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

                    adjustBodyHead(hAngle, vAngle);
                }
            }
#endif
        }

        protected void adjustBodyHead(float hAngle, float vAngle)
        {
            playerBody.Rotate(Vector3.up, hAngle);
            avatarHead.Rotate(Vector3.right, vAngle);
        }
    }
}