using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrchestratorWrapping;
using UnityEngine.Video;

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
        string text = "PLAY_VIDEO_";
        orchestrator.TestSendMessage(text + id.ToString());
    }

    public void SendPauseVideo(int id) {
        string text = "PAUSE_VIDEO_";
        orchestrator.TestSendMessage(text + id.ToString());
    }

    public override void MessageActivation(string message) {
        if (message == "PLAY_VIDEO_1") {
            videos[0].Play();
        }
        else if (message == "PAUSE_VIDEO_1") {
            videos[0].Pause();
        }
        else if (message == "PLAY_VIDEO_2") {
            videos[1].Play();
        }
        else if (message == "PAUSE_VIDEO_2") {
            videos[1].Pause();
        }
    }
}
