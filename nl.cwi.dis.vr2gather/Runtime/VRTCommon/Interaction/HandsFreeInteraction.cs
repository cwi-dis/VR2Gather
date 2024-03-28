using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;


namespace VRT.Pilots.Common
{
    using HandState = HandDirectAppearance.HandState;

    /// <summary>
    /// Behaviour that allows enabling/disabling a ray-based interactor with keyboard/mouse or gamepad.
    /// </summary>
    public class HandsFreeInteraction : MonoBehaviour
    {
        [Tooltip("Camera for mouse pointing (default: main camera)")]
        Camera cam;
        [Tooltip("The Input System Action that determines whether we are pointing with the mouse")]
        [SerializeField] InputActionProperty m_pointingAction;
        [Tooltip("The Input System Action that determines whether we are sweeping with a gamepad")]
        [SerializeField] InputActionProperty m_sweepingAction;
        [Tooltip("The Input System Action that determines sweep delta")]
        [SerializeField] InputActionProperty m_sweepingDelta;
        [Tooltip("The speed of sweeping")]
        [SerializeField] float sweepSpeed = 1;
        [Tooltip("GameObject with the handsfree ray-based interactor")]
        public GameObject handsFreeInteractor;

        [Tooltip("Verbose messages")]
        [SerializeField] bool debugLog = false;
        [SerializeField][DisableEditing] bool pointing;
        [SerializeField][DisableEditing] bool sweeping;

      

        // Start is called before the first frame update
        void Start()
        {
            if (cam == null) cam = Camera.main;
        }

        // Update is called once per frame
        void Update()
        {
            bool pointingNow = m_pointingAction.action.IsPressed();
            if (pointingNow != pointing)
            {
                pointing = pointingNow;
                EnableRay();
            }
            bool sweepingNow = m_sweepingAction.action.IsPressed();
            if (sweeping != sweepingNow)
            {
                sweeping = sweepingNow;
                handsFreeInteractor.transform.localRotation = Quaternion.identity;
                EnableRay();
            }
            if (pointing) CheckMouseRay();
            if (sweeping) CheckSweep();
        }

        private void CheckMouseRay()
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(screenPos);
            RaycastHit hit = new RaycastHit();
            bool hitting = Physics.Raycast(ray, out hit);
            if (hitting)
            {
                Vector3 destinationPoint = hit.point;
                if (debugLog) Debug.Log($"NoHandInteraction: ray destination={destinationPoint}");
                Vector3 sourcePoint = handsFreeInteractor.transform.position;
                Vector3 direction = Vector3.Normalize(destinationPoint - sourcePoint);
                handsFreeInteractor.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void CheckSweep()
        {
            Vector2 delta = m_sweepingDelta.action.ReadValue<Vector2>();
            if (delta == Vector2.zero) return;
            if (debugLog) Debug.Log($"NoHandInteraction: sweepingDelta={delta}");
            handsFreeInteractor.transform.Rotate(-delta.y * Time.deltaTime * sweepSpeed, delta.x*Time.deltaTime*sweepSpeed, 0);
        }

        private void EnableRay()
        {
            if (debugLog) Debug.Log($"NoHandInteraction: pointing={pointing}, sweeping={sweeping}");
            handsFreeInteractor.SetActive(pointing||sweeping);
        }

    }

}
