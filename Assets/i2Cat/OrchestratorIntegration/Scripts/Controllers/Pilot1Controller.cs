using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrchestratorWrapping;
using UnityEngine.Video;

public class Pilot1Controller : PilotController {

    bool useTVM = true;
    bool enter = false;

    [SerializeField] PoliceController policeController;
    
    // Start is called before the first frame update
    public override void Start() {
        base.Start();
        orchestrator.controller = this;
        background.SetActive(false);

        for (int i = 0; i < orchestrator.activeSession.sessionUsers.Length; i++) {
            PlayerManager player;
            player = players[i];

            if (orchestrator.activeSession.sessionUsers[i] == orchestrator.userID) {
                my_ID = player.id; // Save my ID.
                policeController.my_id = i;
            }

            foreach (User u in orchestrator.availableUsers) {
                if (u.userId == orchestrator.activeSession.sessionUsers[i]) {
                    // TVM
                    player.tvm.connectionURI = u.userData.userMQurl;
                    player.tvm.exchangeName = u.userData.userMQexchangeName;
                    player.tvm.gameObject.SetActive(useTVM);
                }
            }
        }
    }

    // Update is called once per frame
    public override void Update() {
        base.Update();

        //Audio Over Socket.io
        if (!enter) {
            if (timer >= IntroSceneController.Instance.introDuration && AudioManager.instance != null && orchestrator.useSocketIOAudio) {
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
    }

    public override void MessageActivation(string message) { }
}
