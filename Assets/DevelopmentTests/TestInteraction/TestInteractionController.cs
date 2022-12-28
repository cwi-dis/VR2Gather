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
            Orchestrator.Wrapping.OrchestratorController.Instance.LocalUserSessionForDevelopmentTests();
            Orchestrator.Wrapping.User user = new Orchestrator.Wrapping.User()
            {
                userId = "no-userid",
                userName = "TestInteractionUser",
                userData = new Orchestrator.Wrapping.UserData()
                {
                    microphoneName = "None",
                    userRepresentationType = UserRepresentationType.__AVATAR__
                }
            };
            player.SetupPlayerNetworkControllerPlayer(true, user.userId);
            if (enableFade)
            {
                CameraFader.Instance.startFadedOut = true;
                StartCoroutine(CameraFader.Instance.FadeIn());
            }
           
            
            playerManager.SetUpPlayerController(true, user, null);
        }

  
    }
}