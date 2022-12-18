using UnityEngine;
using VRT.Pilots.Common;

namespace VRT.Pilots.TechnicalPlayground
{
    public class TechnicalPlaygroundController : PilotController
    {
       
        public static TechnicalPlaygroundController Instance { get; private set; }

        public void Awake()
        {
            base.Awake();
        }

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
            if (enableFadeIn && CameraFader.Instance != null)
            {
                CameraFader.Instance.StartFadedOut = true;
                StartCoroutine(CameraFader.Instance.FadeIn());
            }
        }

        public override void MessageActivation(string message) { }
    }
}