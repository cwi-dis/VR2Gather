using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Behaviour that adjusts the size of a user representation based on HMD position.
    /// </summary>
    public class SizeAdjust : MonoBehaviour
    {
        [Tooltip("GameObject tracking top of the real player")]
        public GameObject SourceTop;
        [Tooltip("GameObject tracking bottom of the real player")]
        public GameObject SourceBottom;
        [Tooltip("GameObject of which Y size will be adjusted (default: this GameObject)")]
        public GameObject Destination;
        [Tooltip("GameObject representing top of destination")]
        public GameObject DestinationTop;
        [Tooltip("GameObject representing bottom of destination")]
        public GameObject DestinationBottom;
        [Tooltip("If true set height at Start(). Otherwise only on AdjustHeight() callback")]
        public bool setHeightOnStart = true;
        [Tooltip("If true set height whenever HMD starts tracking. Otherwise only on AdjustHeight() callback")]
        public bool setHeightOnHMDTracking = true;
        [Tooltip("The Input System Action that determines whether the HMD is tracking")]
        [SerializeField] InputActionProperty m_hmdTrackingAction;
        [Tooltip("Native total height of destination (introspection)")]
        [DisableEditing][SerializeField] float nativeHeight = 1;
        [Tooltip("Current real player height (introspection)")]
        [DisableEditing][SerializeField] float actualHeight = 0;
        [Tooltip("Native size (introspection)")]
        [DisableEditing][SerializeField] Vector3 nativeSize;
        [Tooltip("Current size (introspection)")]
        [DisableEditing][SerializeField] Vector3 currentSize;
        [Tooltip("Enable debug logging")]
        [SerializeField] bool debug = false;


        // Start is called before the first frame update
        void Awake()
        {
            nativeSize = Destination.transform.localScale;
            currentSize = nativeSize;
            Vector3 topPoint = DestinationTop.transform.position;
            Vector3 bottomPoint = DestinationBottom.transform.position;
            nativeHeight = topPoint.y - bottomPoint.y;
            if (debug) Debug.Log($"SizeAdjust: nativeHeight={nativeHeight}");
        }

        private void Start()
        {
            if (setHeightOnStart)
            {
                AdjustHeight();
            }
        }

        private void Update()
        {
            if (setHeightOnHMDTracking)
            {
                if (m_hmdTrackingAction.action.WasPerformedThisFrame())
                {
                    AdjustHeight();
                }
            }
        }

        public void AdjustHeight()
        {
            if (!gameObject.activeInHierarchy || !enabled) return;
            float topY = SourceTop.transform.position.y;
            float botY = SourceBottom.transform.position.y;
            actualHeight = (topY - botY);
            if (actualHeight < 0.5)
            {
                Debug.LogWarning($"SizeAdjust: ignoring preposterous actualHeight={actualHeight}. sourceTop={SourceTop}, sourceBottom={SourceBottom}");
                return;
            }
            float factor = actualHeight / nativeHeight;
            if (factor < 0.5)
            {
                Debug.LogWarning($"SizeAdjust: ignoring preposterous factor={factor}, too small. nativeHeight={nativeHeight}, actualHeight={actualHeight}");
                return;
            }
            currentSize = nativeSize * factor;

            Destination.transform.localScale = currentSize;
            if (debug) Debug.Log($"SizeAdjust: height was {actualHeight}. currentSize={currentSize}. Now height is {SourceTop.transform.position.y - SourceBottom.transform.position.y}");
        }
    }
}

