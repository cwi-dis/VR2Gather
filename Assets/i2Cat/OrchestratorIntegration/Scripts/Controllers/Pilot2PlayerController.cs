using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrchestratorWrapping;
using UnityEngine.Video;

public class Pilot2PlayerController : PilotController {

    EntityPipeline[] p = new EntityPipeline[4];
    [SerializeField] VideoDashReceiver livePresenter;

    bool socket = false;
    bool useTVM = true;
    bool audioPCTogether = false;
    bool enter = false;

    [SerializeField] Pilot2DemoController demoController;
    
    public override void Start() {
        base.Start();
        orchestrator.controller = this;
        background.SetActive(false);

        //LivePresenter
        //livePresenter.url = orchestrator.livePresenterData.liveAddress;
        //livePresenter.gameObject.SetActive(true);
        
        masterID = orchestrator.activeSession.sessionUsers[0];

        if (orchestrator.isDebug) DebugIntro();

        for (int i = 1; i < orchestrator.activeSession.sessionUsers.Length; i++) {
            PlayerManager player;
            if (!orchestrator.isDebug) player = players[i - 1];
            else player = players[i];

            if (orchestrator.activeSession.sessionUsers[i] == orchestrator.userID) {
                player.cam.gameObject.SetActive(true);
                my_ID = player.id; // Save my ID.
            }

            foreach (User u in orchestrator.availableUsers) {
                if (u.userId == orchestrator.activeSession.sessionUsers[i]) {
                    // TVM
                    player.tvm.connectionURI = u.userData.userMQurl;
                    player.tvm.exchangeName = u.userData.userMQexchangeName;
                    player.tvm.gameObject.SetActive(useTVM);
                    // PC & Audio
                    if (!orchestrator.useSocketIOAudio) {
                        player.pc.gameObject.SetActive(!useTVM || !audioPCTogether);
                        if (my_ID == player.id) {
                            p[player.id - 1] = player.pc.gameObject.AddComponent<EntityPipeline>().Init(Config.Instance.Users[0], player.pc.transform, u.sfuData.url_pcc, u.sfuData.url_audio);
                        }
                        else {
                            p[player.id - 1] = player.pc.gameObject.AddComponent<EntityPipeline>().Init(Config.Instance.Users[3], player.pc.transform, u.sfuData.url_pcc, u.sfuData.url_audio);
                        }
                    }
                }
            }
        }        
    }

    public override void Update() {
        base.Update();

        //Audio Over Socket.io
        if (!enter) {
            if (timer >= 12.0f && AudioManager.instance != null && orchestrator.useSocketIOAudio) {
                AudioManager.instance.StartRecordAudio();

                foreach (string id in orchestrator.activeSession.sessionUsers) {
                    if (id != orchestrator.userID) {
                        AudioManager.instance.StartListeningAudio(id);
                    }
                }

                enter = true;
                timer = 0.0f;
            }
            else timer += Time.deltaTime;
        }


        //if (Input.GetKeyDown(KeyCode.Alpha1)) players[0].tvm.gameObject.SetActive(true);
        //if (Input.GetKeyDown(KeyCode.Alpha2)) players[1].tvm.gameObject.SetActive(true);

        if (todoAction != Actions.WAIT) {
            timer += Time.deltaTime;

            if (timer >= delay) {
                switch (todoAction) {
                    case Actions.VIDEO_1_START:
                        if (demoController.HowardLive.activeSelf) {
                            demoController.HowardLive.GetComponent<Animation>().Play();
                            videos[0].Play();
                        }
                        else demoController.HowardLive.SetActive(true);
                        break;
                    case Actions.VIDEO_1_PAUSE:
                        demoController.HowardLive.GetComponent<Animation>().Stop();
                        videos[0].Pause();
                        break;
                    case Actions.VIDEO_2_START:
                        if (demoController.DomeLive.activeSelf) videos[1].Play();
                        else {
                            demoController.OnDirectConnection();
                            demoController.DomeLive.SetActive(true);
                        }
                        break;
                    case Actions.VIDEO_2_PAUSE:
                        videos[1].Pause();
                        break;
                    default:
                        break;
                }
                timer = 0.0f;
                todoAction = Actions.WAIT;
            }
        }
    }

    public override void MessageActivation(string message) {
        string[] msg = message.Split(new char[] { '_' });
        if (msg[0] == MessageType.LIVESTREAM) {

        }
        else if (msg[0] == MessageType.PLAY) {
            if (msg[1] == "1") todoAction = Actions.VIDEO_1_START;
            else if (msg[1] == "2") todoAction = Actions.VIDEO_2_START;
            delay = 1.3f;
            //delay = (float)SyncTool.GetDelay(SyncTool.ToDateTime(msg[2]));
            Debug.Log(delay);
        }
        else if (msg[0] == MessageType.PAUSE) {
            if (msg[1] == "1") todoAction = Actions.VIDEO_1_PAUSE;
            else if (msg[1] == "2") todoAction = Actions.VIDEO_2_PAUSE;
            delay = 1.3f;
            //delay = (float)SyncTool.GetDelay(SyncTool.ToDateTime(msg[2]));
            Debug.Log(delay);
        }
    }

    public void DebugIntro() {
        PlayerManager player = players[0];

        if (orchestrator.activeSession.sessionUsers[0] == orchestrator.userID) {
            player.cam.gameObject.SetActive(true);
            my_ID = player.id; // Save my ID.
        }

        foreach (User u in orchestrator.availableUsers) {
            if (u.userId == orchestrator.activeSession.sessionUsers[0]) {
                // TVM
                player.tvm.connectionURI = u.userData.userMQurl;
                player.tvm.exchangeName = u.userData.userMQexchangeName;
                player.tvm.gameObject.SetActive(useTVM);
                // PC & Audio
                if (!orchestrator.useSocketIOAudio) {
                    player.pc.gameObject.SetActive(!useTVM || !audioPCTogether);
                    if (my_ID == player.id) {
                        p[player.id - 1] = player.pc.gameObject.AddComponent<EntityPipeline>().Init(Config.Instance.Users[0], player.pc.transform, u.sfuData.url_pcc, u.sfuData.url_audio);
                    }
                    else {
                        p[player.id - 1] = player.pc.gameObject.AddComponent<EntityPipeline>().Init(Config.Instance.Users[3], player.pc.transform, u.sfuData.url_pcc, u.sfuData.url_audio);
                    }
                }
            }
        }
    }
}
