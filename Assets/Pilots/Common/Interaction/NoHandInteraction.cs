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
        [Tooltip("Camera (default: main camera)")]
        Camera cam;
        [Tooltip("The Input System Action that determines whether we are pointing (if > 0.5)")]
        [SerializeField] InputActionProperty m_pointingAction;
        [Tooltip("The Input System Action that activates when we are pointing")]
        [SerializeField] InputActionProperty m_activateAction;

        [SerializeField][DisableEditing] bool pointing;
        [SerializeField][DisableEditing] bool hitting;

        private Texture2D curCursor;
        private Texture2D wantedCursor;
      

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
                EnablePointing(pointingNow);
            }
            if (pointing) CheckRay();
            FixCursor();
        }

        private void CheckRay()
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            int layerMask = LayerMask.GetMask("TouchableObject");
            Ray ray = cam.ScreenPointToRay(screenPos);
            RaycastHit hit = new RaycastHit();
            hitting = Physics.Raycast(ray, out hit, maxDistance, layerMask);
            wantedCursor = hitting ? castingCursorHitTexture : castingCursorTexture;
            if (hitting)
            {
                if (m_activateAction.action.IsPressed())
                {
                    var hitGO = hit.collider.gameObject;
                    Debug.Log($"NoHandInteraction: hitting={hitGO}");
                    var hitTrigger = hitGO.GetComponent<Trigger>();
                    if (hitTrigger)
                    {
                        hitTrigger.OnActivate();
                    }

                }
            }
        }

        private void EnablePointing(bool pointingNow)
        {
            pointing = pointingNow;
            Debug.Log($"NoHandInteraction: pointing={pointing}");
            if (pointing)
            {
                wantedCursor = castingCursorTexture;

            }
            else
            {
                wantedCursor = null;
            }
        }

        private void FixCursor()
        {
            if (curCursor != wantedCursor)
            {
                curCursor = wantedCursor;
                Cursor.SetCursor(curCursor, Vector2.zero, CursorMode.Auto);
            }
        }
    }

}
