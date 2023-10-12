using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using VRT.Orchestrator.Wrapping;
using VRT.Pilots.Common;
using VRT.Core;

namespace VRT.Pilots.LoginManager
{

    public class LoginController : PilotController
    {
        [Tooltip("The self-player for the login scene")]
        [SerializeField] public PlayerControllerSelf selfPlayer;

       
        //AsyncOperation async;
        Coroutine loadCoroutine = null;

        public override void Start()
        {
            // Do not call base.Start(), we don't want to fade in for the login scene.
            Orchestrator.Wrapping.User user = new Orchestrator.Wrapping.User()
            {
                userId = "no-userid",
                userName = "TestInteractionUser",
                userData = new Orchestrator.Wrapping.UserData()
                {
                    microphoneName = "None",
                    userRepresentationType = UserRepresentationType.SimpleAvatar // xxxjack need correct one.
                }
            };
            selfPlayer.SetUpPlayerController(true, user);
        }

        IEnumerator RefreshAndLoad(string scenary)
        {
            yield return null;
#if orch_removed_2
            OrchestratorController.Instance.GetUsers();
            // The OrchestratorController is in DontDestroyOnLoad, so we don't have to wait for the GetUsers
            // response before loading the next scene (as long as we don't start acting on the data until the resonse has
            // been received).
            yield return null;
#endif
            LoadNewScene(scenary);
        }

        public override void OnUserMessageReceived(string message)
        {
            Debug.Log($"{Name()}: OnUserMessageReceived: {message}");
            if (!message.StartsWith("START_")) {
                Debug.LogError("LoginController: only expecting START_ messages");
                return;
            }
            message = message.Substring(6);
            SessionConfig.FromJson(message);
            string sceneName = ScenarioRegistry.Instance.GetSceneNameForSession(SessionConfig.Instance);
            if (sceneName == null)
            {
                Debug.LogError($"{Name()}: Selected scenario \"{SessionConfig.Instance.scenarioName}\" not implemented in this player (unknown scene)");
                return;
            }

            if (loadCoroutine == null) loadCoroutine = StartCoroutine(RefreshAndLoad(sceneName));

        }
    }

}