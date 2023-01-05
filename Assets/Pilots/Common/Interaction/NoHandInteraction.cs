using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;


namespace VRT.Pilots.Common
{
    using HandState = Hand.HandState;

    /// <summary>
    /// Behaviour that allows using the mouse to activate things.
    /// </summary>
    public class NoHandInteraction : MonoBehaviour
    {
        [Tooltip("Mouse cursor to use while looking for touchable items")]
        public Texture2D castingCursorTexture;
        [Tooltip("Mouse cursor to use when over a touchable item")]
        public Texture2D castingCursorHitTexture;
        [Tooltip("Maximum distance of touchable objects")]
        public float maxDistance = Mathf.Infinity;

        [Tooltip("The Input System Action that determines whether we are pointing (if > 0.5)")]
        [SerializeField] InputActionProperty m_pointingAction;
        [Tooltip("The Input System Action that activates when we are pointing")]
        [SerializeField] InputActionProperty m_activateAction;

        [DisableEditing] bool pointing;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            bool pointingNow = m_pointingAction.action.IsPressed();
            if (pointingNow != pointing)
            {
                EnablePointing(pointingNow);
            }
        }

        private void EnablePointing(bool pointingNow)
        {
            pointing = pointingNow;
            Debug.Log($"NoHandInteraction: pointing={pointing}");
            if (pointing)
            {
                // Enable ray
                // Fix mouse cursor
            }
            else
            {
                // Disable ray
                // Fix mouse cursor
            }
        }
    }

}
