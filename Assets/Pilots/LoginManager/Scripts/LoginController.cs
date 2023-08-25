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
            OrchestratorController.Instance.GetUsers();
            // The OrchestratorController is in DontDestroyOnLoad, so we don't have to wait for the GetUsers
            // response before loading the next scene (as long as we don't start acting on the data until the resonse has
            // been received).
            yield return null;
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
            string sceneName = PilotRegistry.Instance.GetSceneNameForPilotName(SessionConfig.Instance.scenarioName, SessionConfig.Instance.scenarioVariant); ;
            if (sceneName == null)
            {
                Debug.LogError($"{Name()}: Selected scenario \"{SessionConfig.Instance.scenarioName}\" not implemented in this player (unknown scene)");
                return;
            }

            if (loadCoroutine == null) loadCoroutine = StartCoroutine(RefreshAndLoad(sceneName));
#if xxxjack_old
           
            string[] msg = message.Split(new char[] { '_' });
            if (msg[0] == MessageType.START)
            {
                // Check Audio
                switch (msg[2])
                {
                    case "0": // No Audio
                        VRTConfig.Instance.protocolType = VRTConfig.ProtocolType.None;
                        break;
                    case "1": // Socket Audio
                        VRTConfig.Instance.protocolType = VRTConfig.ProtocolType.SocketIO;
                        break;
                    case "2": // Dash Audio
                        VRTConfig.Instance.protocolType = VRTConfig.ProtocolType.Dash;
                        break;
                    case "3": // Raw TCP
                        VRTConfig.Instance.protocolType = VRTConfig.ProtocolType.TCP;
                        break;
                    default:
                        Debug.LogError($"{Name()}: received unknown START audio type {msg[2]}");
                        break;
                }
                string pilotName = msg[1];
                string pilotVariant = null;
                if (msg.Length > 3 && msg[3] != "") pilotVariant = msg[3];
                if (msg.Length > 4 && msg[4] != "")
                {
                    VRTConfig.Instance.PCs.Codec = msg[4];
                }
                if (msg.Length > 5 && msg[5] != "")
                {
                    VRTConfig.Instance.Voice.Codec = msg[5];
                }
                string sceneName = PilotRegistry.Instance.GetSceneNameForPilotName(pilotName, pilotVariant);
                if (sceneName == null)
                {
                    Debug.LogError($"{Name()}: Selected scenario \"{pilotName}\" not implemented in this player (unknown scene)");
                    return;
                }
                
                if (loadCoroutine == null) loadCoroutine = StartCoroutine(RefreshAndLoad(sceneName));
            }
            else if (msg[0] == MessageType.READY)
            {
                // Do something to check if all the users are ready (future implementation)
            }
#endif
        }
    }

}