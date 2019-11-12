using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OrchestratorController : PilotController {

    //AsyncOperation async;
    bool load = false;

    public override void Start() {
        base.Start();
        orchestrator.controller = this;
    }

    public override void Update() {
        base.Update();
    }

    public override void MessageActivation(string msg) {
        if (msg == MessageType.START) {
            if (orchestrator.activeScenario.scenarioName == "Pilot 1") SceneManager.LoadScene("Pilot1");
            if (orchestrator.activeScenario.scenarioName == "Pilot 2") {
                if (orchestrator.isMaster && !orchestrator.isDebug) SceneManager.LoadScene("Pilot2_Presenter");
                else SceneManager.LoadScene("TVSet_Test_Distancia");
            }
        }
        else if (msg == MessageType.READY) {
            // Do something to check if all the users are ready (future implementation)
        }
    }
}
