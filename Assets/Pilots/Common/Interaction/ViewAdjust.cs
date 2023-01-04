using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace VRT.Pilots.Common
{
    public class ViewAdjust : LocomotionProvider
    {
        [Tooltip("The player controller, for saving the view origin")]
        [SerializeField] PlayerControllerSelf playerController;

        [Tooltip("The object of which the height is adjusted, and that resetting origin will modify")]
        [SerializeField] GameObject cameraOffset;

        [Tooltip("Toplevel object of this player, usually the XROrigin, for resetting origin")]
        [SerializeField] GameObject player;

        [Tooltip("Camera used for determining zero position, for resetting origin")]
        [SerializeField] Camera playerCamera;

        [Tooltip("Multiplication factor for height adjustment")]
        [SerializeField] float heightFactor = 1;

        [Tooltip("The Input System Action that will be used to change view height. Must be a Value Vector2 Control of which y is used.")]
        [SerializeField] InputActionProperty m_ViewHeightAction;

        [Tooltip("Use Reset Origin action. Unset if ResetOrigin() is called from a script.")]
        [SerializeField] bool useResetOriginAction = true;

        [Tooltip("The Input System Action that will be used to reset view origin.")]
        [SerializeField] InputActionProperty m_resetOriginAction;

        public bool debugLogging = false;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            Vector2 heightInput = m_ViewHeightAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
            float deltaHeight = heightInput.y * heightFactor;
            if (deltaHeight != 0 && BeginLocomotion())
            {
                cameraOffset.transform.position += new Vector3(0, deltaHeight, 0);
                // Note: we don't save height changes. But if you reset view position
                // afterwards we do also save height changes.
                EndLocomotion();
            }
            if (useResetOriginAction && m_resetOriginAction != null)
            {
                bool doResetOrigin = m_resetOriginAction.action.ReadValue<float>() >= 0.5;
                if (doResetOrigin)
                {
                    ResetOrigin();
                }
            }
        }

        void ResetOrigin()
        {
            if (BeginLocomotion())
            {
                if (debugLogging) Debug.Log($"ViewAdjust: reset origin");
                float rotationY = playerCamera.transform.rotation.eulerAngles.y - player.transform.rotation.eulerAngles.y;
                cameraOffset.transform.Rotate(0, -rotationY, 0);
                //Vector3 moveXZ = playerCamera.transform.position - cameraOffset.transform.position;
                Vector3 moveXZ = playerCamera.transform.position - player.transform.position;
                moveXZ.y = 0;
                cameraOffset.transform.position -= moveXZ;
                if (playerController != null)
                {
                    playerController.SaveCameraTransform();
                }
                EndLocomotion();
            }
        }

        protected void OnEnable()
        {
            m_ViewHeightAction.EnableDirectAction();
            if (useResetOriginAction) m_resetOriginAction.EnableDirectAction();
        }

        protected void OnDisable()
        {
            m_ViewHeightAction.DisableDirectAction();
            if (useResetOriginAction) m_resetOriginAction.DisableDirectAction();
        }
    }
}