using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrchestratorWrapping;
using UnityEngine.Video;

public class Pilot2PlayerController : PilotController {

    EntityPipeline[] p = new EntityPipeline[4];

    bool socket = false;
    
    public override void Start() {
        base.Start();
        orchestrator.controller = this;
        background.SetActive(false);

        masterID = orchestrator.activeSession.sessionUsers[0];

        if (orchestrator.isDebug) DebugIntro();

        for (int i = 1; i < orchestrator.activeSession.sessionUsers.Length; i++) {
            PlayerManager player;
            if (!orchestrator.isDebug) player = players[i - 1];
            else player = players[i];

            if (orchestrator.activeSession.sessionUsers[i] == orchestrator.userID) {
                player.cam.gameObject.SetActive(true);
                my_ID = player.id; // Save my ID.
                //if (socket) { // Audio Socket Sender
                //    audioController.userID = my_ID;
                //    audioController.gameObject.SetActive(true);
                //}
                //else { // Audio Dash Sender
                //    player.audioSender.AddComponent<VoiceDashSender>().Init(Config.Instance.Users[0].PCSelfConfig.AudioBin2Dash, player.id.ToString());
                //}
            }

            foreach (User u in orchestrator.availableUsers) {
                if (u.userId == orchestrator.activeSession.sessionUsers[i]) {
                    // TVM
                    player.tvm.connectionURI = u.userData.userMQurl;
                    player.tvm.exchangeName = u.userData.userMQexchangeName;
                    player.tvm.gameObject.SetActive(false);
                    // PC
                    //player.pc.SetActive(true);
                    if (my_ID == player.id) {
                        p[player.id - 1] = player.gameObject.AddComponent<EntityPipeline>().Init(Config.Instance.Users[0], player.transform, u.sfuData.url_gen);
                    }
                    else {
                        p[player.id - 1] = player.gameObject.AddComponent<EntityPipeline>().Init(Config.Instance.Users[3], player.transform, u.sfuData.url_gen);
                    // AUDIO
                        //player.audioReceiver.SetActive(true);
                        //player.audioSender.SetActive(false);
                        //if (socket) // Audio Socket Receiver
                        //    player.audioReceiver.GetComponent<TestVoiceSocketIOReceiver>().enabled = true;
                        //else // Audio Dash Receiver
                        //    player.audioReceiver.AddComponent<VoiceDashReceiver>().Init(Config.Instance.Users[3].AudioSUBConfig, player.id.ToString());
                    }
                }
            }
        }
    }

    public override void Update() {
        base.Update();

        if (todoAction != Actions.WAIT) {
            timer += Time.deltaTime;

            if (timer >= delay) {
                switch (todoAction) {
                    case Actions.VIDEO_1_START:
                        videos[0].Play();
                        break;
                    case Actions.VIDEO_1_PAUSE:
                        videos[0].Pause();
                        break;
                    case Actions.VIDEO_2_START:
                        videos[1].Play();
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
            delay = (float)SyncTool.GetDelay(SyncTool.ToDateTime(msg[2]));
            Debug.Log(delay);
        }
        else if (msg[0] == MessageType.PAUSE) {
            if (msg[1] == "1") todoAction = Actions.VIDEO_1_PAUSE;
            else if (msg[1] == "2") todoAction = Actions.VIDEO_2_PAUSE;
            delay = (float)SyncTool.GetDelay(SyncTool.ToDateTime(msg[2]));
            Debug.Log(delay);
        }
    }

    public void DebugIntro() {
        PlayerManager player = players[0];

        if (orchestrator.activeSession.sessionUsers[0] == orchestrator.userID) {
            player.cam.gameObject.SetActive(true);
            my_ID = player.id; // Save my ID.
            //if (socket) { // Audio Socket Sender
            //    audioController.userID = my_ID;
            //    audioController.gameObject.SetActive(true);
            //}
            //else { // Audio Dash Sender
            //    player.audioSender.AddComponent<VoiceDashSender>().Init(Config.Instance.Users[0].PCSelfConfig.AudioBin2Dash, player.id.ToString());
            //}
        }

        foreach (User u in orchestrator.availableUsers) {
            if (u.userId == orchestrator.activeSession.sessionUsers[0]) {
                // TVM
                player.tvm.connectionURI = u.userData.userMQurl;
                player.tvm.exchangeName = u.userData.userMQexchangeName;
                player.tvm.gameObject.SetActive(false);
                // PC
                player.pc.SetActive(true);
                if (my_ID == player.id) {
                    p[player.id - 1] = player.gameObject.AddComponent<EntityPipeline>().Init(Config.Instance.Users[0], player.transform, u.sfuData.url_gen);
                    Debug.Log(u.sfuData.url_gen);
                }
                else {
                    p[player.id - 1] = player.gameObject.AddComponent<EntityPipeline>().Init(Config.Instance.Users[3], player.transform, u.sfuData.url_gen);
                    // AUDIO
                    //player.audioReceiver.SetActive(true);
                    //player.audioSender.SetActive(false);
                    //if (socket) // Audio Socket Receiver
                    //    player.audioReceiver.GetComponent<TestVoiceSocketIOReceiver>().enabled = true;
                    //else // Audio Dash Receiver
                    //    player.audioReceiver.AddComponent<VoiceDashReceiver>().Init(Config.Instance.Users[3].AudioSUBConfig, player.id.ToString());
                }
            }
        }


    }
}
