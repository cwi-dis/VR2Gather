using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OrchestratorController : PilotController {

    public override void Start() {
        base.Start();
        orchestrator.controller = this;
    }

    public override void MessageActivation(string msg) {
        if (msg == MessageType.START) {
            if (orchestrator.isMaster && !orchestrator.isDebug) SceneManager.LoadScene("Pilot2_Presenter");
            else SceneManager.LoadScene("TVSet_Test_Distancia"); //SceneManager.LoadScene("Pilot2_Player");
        }
        else if (msg == MessageType.READY) {
            // Do something to check if all the users are ready (future implementation)
        }
    }
}
