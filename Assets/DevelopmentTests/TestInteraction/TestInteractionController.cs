using UnityEngine;
using VRT.Pilots.Common;

namespace VRT.DevelopmentTests
{
    public class TestInteractionController : MonoBehaviour
    {
        [Tooltip("Fade in at start of scene")]
        public bool enableFade;

        [Tooltip("The user (for enabling local)")]
        public VRT.Pilots.Common.NetworkPlayer player;
        public static TestInteractionController Instance { get; private set; }

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
        public void Start()
        {
            player.SetIsLocalPlayer(true);
            if (enableFade)
            {
                CameraFader.Instance.StartFadedOut = true;
                StartCoroutine(CameraFader.Instance.FadeIn());
            }
        }
    }
}