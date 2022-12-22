using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace VRT.Pilots.Common
{
    public class HeadsUpDisplay : MonoBehaviour
    {
        [Tooltip("The Input System Action that will show/hide the HUD")]
        [SerializeField] InputActionProperty m_ShowHideAction;
        [Tooltip("The canvas to show/hide")]
        [SerializeField] GameObject canvas;
        [Tooltip("How far away is the HMD from the users eyes?")]
        public float distance = 1;
        [Tooltip("How far up/down from the center is the HMD?")]
        public float height = 0;
        [Tooltip("How fast should it move when the user changes position/orientation?")]
        public float velocity = 0.01f;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (m_ShowHideAction.action.WasPressedThisFrame())
            {
                canvas.SetActive(!canvas.activeSelf);
                Debug.Log($"xxxjack toggle HUD, now {canvas.activeSelf}");
            }
        }
        void LateUpdate()
        {
            Vector3 forward = Camera.main.transform.forward;
            forward.y = height;
            forward = forward.normalized;

            Vector3 position = Camera.main.transform.position + forward * distance;
            Quaternion rotation = Quaternion.LookRotation(forward);
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, velocity);
            transform.position = Vector3.Lerp(transform.position, position, velocity);
            //transform.localScale = Vector3.Lerp(transform.localScale, scale, scaleVel);
        }
    }

}
