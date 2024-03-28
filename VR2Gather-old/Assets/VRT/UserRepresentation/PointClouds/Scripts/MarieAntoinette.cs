using UnityEngine;

namespace VRT.UserRepresentation.PointCloud
{
    /// <summary>
    /// Enable head-only pointclouds together with another representation.
    /// </summary>
    public class MarieAntoinette : MonoBehaviour
    {
        [Tooltip("The pointcloud representation")]
        public GameObject pc;
        [Tooltip("The other representation")]
        public GameObject other;
        [Tooltip("The gameObject with the headfilter (for the self-user only)")]
        public GameObject headFilter;
        [Tooltip("Enable marieAntoinette automatically (otherwise call SetMarieAntoinette() method)")]
        public bool autoEnable = false;

        // Start is called before the first frame update
        void Start()
        {
            if (autoEnable)
            {
                SetMarieAntoinette(true);
            }
        }

        private void OnDisable()
        {
            SetMarieAntoinette(false);
        }

        private void OnEnable()
        {
            if (autoEnable)
            {
                SetMarieAntoinette(true);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetMarieAntoinette(bool isEnabled)
        {
            if (isEnabled)
            {
                if (pc == null || !pc.activeSelf) {
                    Debug.LogWarning("MarieAntoinette: pointcloud not active");
                    return;
                }
                else
                {
                    if (headFilter != null)
                    {
                        PointCloudHeadFilter selfHeadFilter = headFilter.GetComponent<PointCloudHeadFilter>();
                        if (selfHeadFilter == null)
                        {
                            Debug.LogError("MarieAntoinette: PointCloudHeadFilter is required on some child GameObject");
                        }
                        else
                        {
                            selfHeadFilter.headOnly = true;
                            headFilter.SetActive(true);
                        }
                    }
                }
                other.SetActive(true);
                Debug.Log("MarieAntoinette: enabled");
            }
            else
            {
                if (headFilter != null)
                {
                    PointCloudHeadFilter selfHeadFilter = headFilter.GetComponent<PointCloudHeadFilter>();
                    if (selfHeadFilter != null)
                    {
                        selfHeadFilter.headOnly = false;
                        headFilter.SetActive(false);
                    }
                }
              
                other.SetActive(false);
                Debug.Log("MarieAntoinette: disabled");
            }
        }
    }
}

