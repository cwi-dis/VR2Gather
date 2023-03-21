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
        [Tooltip("GameObject tracking HMD height Y (default: Main Camera)")]
        public GameObject HeightSource;
        [Tooltip("GameObject tracking floor height Y (default: 0")]
        public GameObject FloorSource;
        [Tooltip("GameObject of which Y size will be adjusted (default: this GameObject)")]
        public GameObject Destination;
        [Tooltip("Factor by which height will be multiplied when applying to destination Y size")]
        public float heightFactor = 1;
        [Tooltip("Factor by which height will be multiplied to set X and Z size (default: no change")]
        public float widthFactor = 0;
        [Tooltip("If true set height at Start(). Otherwise only on AdjustHeight() callback")]
        public bool setHeightOnStart = true;
        [Tooltip("Number of frame to delay setting height on start")]
        [SerializeField] private int delayFrameCount = 5;
        [Tooltip("Current size (introspection)")]
        [DisableEditing][SerializeField] Vector3 currentSize;


        // Start is called before the first frame update
        void Awake()
        {
            if (HeightSource == null) HeightSource = Camera.main.gameObject;
            if (Destination == null) Destination = gameObject;
            currentSize = Destination.transform.localScale;
        }

        private void Start()
        {
            if (setHeightOnStart)
            {

            }
        }

        IEnumerator adjustAfterDelay()
        {
            while (delayFrameCount > 0)
            {
                yield return null;
                delayFrameCount--;
            }
            AdjustHeight();
        }
        public void AdjustHeight()
        {
            if (!gameObject.activeInHierarchy || !enabled) return;
            float topY = HeightSource.transform.position.y;
            float botY = 0;
            if (FloorSource != null) botY = FloorSource.transform.position.y;
            float height = (topY - botY);
            currentSize = Destination.transform.localScale;
            currentSize.y = height * heightFactor;
            if (widthFactor != 0)
            {
                currentSize.x = height * widthFactor;
                currentSize.z = height * widthFactor;
            }
            Destination.transform.localScale = currentSize;
            Debug.Log($"SizeAdjust: size={currentSize}");
        }
    }
}

