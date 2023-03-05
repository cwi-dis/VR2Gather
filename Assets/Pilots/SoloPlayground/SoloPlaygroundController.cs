using UnityEngine;
using VRT.Core;
using VRT.Pilots.Common;

namespace VRT.Pilots.SoloPlayground
{
    public class SoloPlaygroundController : PilotController
    {
        [Tooltip("Fade in at start of scene")]
        public bool enableFade;

        [Tooltip("The user (for enabling isLocal)")]
        public VRT.Pilots.Common.PlayerNetworkControllerBase player;
        [Tooltip("The user (for setup camera position and input/output)")]
        public PlayerControllerSelf playerManager;
        [Tooltip("User representation")]
        public UserRepresentationType userRepresentation = UserRepresentationType.__AVATAR__;


        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
            Orchestrator.Wrapping.OrchestratorController.Instance.LocalUserSessionForDevelopmentTests();
            Orchestrator.Wrapping.User user = new Orchestrator.Wrapping.User()
            {
                userId = "no-userid",
                userName = "TestInteractionUser",
                userData = new Orchestrator.Wrapping.UserData()
                {
                    microphoneName = "None",
                    userRepresentationType = userRepresentation
                }
            };
            if (enableFade)
            {
                CameraFader.Instance.startFadedOut = true;
                StartCoroutine(CameraFader.Instance.FadeIn());
            }
            if (playerManager == null)
            {
                Debug.LogError($"{Name()}: playerManager field not set");
                return;
            }
            playerManager.SetUpPlayerController(true, user);
        }
    }
}