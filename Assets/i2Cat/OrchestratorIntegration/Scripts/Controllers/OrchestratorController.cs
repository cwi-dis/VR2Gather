using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OrchestratorController : PilotController {

    AsyncOperation async;
    bool load = false;

    public override void Start() {
        base.Start();
        orchestrator.controller = this;
        async = SceneManager.LoadSceneAsync("TVSet_Test_Distancia");
        async.allowSceneActivation = false;
    }

    public override void Update() {
        base.Update();
        Debug.Log(async.progress);
        if (load && async.progress >= 0.8) async.allowSceneActivation = true;
    }

    public override void MessageActivation(string msg) {
        if (msg == MessageType.START) {
            if (orchestrator.isMaster && !orchestrator.isDebug) SceneManager.LoadScene("Pilot2_Presenter");
            else {
                //SceneManager.LoadScene("TVSet_Test_Distancia"); 
                //SceneManager.LoadScene("Pilot2_Player");
                load = true;
            }
        }
        else if (msg == MessageType.READY) {
            // Do something to check if all the users are ready (future implementation)
        }
    }
}
