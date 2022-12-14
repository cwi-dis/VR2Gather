using UnityEngine;
using VRT.Pilots.Common;

namespace VRT.Pilots.TechnicalPlayground
{
    public class TechnicalPlaygroundController : PilotController
    {
        [Tooltip("Fade in at start of scene")]
        public bool enableFade;

        public static TechnicalPlaygroundController Instance { get; private set; }

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            if (enableFade && CameraFader.Instance != null)
            {
                CameraFader.Instance.StartFadedOut = true;
            }
        }

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
            if (enableFade && CameraFader.Instance != null)
            {
                CameraFader.Instance.StartFadedOut = true;
                StartCoroutine(CameraFader.Instance.FadeIn());
            }
        }

        public override void MessageActivation(string message) { }
    }
}