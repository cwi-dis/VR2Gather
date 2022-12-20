using UnityEngine;
using VRT.Core;
using VRT.Pilots.Common;

namespace VRT.DevelopmentTests
{
    public class TestInteractionController : MonoBehaviour
    {
        [Tooltip("Fade in at start of scene")]
        public bool enableFade;

        [Tooltip("The user (for enabling isLocal)")]
        public VRT.Pilots.Common.PlayerNetworkController player;
        [Tooltip("The user (for setup camera position and input/output)")]
        public PlayerControllerSelf playerManager;

        bool pmSetupDone = false;
        public static TestInteractionController Instance { get; private set; }

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            if (enableFade && CameraFader.Instance != null)
            {
                CameraFader.Instance.startFadedOut = true;
            }
        }

        // Start is called before the first frame update
        public void Start()
        {
            player.SetupPlayerNetworkControllerPlayer(true, "no-userid");
            if (enableFade)
            {
                CameraFader.Instance.startFadedOut = true;
                StartCoroutine(CameraFader.Instance.FadeIn());
            }
        }

        public void Update()
        {
            if (pmSetupDone) return;
            playerManager.setupCamera(true);
            pmSetupDone = true;
        }
    }
}