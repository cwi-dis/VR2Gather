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

        public class MessageType
        {
            // Bad magic. These are prefixes generated (I think) by the orchestrator)
            public const string START = "START";
            public const string READY = "READY";
        }
      
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
                    userRepresentationType = UserRepresentationType.__AVATAR__ // xxxjack need correct one.
                }
            };
            selfPlayer.SetUpPlayerController(true, user, null);
        }

        IEnumerator RefreshAndLoad(string scenary)
        {
            yield return null;
            OrchestratorController.Instance.GetUsers();
            yield return new WaitForSeconds(0.5f);
            LoadNewScene(scenary);
        }

        public override void OnUserMessageReceived(string message)
        {
            Debug.Log($"{Name()}: OnUserMessageReceived: {message}");
            string[] msg = message.Split(new char[] { '_' });
            if (msg[0] == MessageType.START)
            {
                // Check Audio
                switch (msg[2])
                {
                    case "0": // No Audio
                        Config.Instance.protocolType = Config.ProtocolType.None;
                        break;
                    case "1": // Socket Audio
                        Config.Instance.protocolType = Config.ProtocolType.SocketIO;
                        break;
                    case "2": // Dash Audio
                        Config.Instance.protocolType = Config.ProtocolType.Dash;
                        break;
                    case "3": // Raw TCP
                        Config.Instance.protocolType = Config.ProtocolType.TCP;
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
                    Config.Instance.PCs.Codec = msg[4];
                }
                if (msg.Length > 5 && msg[5] != "")
                {
                    Config.Instance.Voice.Codec = msg[5];
                }
                string sceneName = PilotRegistry.GetSceneNameForPilotName(pilotName, pilotVariant);
                if (sceneName == null)
                {
                    throw new System.Exception($"{Name()}: Selected scenario \"{sceneName}\" not implemented in this player");
                }
                if (loadCoroutine == null) loadCoroutine = StartCoroutine(RefreshAndLoad(sceneName));
            }
            else if (msg[0] == MessageType.READY)
            {
                // Do something to check if all the users are ready (future implementation)
            }
        }
    }

}