using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginController : PilotController {

    //AsyncOperation async;
    Coroutine loadCoroutine = null;

    public override void Start() {
        base.Start();
        orchestrator.controller = this;
    }

    public override void Update() {
        base.Update();
    }

    IEnumerator RefreshAndLoad() {
        yield return null;
        orchestrator.GetUsers();
        while (orchestrator.watingForUser) {
            yield return null;
        }
        Debug.Log("Load!!!");
        /*
        if (orchestrator.isMaster && !orchestrator.isDebug) SceneManager.LoadScene("Pilot2_Presenter");
        else SceneManager.LoadScene("Pilot2_Player");
        */
    }

public override void MessageActivation(string message) {
        Debug.Log(message);
        string[] msg = message.Split(new char[] { '_' });
        if (msg[0] == MessageType.START) {
            if (msg[1] == "Pilot 1") SceneManager.LoadScene("Pilot1");
            if (msg[1] == "Pilot 2") {
                // Check Representation
                if (msg[2] == "0") { // TVM
                    orchestrator.useTVM = true;
                    orchestrator.usePC = false;
                }
                else { // PC
                    orchestrator.useTVM = false;
                    orchestrator.usePC = true;
                }
                // Check Audio
                switch (msg[3]) {
                    case "0": // No Audio
                        orchestrator.useAudio = false;
                        orchestrator.useSocketIOAudio = false;
                        orchestrator.useDashAudio = false;
                        break;
                    case "1": // Socket Audio
                        orchestrator.useAudio = true;
                        orchestrator.useSocketIOAudio = true;
                        orchestrator.useDashAudio = false;
                        break;
                    case "2": // Dash Audio
                        orchestrator.useAudio = true;
                        orchestrator.useSocketIOAudio = false;
                        orchestrator.useDashAudio = true;
                        break;
                    default:
                        break;
                }
                // Check Presenter Toggle
                if (msg[4] == "True") orchestrator.presenterToggle.isOn = true;
                else orchestrator.presenterToggle.isOn = false;
                // Check Live Toggle
                if (msg[5] == "True") orchestrator.liveToggle.isOn = true;
                else orchestrator.liveToggle.isOn = false;

                if (loadCoroutine==null)  loadCoroutine = StartCoroutine(RefreshAndLoad());
            }
        }
        else if (msg[0] == MessageType.READY) {
            // Do something to check if all the users are ready (future implementation)
        }
    }
}
