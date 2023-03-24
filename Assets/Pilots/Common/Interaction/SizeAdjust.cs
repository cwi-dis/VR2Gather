using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        [Tooltip("Native total height of destination (introspection)")]
        [DisableEditing] [SerializeField] float nativeHeight = 1;
        [Tooltip("Current real player height (introspection)")]
        [DisableEditing] [SerializeField] float actualHeight = 0;
        [Tooltip("Native size (introspection)")]
        [DisableEditing] [SerializeField] Vector3 nativeSize;
        [Tooltip("Current size (introspection)")]
        [DisableEditing] [SerializeField] Vector3 currentSize;


        // Start is called before the first frame update
        void Awake()
        {
            nativeSize = Destination.transform.localScale;
            currentSize = nativeSize;
            Vector3 topPoint = DestinationTop.transform.position;
            Vector3 bottomPoint = DestinationBottom.transform.position;
            nativeHeight = topPoint.y - bottomPoint.y;
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
            
        }

        public void AdjustHeight()
        {
            if (!gameObject.activeInHierarchy || !enabled) return;
            float topY = SourceTop.transform.position.y;
            float botY = SourceBottom.transform.position.y;
            actualHeight = (topY - botY);
            float factor = actualHeight / nativeHeight;
            currentSize = nativeSize * factor;
            
            Destination.transform.localScale = currentSize;
            Debug.Log($"SizeAdjust: size={currentSize}");
        }
    }
}

