using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
using VRT.Pilots.Common;

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
           Orchestrator.Wrapping.OrchestratorController.Instance.LocalUserSessionForDevelopmentTests();
            Orchestrator.Responses.User user = new Orchestrator.Responses.User()
            {
                userId = "no-userid",
                userName = "TestInteractionUser",
                userData = new Orchestrator.Responses.UserData()
                {
                    microphoneName = "None",
                    userRepresentationType = userRepresentation
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

