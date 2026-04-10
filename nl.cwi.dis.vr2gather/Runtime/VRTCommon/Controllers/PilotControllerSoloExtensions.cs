using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Extensions to PilotController for solo experiences.
    /// </summary>
    public class PilotControllerSoloExtensions : MonoBehaviour
    {
        [Tooltip("The user (for setup camera position and input/output)")]
        public PlayerControllerSelf playerManager;
        [Tooltip("User representation")]
        public UserRepresentationType userRepresentation = UserRepresentationType.SimpleAvatar;

        public void Start()
        {
            Orchestrator.Elements.User user = new Orchestrator.Elements.User()
            {
                userId = "no-userid",
                userName = "TestInteractionUser",
                userData = new Orchestrator.Elements.UserData()
                {
                    userRepresentation = userRepresentation
                }
            };
           
            if (playerManager == null)
            {
                Debug.LogError($"{name}: playerManager field not set");
                return;
            }
            playerManager.gameObject.SetActive(true);
            playerManager.SetUpPlayerController(true, user);
        }

    }
}

