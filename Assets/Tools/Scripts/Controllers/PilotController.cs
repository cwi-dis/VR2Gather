using UnityEngine;
using UnityEngine.Video;

//public enum Actions { VIDEO_1_START, VIDEO_1_PAUSE, VIDEO_2_START, VIDEO_2_PAUSE, WAIT }

abstract public class PilotController : MonoBehaviour {

    [HideInInspector] public float timer = 0.0f;
    [HideInInspector] public int my_id;

    // Start is called before the first frame update
    public virtual void Start() {
        var tmp = Config.Instance;
        my_id = -1;
    }

    public void LoadAudio(PlayerManager player, OrchestratorWrapping.User u) {
        if (my_id == player.id) { // Sender
            if (Config.Instance.audioType == Config.AudioType.Dash) {
                var AudioBin2Dash = Config.Instance.LocalUser.PCSelfConfig.AudioBin2Dash;
                if (AudioBin2Dash == null)
                    throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.AudioBin2Dash config");
                try {
                    player.audio.AddComponent<VoiceDashSender>().Init(u.sfuData.url_audio, "audio", AudioBin2Dash.segmentSize, AudioBin2Dash.segmentLife); //Audio Pipeline
                }
                catch (System.EntryPointNotFoundException e) {
                    Debug.LogError("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                    throw new System.Exception("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                }
            }
            else if (Config.Instance.audioType == Config.AudioType.SocketIO) {
                player.audio.AddComponent<VoiceIOSender>().Init(u.sfuData.url_audio, "audio");
            }
        }
        else { // Receiver
            if (Config.Instance.audioType == Config.AudioType.Dash) {
                var AudioSUBConfig = Config.Instance.RemoteUser.AudioSUBConfig;
                if (AudioSUBConfig == null)
                    throw new System.Exception("EntityPipeline: missing other-user AudioSUBConfig config");
                player.audio.AddComponent<VoiceDashReceiver>().Init(u.sfuData.url_audio, "audio", AudioSUBConfig.streamNumber, AudioSUBConfig.initialDelay); //Audio Pipeline
            }
            else if (Config.Instance.audioType == Config.AudioType.SocketIO) {
                player.audio.AddComponent<VoiceIOReceiver>().Init(u.sfuData.url_audio, "audio"); //Audio Pipeline
            }
        }
    }

    public void LoadPlayers(PlayerManager[] players, PlayerManager[] spectators = null) {
        int playerIdx = 0;
        int spectatorIdx = 0;
        int id = 0;
        bool firstTVM = true;
        foreach (OrchestratorWrapping.User u in OrchestratorController.Instance.ConnectedUsers) {
            if (u.userData.userRepresentationType != OrchestratorWrapping.UserData.eUserRepresentationType.__NONE__) {
                if (u.userData.userRepresentationType == OrchestratorWrapping.UserData.eUserRepresentationType.__SPECTATOR__) { // Load Spectator
                    // Activate the GO
                    spectators[spectatorIdx].gameObject.SetActive(true);

                    // Fill PlayerManager properties
                    spectators[spectatorIdx].id = id;
                    if (u.userName == OrchestratorController.Instance.SelfUser.userName) {
                        spectators[spectatorIdx].cam.gameObject.SetActive(true);
                        my_id = spectators[spectatorIdx].id;
                        spectators[spectatorIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                    }
                    spectators[spectatorIdx].orchestratorId = u.userId;

                    // Load Audio
                    spectators[spectatorIdx].audio.SetActive(true);
                    LoadAudio(spectators[spectatorIdx], u);

                    spectatorIdx++;
                }
                else { // Load Players
                    // Activate the GO
                    players[playerIdx].gameObject.SetActive(true);

                    // Fill PlayerManager properties
                    players[playerIdx].id = id;
                    players[playerIdx].orchestratorId = u.userId;
                    players[playerIdx].userName.text = u.userName;
                    if (u.userName == OrchestratorController.Instance.SelfUser.userName) {
                        players[playerIdx].cam.gameObject.SetActive(true);
                        my_id = players[playerIdx].id;
                        players[playerIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                    }

                    switch (u.userData.userRepresentationType) {
                        case OrchestratorWrapping.UserData.eUserRepresentationType.__2D__:
                        case OrchestratorWrapping.UserData.eUserRepresentationType.__AVATAR__:
                            players[playerIdx].avatar.SetActive(true);
                            if (u.userName == OrchestratorController.Instance.SelfUser.userName) {
                                players[playerIdx].avatar.GetComponentInChildren<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                            }
                            // Audio
                            players[playerIdx].audio.SetActive(true);
                            LoadAudio(players[playerIdx], u);
                            break;
                        case OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CERTH__:
                        case OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CWI_: // PC & AUDIO
                            players[playerIdx].pc.SetActive(true);
                            Config._User userCfg = my_id == players[playerIdx].id ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
                            players[playerIdx].pc.AddComponent<EntityPipeline>().Init(players[playerIdx].orchestratorId, userCfg, u.sfuData.url_pcc, u.sfuData.url_audio);
                            // xxxjack debug code
                            {
                                EntityPipeline selfPipeline = players[playerIdx].pc.GetComponent<EntityPipeline>();
                                if (selfPipeline == null)
                                {
                                    Debug.Log($"xxxjack sync: self EntityPipeline is null");
                                }
                                else
                                {
                                    SyncConfig syncConfig = selfPipeline.GetSyncConfig();
                                    Debug.Log($"xxxjack sync: self EntityPipeline audio: {syncConfig.audio.wallClockTime}={syncConfig.audio.streamClockTime}, visual: {syncConfig.visuals.wallClockTime}={syncConfig.visuals.streamClockTime}");
                                    var tileInfo = selfPipeline.GetTilingConfig();
                                    Debug.Log($"xxxjack tiling: self: {JsonUtility.ToJson(tileInfo)}");
                                }
                            }
                            break;
                        case OrchestratorWrapping.UserData.eUserRepresentationType.__TVM__: // TVM & AUDIO
                            if (u.userName == OrchestratorController.Instance.SelfUser.userName) {
                                players[playerIdx].tvm.transform.localPosition = new Vector3(PlayerPrefs.GetFloat("tvm_pos_x", 0), PlayerPrefs.GetFloat("tvm_pos_y", 0), PlayerPrefs.GetFloat("tvm_pos_z", 0));
                                players[playerIdx].tvm.transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat("tvm_rot_x", 0), PlayerPrefs.GetFloat("tvm_rot_y", 0), PlayerPrefs.GetFloat("tvm_rot_z", 0));
                            }
                            players[playerIdx].tvm.isMaster = firstTVM;
                            if (firstTVM) firstTVM = false;
                            players[playerIdx].tvm.connectionURI = u.userData.userMQurl;
                            players[playerIdx].tvm.exchangeName = u.userData.userMQexchangeName;
                            players[playerIdx].tvm.gameObject.SetActive(true);
                            // Audio
                            players[playerIdx].audio.SetActive(true);
                            LoadAudio(players[playerIdx], u);
                            break;
                        default:
                            break;
                    }

                    playerIdx++;
                }
                id++;
            }
        }
    }

    public void LoadPlayersWithPresenter(PlayerManager[] players, PlayerManager[] spectators = null) {
        int playerIdx = 0;
        int spectatorIdx = 0;
        int id = 0;
        bool firstTVM = true;
        bool firstPresenter = true;
        foreach (OrchestratorWrapping.User u in OrchestratorController.Instance.ConnectedUsers) {
            if (!firstPresenter) {
                if (u.userData.userRepresentationType != OrchestratorWrapping.UserData.eUserRepresentationType.__NONE__) {
                    if (u.userData.userRepresentationType == OrchestratorWrapping.UserData.eUserRepresentationType.__SPECTATOR__) { // Load Spectator
                        spectators[spectatorIdx].gameObject.SetActive(true);

                        // Fill PlayerManager properties
                        spectators[spectatorIdx].id = id;
                        spectators[spectatorIdx].orchestratorId = u.userId;
                        if (u.userName == OrchestratorController.Instance.SelfUser.userName) {
                            spectators[spectatorIdx].cam.gameObject.SetActive(true);
                            my_id = spectators[spectatorIdx].id;
                            spectators[spectatorIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                        }

                        // Load Audio
                        spectators[spectatorIdx].audio.SetActive(true);
                        LoadAudio(spectators[spectatorIdx], u);

                        spectatorIdx++;
                    }
                    else { // Load Players
                        players[playerIdx].gameObject.SetActive(true);

                        // Fill PlayerManager properties
                        players[playerIdx].id = id;
                        players[playerIdx].orchestratorId = u.userId;
                        if (u.userName == OrchestratorController.Instance.SelfUser.userName) {
                            players[playerIdx].cam.gameObject.SetActive(true);
                            my_id = players[playerIdx].id;
                            players[playerIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                        }

                        switch (u.userData.userRepresentationType) {
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__2D__:
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__AVATAR__:
                                players[playerIdx].avatar.SetActive(true);
                                if (u.userName == OrchestratorController.Instance.SelfUser.userName) {
                                    players[playerIdx].avatar.GetComponentInChildren<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                                }
                                // Audio
                                players[playerIdx].audio.SetActive(true);
                                LoadAudio(players[playerIdx], u);
                                break;
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CERTH__:
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CWI_: // PC & AUDIO
                                players[playerIdx].pc.SetActive(true);
                                Config._User userCfg = my_id == players[playerIdx].id ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
                                players[playerIdx].pc.AddComponent<EntityPipeline>().Init(players[playerIdx].orchestratorId, userCfg, u.sfuData.url_pcc, u.sfuData.url_audio);
                                break;
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__TVM__: // TVM & AUDIO
                                if (u.userName == OrchestratorController.Instance.SelfUser.userName) {
                                    players[playerIdx].tvm.transform.localPosition = new Vector3(PlayerPrefs.GetFloat("tvm_pos_x", 0), PlayerPrefs.GetFloat("tvm_pos_y", 0), PlayerPrefs.GetFloat("tvm_pos_z", 0));
                                    players[playerIdx].tvm.transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat("tvm_rot_x", 0), PlayerPrefs.GetFloat("tvm_rot_y", 0), PlayerPrefs.GetFloat("tvm_rot_z", 0));
                                }
                                players[playerIdx].tvm.isMaster = firstTVM;
                                if (firstTVM) firstTVM = false;
                                players[playerIdx].tvm.connectionURI = u.userData.userMQurl;
                                players[playerIdx].tvm.exchangeName = u.userData.userMQexchangeName;
                                players[playerIdx].tvm.gameObject.SetActive(true);
                                // Audio
                                players[playerIdx].audio.SetActive(true);
                                LoadAudio(players[playerIdx], u);
                                break;
                            default:
                                break;
                        }

                        playerIdx++;
                    }
                    id++;
                }
            }
            else {
                firstPresenter = false;
            }
        }
    }

    public abstract void MessageActivation(string message);
}

