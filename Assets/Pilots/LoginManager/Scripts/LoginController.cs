using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginController : PilotController {

    //AsyncOperation async;
    bool load = false;

    public override void Start() {
        base.Start();
        orchestrator.controller = this;
    }

    public override void Update() {
        base.Update();
    }

    public override void MessageActivation(string message) {
        Debug.Log(message);
        string[] msg = message.Split(new char[] { '_' });
        if (msg[0] == MessageType.START) {
            if (msg[1] == "Pilot 1") SceneManager.LoadScene("Pilot1");
            if (msg[1] == "Pilot 2") {
                // Check Presenter Toggle
                if (msg[2] == "True") orchestrator.presenterToggle.isOn = true;
                else orchestrator.presenterToggle.isOn = false;
                // Check Live Toggle
                if (msg[3] == "True") orchestrator.liveToggle.isOn = true;
                else orchestrator.liveToggle.isOn = false;

                if (orchestrator.isMaster && !orchestrator.isDebug) SceneManager.LoadScene("Pilot2_Presenter");
                else SceneManager.LoadScene("Pilot2_Player");
            }
        }
        else if (msg[0] == MessageType.READY) {
            // Do something to check if all the users are ready (future implementation)
        }
    }
}
