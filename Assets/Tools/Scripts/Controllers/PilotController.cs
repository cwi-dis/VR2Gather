using UnityEngine;
using UnityEngine.Video;

//public enum Actions { VIDEO_1_START, VIDEO_1_PAUSE, VIDEO_2_START, VIDEO_2_PAUSE, WAIT }

abstract public class PilotController : MonoBehaviour {

    [HideInInspector] public float timer = 0.0f;
    [HideInInspector] public int my_id;

    // Start is called before the first frame update
    public virtual void Start() {
        var tmp = Config.Instance;
    }

    public void LoadPlayers(PlayerManager[] players) {
        int playerIdx = 0;
        foreach (OrchestratorWrapping.User u in OrchestratorController.Instance.ConnectedUsers) {
            // Activate the GO
            players[playerIdx].gameObject.SetActive(true);

            // Fill PlayerManager properties
            players[playerIdx].id = playerIdx;
            if (u.userName == OrchestratorController.Instance.SelfUser.userName) {
                players[playerIdx].cam.gameObject.SetActive(true);
                my_id = players[playerIdx].id;
            }
            players[playerIdx].orchestratorId = u.userId;

            // TVM
            if (Config.Instance.userRepresentation == Config.UserRepresentation.TVM) {
                players[playerIdx].tvm.connectionURI = u.userData.userMQurl;
                players[playerIdx].tvm.exchangeName = u.userData.userMQexchangeName;
                players[playerIdx].tvm.gameObject.SetActive(true);
            }
            // PC & AUDIO
            if (Config.Instance.userRepresentation == Config.UserRepresentation.PC) {
                players[playerIdx].pc.SetActive(true);
                Config._User userCfg = my_id == players[playerIdx].id ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
                players[playerIdx].pc.AddComponent<EntityPipeline>().Init(players[playerIdx].orchestratorId, userCfg, u.sfuData.url_pcc, u.sfuData.url_audio);
            }

            playerIdx++;
        }
    }

    public void LoadPlayersWithPresenter(PlayerManager[] players) {
        int playerIdx = 0;
        bool firstPresenter = true;
        foreach (OrchestratorWrapping.User u in OrchestratorController.Instance.ConnectedUsers) {
            if (!firstPresenter) {
                // Activate the GO
                players[playerIdx].gameObject.SetActive(true);

                // Fill PlayerManager properties
                players[playerIdx].id = playerIdx;
                if (u.userName == OrchestratorController.Instance.SelfUser.userName) {
                    players[playerIdx].cam.gameObject.SetActive(true);
                    my_id = players[playerIdx].id;
                }
                players[playerIdx].orchestratorId = u.userId;

                // TVM
                if (Config.Instance.userRepresentation == Config.UserRepresentation.TVM) {
                    players[playerIdx].tvm.connectionURI = u.userData.userMQurl;
                    players[playerIdx].tvm.exchangeName = u.userData.userMQexchangeName;
                    players[playerIdx].tvm.gameObject.SetActive(true);
                }
                // PC & AUDIO
                if (Config.Instance.userRepresentation == Config.UserRepresentation.PC) {
                    players[playerIdx].pc.SetActive(true);
                    Config._User userCfg = my_id == players[playerIdx].id ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
                    players[playerIdx].pc.AddComponent<EntityPipeline>().Init(players[playerIdx].orchestratorId, userCfg, u.sfuData.url_pcc, u.sfuData.url_audio);
                }

                playerIdx++;
            }
            else {
                firstPresenter = false;
            }
        }
    }

    public abstract void MessageActivation(string message);
}

