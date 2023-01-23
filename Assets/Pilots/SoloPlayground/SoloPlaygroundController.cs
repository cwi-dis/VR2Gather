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
                    userRepresentationType = UserRepresentationType.__AVATAR__
                }
            };
            if (enableFade)
            {
                CameraFader.Instance.startFadedOut = true;
                StartCoroutine(CameraFader.Instance.FadeIn());
            }      
            playerManager.SetUpPlayerController(true, user);
        }
    }
}