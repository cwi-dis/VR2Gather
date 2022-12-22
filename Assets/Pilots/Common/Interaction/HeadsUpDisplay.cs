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
    }

}
