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

    public void LoadAudio(PlayerManager player, OrchestratorWrapping.User user) {
        if (my_id == player.id) { // Sender
            var AudioBin2Dash = Config.Instance.LocalUser.PCSelfConfig.AudioBin2Dash;
            if (AudioBin2Dash == null)
                throw new System.Exception("EntityPipeline: missing self-user PCSelfConfig.AudioBin2Dash config");
            try {
                player.audio.AddComponent<VoiceSender>().Init(user, "audio", AudioBin2Dash.segmentSize, AudioBin2Dash.segmentLife, Config.Instance.protocolType == Config.ProtocolType.Dash); //Audio Pipeline
            }
            catch (System.EntryPointNotFoundException e) {
                Debug.LogError("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                throw new System.Exception("EntityPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
            }
        }
        else { // Receiver
            var AudioSUBConfig = Config.Instance.RemoteUser.AudioSUBConfig;
            if (AudioSUBConfig == null)
                throw new System.Exception("EntityPipeline: missing other-user AudioSUBConfig config");
            player.audio.AddComponent<VoiceReceiver>().Init(user, "audio", AudioSUBConfig.streamNumber, AudioSUBConfig.initialDelay, Config.Instance.protocolType == Config.ProtocolType.Dash); //Audio Pipeline
        }
    }

    public void LoadPlayers(PlayerManager[] players, PlayerManager[] spectators = null) {
        int playerIdx = 0;
        int spectatorIdx = 0;
        int id = 0;
        bool firstTVM = true;
        // First tell the tilingConfigDistributor what our user ID is.
        var tilingConfigDistributor = FindObjectOfType<TilingConfigDistributor>();
        if (tilingConfigDistributor == null)
        {
            Debug.LogWarning("No TilingConfigDistributor found");
        }
        tilingConfigDistributor?.Init(OrchestratorController.Instance.SelfUser.userId);

        foreach (OrchestratorWrapping.User user in OrchestratorController.Instance.ConnectedUsers) {
            if (user.userData.userRepresentationType != OrchestratorWrapping.UserData.eUserRepresentationType.__NONE__) {
                if (user.userData.userRepresentationType == OrchestratorWrapping.UserData.eUserRepresentationType.__SPECTATOR__) { // Load Spectator
                    // Activate the GO
                    spectators[spectatorIdx].gameObject.SetActive(true);

                    // Fill PlayerManager properties
                    spectators[spectatorIdx].id = id;
                    if (user.userName == OrchestratorController.Instance.SelfUser.userName) {
                        spectators[spectatorIdx].cam.gameObject.SetActive(true);
                        my_id = spectators[spectatorIdx].id;
                        spectators[spectatorIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                    }
                    else {
                        spectators[spectatorIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().InterpolateUpdates = true;
                    }
                    spectators[spectatorIdx].orchestratorId = user.userId;

                    // Load Audio
                    spectators[spectatorIdx].audio.SetActive(true);
                    LoadAudio(spectators[spectatorIdx], user);

                    spectatorIdx++;
                }
                else { // Load Players
                    // Activate the GO
                    players[playerIdx].gameObject.SetActive(true);

                    // Fill PlayerManager properties
                    players[playerIdx].id = id;
                    players[playerIdx].orchestratorId = user.userId;
                    players[playerIdx].userName.text = user.userName;
                    if (user.userName == OrchestratorController.Instance.SelfUser.userName) {
                        players[playerIdx].cam.gameObject.SetActive(true);
                        my_id = players[playerIdx].id;
                        players[playerIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                    }
                    else {
                        players[playerIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().InterpolateUpdates = true;
                    }

                    switch (user.userData.userRepresentationType) {
                        case OrchestratorWrapping.UserData.eUserRepresentationType.__2D__:
                            // FER: Implementacion representacion de webcam.
                            players[playerIdx].webcam.SetActive(true);
                            Config._User userCfg = my_id == players[playerIdx].id ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
                            players[playerIdx].webcam.AddComponent<WebCamPipeline>().Init(user, userCfg, Config.Instance.protocolType == Config.ProtocolType.Dash);
                            // Audio
                            players[playerIdx].audio.SetActive(true);
                            LoadAudio(players[playerIdx], user);
                            break;

                        case OrchestratorWrapping.UserData.eUserRepresentationType.__AVATAR__:
                            players[playerIdx].avatar.SetActive(true);
                            if (user.userName == OrchestratorController.Instance.SelfUser.userName) {
                                players[playerIdx].avatar.GetComponentInChildren<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                            }
                            else {
                                players[playerIdx].avatar.GetComponentInChildren<NetworkTransformSyncBehaviour>().InterpolateUpdates = true;
                            }
                            // Audio
                            players[playerIdx].audio.SetActive(true);
                            LoadAudio(players[playerIdx], user);
                            break;
                        case OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CERTH__:
                        case OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_SYNTH__:
                        case OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CWI_: // PC & AUDIO
                            players[playerIdx].pc.SetActive(true);
                            bool isSelf = my_id == players[playerIdx].id;
                            userCfg = isSelf ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
                            var pipeline = players[playerIdx].pc.AddComponent<EntityPipeline>();
                            pipeline.Init(user, userCfg);
                            // xxxjack debug code
                            if (isSelf)
                            {
                                SyncConfig syncConfig = pipeline.GetSyncConfig();
                                Debug.Log($"xxxjack sync: self EntityPipeline audio: {syncConfig.audio.wallClockTime}={syncConfig.audio.streamClockTime}, visual: {syncConfig.visuals.wallClockTime}={syncConfig.visuals.streamClockTime}");
                                var tileInfo = pipeline.GetTilingConfig();
                                Debug.Log($"xxxjack tiling: self: {JsonUtility.ToJson(tileInfo)}");
                            }
                            // Register for distribution of tiling configurations
                            tilingConfigDistributor?.RegisterPipeline(user.userId, pipeline);
                            break;
                        case OrchestratorWrapping.UserData.eUserRepresentationType.__TVM__: // TVM & AUDIO
                            if (user.userName == OrchestratorController.Instance.SelfUser.userName) {
                                players[playerIdx].tvm.transform.localPosition = new Vector3(PlayerPrefs.GetFloat("tvm_pos_x", 0), PlayerPrefs.GetFloat("tvm_pos_y", 0), PlayerPrefs.GetFloat("tvm_pos_z", 0));
                                players[playerIdx].tvm.transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat("tvm_rot_x", 0), PlayerPrefs.GetFloat("tvm_rot_y", 0), PlayerPrefs.GetFloat("tvm_rot_z", 0));
                            }
                            players[playerIdx].tvm.isMaster = firstTVM;
                            if (firstTVM) firstTVM = false;
                            players[playerIdx].tvm.connectionURI = user.userData.userMQurl;
                            players[playerIdx].tvm.exchangeName = user.userData.userMQexchangeName;
                            players[playerIdx].tvm.gameObject.SetActive(true);
                            // Audio
                            players[playerIdx].audio.SetActive(true);
                            LoadAudio(players[playerIdx], user);
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
        foreach (OrchestratorWrapping.User user in OrchestratorController.Instance.ConnectedUsers) {
            if (!firstPresenter) {
                if (user.userData.userRepresentationType != OrchestratorWrapping.UserData.eUserRepresentationType.__NONE__) {
                    if (user.userData.userRepresentationType == OrchestratorWrapping.UserData.eUserRepresentationType.__SPECTATOR__) { // Load Spectator
                        spectators[spectatorIdx].gameObject.SetActive(true);

                        // Fill PlayerManager properties
                        spectators[spectatorIdx].id = id;
                        spectators[spectatorIdx].orchestratorId = user.userId;
                        if (user.userName == OrchestratorController.Instance.SelfUser.userName) {
                            spectators[spectatorIdx].cam.gameObject.SetActive(true);
                            my_id = spectators[spectatorIdx].id;
                            spectators[spectatorIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                        }
                        else {
                            spectators[spectatorIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().InterpolateUpdates = true;
                        }

                        // Load Audio
                        spectators[spectatorIdx].audio.SetActive(true);
                        LoadAudio(spectators[spectatorIdx], user);

                        spectatorIdx++;
                    }
                    else { // Load Players
                        players[playerIdx].gameObject.SetActive(true);

                        // Fill PlayerManager properties
                        players[playerIdx].id = id;
                        players[playerIdx].orchestratorId = user.userId;
                        if (user.userName == OrchestratorController.Instance.SelfUser.userName) {
                            players[playerIdx].cam.gameObject.SetActive(true);
                            my_id = players[playerIdx].id;
                            players[playerIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                        }
                        else {
                            players[playerIdx].gameObject.GetComponent<NetworkTransformSyncBehaviour>().InterpolateUpdates = true;
                        }

                        switch (user.userData.userRepresentationType) {
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__2D__:
                                // FER: Implementacion representacion de webcam.
                                players[playerIdx].webcam.SetActive(true);
                                // Audio
                                players[playerIdx].audio.SetActive(true);
                                LoadAudio(players[playerIdx], user);
                                break;
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__AVATAR__:
                                players[playerIdx].avatar.SetActive(true);
                                if (user.userName == OrchestratorController.Instance.SelfUser.userName) {
                                    players[playerIdx].avatar.GetComponentInChildren<NetworkTransformSyncBehaviour>().SyncAutomatically = true;
                                }
                                else {
                                    players[playerIdx].avatar.GetComponentInChildren<NetworkTransformSyncBehaviour>().InterpolateUpdates = true;
                                }
                                // Audio
                                players[playerIdx].audio.SetActive(true);
                                LoadAudio(players[playerIdx], user);
                                break;
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CERTH__:
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_SYNTH__:
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CWI_: // PC & AUDIO
                                players[playerIdx].pc.SetActive(true);
                                Config._User userCfg = my_id == players[playerIdx].id ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
                                players[playerIdx].pc.AddComponent<EntityPipeline>().Init(user, userCfg);
                                break;
                            case OrchestratorWrapping.UserData.eUserRepresentationType.__TVM__: // TVM & AUDIO
                                if (user.userName == OrchestratorController.Instance.SelfUser.userName) {
                                    players[playerIdx].tvm.transform.localPosition = new Vector3(PlayerPrefs.GetFloat("tvm_pos_x", 0), PlayerPrefs.GetFloat("tvm_pos_y", 0), PlayerPrefs.GetFloat("tvm_pos_z", 0));
                                    players[playerIdx].tvm.transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat("tvm_rot_x", 0), PlayerPrefs.GetFloat("tvm_rot_y", 0), PlayerPrefs.GetFloat("tvm_rot_z", 0));
                                }
                                players[playerIdx].tvm.isMaster = firstTVM;
                                if (firstTVM) firstTVM = false;
                                players[playerIdx].tvm.connectionURI = user.userData.userMQurl;
                                players[playerIdx].tvm.exchangeName = user.userData.userMQexchangeName;
                                players[playerIdx].tvm.gameObject.SetActive(true);
                                // Audio
                                players[playerIdx].audio.SetActive(true);
                                LoadAudio(players[playerIdx], user);
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

