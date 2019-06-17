using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrchestratorWrapping;
using UnityEngine.Video;
using System;

public class Pilot2PresenterController : PilotController {
    
    public override void Start() {
        base.Start();
        mainPanel.SetActive(false);
        background.SetActive(false);

        test.controller = this;

        for (int i = 1; i < orchestrator.activeSession.sessionUsers.Length; i++) {
            foreach (User u in orchestrator.availableUsers) {
                if (u.userId == orchestrator.activeSession.sessionUsers[i]) {
                    PlayerManager player = players[i];
                    player.cam.SetActive(true);
                    player.tvm.GetComponent<ShowTVMs>().connectionURI = u.userData.userMQurl;
                    player.tvm.GetComponent<ShowTVMs>().exchangeName = u.userData.userMQexchangeName;
                    player.tvm.SetActive(true);
                    player.pc.GetComponent<PointCloudsMainController>().subURL = u.userData.userPCDash;
                    player.pc.SetActive(false);
                }
            }
        }
    }

    public void SendPlayVideo(int id) {
        string text = "PLAY_";
        orchestrator.TestSendMessage(text + id.ToString() + "_" + myTime);
    }

    public void SendPauseVideo(int id) {
        string text = "PAUSE_";
        orchestrator.TestSendMessage(text + id.ToString() + "_" + myTime);
    }

    public override void MessageActivation(string message) {
        string[] msg = message.Split(new char[] { '_' });
        if (msg[0] == "PLAY") {
            if (msg[1] == "1") videos[0].Play();
            else if (msg[1] == "2") videos[1].Play();
        }
        else if (msg[0] == "PAUSE") {
            if (msg[1] == "1") videos[0].Pause();
            else if (msg[1] == "2") videos[1].Pause();
        }
    }
}
