using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrchestratorWrapping;

public class Pilot2Controller : MonoBehaviour {
    public static int my_ID = 0;
    [SerializeField]
    private PlayerManager[] players;

    private OrchestrationTest test;
    private OrchestratorGui orchestrator;

    private GameObject mainPanel;
    private GameObject background;

    #region Utils
    private Color playerCol = new Color(0.15f, 0.78f, 0.15f); // Green
    private Color otherCol = new Color(1.0f, 1.0f, 1.0f); // White
    private Color offlineCol = new Color(1.0f, 0.5f, 0.5f); // Red
    #endregion

    // Start is called before the first frame update
    void Start() {
       

        mainPanel = GameObject.Find("MainPanel");
        background = GameObject.Find("Background");
        test = GameObject.Find("ManagerTest").GetComponent<OrchestrationTest>();
        orchestrator = GameObject.Find("MainWindow").GetComponent<OrchestratorGui>();
        mainPanel.SetActive(false);
        background.SetActive(false);

        // Check if is master/presenter
        if (test.isMaster && !test.isDebug)  {
            // Put the players on the correct seat
            for (int i = 1; i < orchestrator.activeSession.sessionUsers.Length; i++) {
                foreach (User u in orchestrator.availableUsers) {
                    if (u.userId == orchestrator.activeSession.sessionUsers[i]) {
<<<<<<< HEAD
                        players[i - 1].cam.SetActive(true);
                        players[i - 1].tvm.GetComponent<ShowTVMs>().connectionURI = u.userData.userMQurl;
                        players[i - 1].tvm.GetComponent<ShowTVMs>().exchangeName = u.userData.userMQexchangeName;
                        players[i - 1].tvm.SetActive(true);
                        //players[i - 1].pc.GetComponent<PointCloudsMainController>().subURL = u.userData.userPCDash;
                        //players[i - 1].pc.SetActive(false);
=======
                        PlayerManager player = players[i];
                        player.cam.SetActive(true);
                        player.tvm.GetComponent<ShowTVMs>().connectionURI = u.userData.userMQurl;
                        player.tvm.GetComponent<ShowTVMs>().exchangeName = u.userData.userMQexchangeName;
                        player.tvm.SetActive(true);
>>>>>>> master
                    }
                }
            }
        }
        else {
            // Put the players on the correct seat
<<<<<<< HEAD
            for (int i = 1; i < orchestrator.activeSession.sessionUsers.Length; i++) {
                //if (orchestrator.activeSession.sessionUsers[i] == orchestrator.TestGetUserID()) players[i - 1].cam.SetActive(true);
                foreach (User u in orchestrator.availableUsers) {
                    if (u.userId == orchestrator.activeSession.sessionUsers[i]) {
                        players[i - 1].tvm.GetComponent<ShowTVMs>().connectionURI = u.userData.userMQurl;
                        players[i - 1].tvm.GetComponent<ShowTVMs>().exchangeName = u.userData.userMQexchangeName;
                        players[i - 1].tvm.SetActive(true);
                        //players[i - 1].pc.GetComponent<PointCloudsMainController>().subURL = u.userData.userPCDash;
                        //players[i - 1].pc.SetActive(false);
=======
            for (int i = 0; i < orchestrator.activeSession.sessionUsers.Length; i++) {
                PlayerManager player = players[i];
                if (test.isDebug) player = players[i+1];

                if (orchestrator.activeSession.sessionUsers[i] == orchestrator.TestGetUserID()) {
                    player.cam.SetActive(true);
                    my_ID = player.id; // Save my ID.
                    if (test.useEcho) ActivateVoiceChat(player.chat, player.id);
                }
                else
                    ActivateVoiceChat(player.chat, player.id);

                foreach (User u in orchestrator.availableUsers) {
                    if (u.userId == orchestrator.activeSession.sessionUsers[i]) {
                        player.tvm.GetComponent<ShowTVMs>().connectionURI = u.userData.userMQurl;
                        player.tvm.GetComponent<ShowTVMs>().exchangeName = u.userData.userMQexchangeName;
                        player.tvm.SetActive(true);
>>>>>>> master
                    }
                }
            }

            GetComponent<MicroRecorder>().Init(my_ID, test.useEcho);
        }
    }

    // Update is called once per frame
    void Update() {
        
    }


    //private void OnGUI() {
    //    for (int i = 0; i < orchestrator.activeSession.sessionUsers.Length; i++) {
    //        GUI.Label(new Rect(5, 40 * (i + 1), 1000, 25), "Player " + i + " - URL: " + players[i].tvm.GetComponent<ShowTVMs>().connectionURI + " - Name: " + players[i].tvm.GetComponent<ShowTVMs>().exchangeName);
    //    }
    //}


    void ActivateVoiceChat(VoicePlayer voicePlayer, int id) {
        voicePlayer.gameObject.SetActive(true);
        SocketIOServer.player[id] = voicePlayer;
        voicePlayer.Init();
    }
}
