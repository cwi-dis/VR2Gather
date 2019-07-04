using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrchestratorWrapping;
using UnityEngine.Video;
using System;

public class Pilot2PresenterController : PilotController {

    public new PlayerPresenterManager[] players;
        
    public override void Start() {
        base.Start();
        mainPanel.SetActive(false);
        background.SetActive(false);
        
        test.controller = this;

        for (int i = 1; i < orchestrator.activeSession.sessionUsers.Length; i++) {
            foreach (User u in orchestrator.availableUsers) {
                if (u.userId == orchestrator.activeSession.sessionUsers[i]) {                    
                    players[i - 1].cam.gameObject.SetActive(true);
                    players[i - 1].tvm.connectionURI = u.userData.userMQurl;
                    players[i - 1].tvm.exchangeName = u.userData.userMQexchangeName;
                    players[i - 1].tvm.gameObject.SetActive(true);
                    players[i - 1].pc.subURL = u.userData.userPCDash;
                    players[i - 1].pc.gameObject.SetActive(false);
                    players[i - 1].offlineText.gameObject.SetActive(false);
                    //players[i - 1].latencyText.gameObject.SetActive(true);

                    //players[i - 1].tvm.gameObject.transform.localPosition = new Vector3(0, 0, 0);
                    //players[i - 1].tvm.gameObject.transform.localRotation = Quaternion.Euler(0, 216, 0);

                    //player.cam.SetActive(true);
                    //player.tvm.GetComponent<ShowTVMs>().connectionURI = u.userData.userMQurl;
                    //player.tvm.GetComponent<ShowTVMs>().exchangeName = u.userData.userMQexchangeName;
                    //player.tvm.SetActive(true);
                    //player.pc.GetComponent<PointCloudsMainController>().subURL = u.userData.userPCDash;
                    //player.pc.SetActive(false);
                }
            }
        }
    }

    public void SendStartLivestream() {
        string text = MessageType.LIVESTREAM + "_";
        orchestrator.TestSendMessage(text + SyncTool.GetMyTimeString());
    }

    public void SendPlayVideo(int id) {
        string text = MessageType.PLAY + "_";
        orchestrator.TestSendMessage(text + id.ToString() + "_" + SyncTool.GetMyTimeString());
        videos[id - 1].Play();
    }

    public void SendPauseVideo(int id) {
        string text = MessageType.PAUSE + "_";
        orchestrator.TestSendMessage(text + id.ToString() + "_" + SyncTool.GetMyTimeString());
        videos[id - 1].Pause();
    }

    public override void MessageActivation(string message) {
        string[] msg = message.Split(new char[] { '_' });
        if (msg[0] == MessageType.PING) {
            int id = int.Parse(msg[1]);
            delay = (float)SyncTool.GetDelayMilis(SyncTool.ToDateTime(msg[2]));
            players[id].latencyText.text = delay.ToString();
            Debug.Log("PING from " + id + " with " + delay + "ms delay.");
        }
    }
}
