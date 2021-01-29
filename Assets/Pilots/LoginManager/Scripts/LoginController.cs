using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using VRTCore;
using VRT.Orchestrator.Wrapping;
using VRT.LivePresenter;
using VRT.Pilots.Common;

public class LoginController : PilotController {

    private static LoginController instance;

    public static LoginController Instance { get { return instance; } }

    //AsyncOperation async;
    Coroutine loadCoroutine = null;

    public static string GetSceneNameForPilotName(string pilotName, string pilotVariant)
    {
        // Note: Pilot scenes need to be registered here, but also added to the "scenes in build"
        // through Unity Editor File->Build Settings dialog.
        switch (pilotName)
        {
            case "Pilot 0": 
                return "Pilot0";
            case "Pilot 1":
                return "Pilot1";
            case "Pilot 2":
                switch (pilotVariant)
                {
                    case "0": // NONE
                        Config.Instance.presenter = Config.Presenter.None;
                        break;
                    case "1": // LOCAL
                        Config.Instance.presenter = Config.Presenter.Local;
                        break;
                    case "2": // LIVE
                        Config.Instance.presenter = Config.Presenter.Live;
                        break;
                    default:
                        break;
                }
                if (OrchestratorController.Instance.UserIsMaster && Config.Instance.presenter == Config.Presenter.Live)
                {
                    return "Pilot2_Presenter";
                }
                return "Pilot2_Player";
            case "Pilot 3":
                return "Pilot3";
            case "Museum":
                return "Museum";
            case "HoloConference":
                return "HoloMeet";
            case "MedicalExamination":
                return "MedicalExamination";
            default:
                Debug.LogError($"Selected scenario \"{pilotName}\" not implemented in this player");
                return null;
        }
    }

    void Awake() {
        if (!XRDevice.isPresent) {
            Resolution[] resolutions = Screen.resolutions;
            bool fullRes = false;
            foreach (var res in resolutions) {
                if (res.width == 1920 && res.height == 1080) fullRes = true;
            }
            if (fullRes) Screen.SetResolution(1920, 1080, false, 30);
            else Screen.SetResolution(1280, 720, false, 30);
            Debug.Log("Resolution: " + Screen.width + "x" + Screen.height);
        }
    }

    public override void Start() {
        base.Start();
        if (instance == null) {
            instance = this;
        }
    }

    IEnumerator RefreshAndLoad(string scenary) {
        yield return null;
        OrchestratorController.Instance.GetUsers();
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(scenary);
    }

    public override void MessageActivation(string message) {
        Debug.Log($"[FPA] MessageActivation {message}");
        string[] msg = message.Split(new char[] { '_' });
        if (msg[0] == MessageType.START) {
            // Check Audio
            switch (msg[2]) {
                case "0": // No Audio
                    Config.Instance.protocolType = Config.ProtocolType.None;
                    break;
                case "1": // Socket Audio
                    Config.Instance.protocolType = Config.ProtocolType.SocketIO;
                    break;
                case "2": // Dash Audio
                    Config.Instance.protocolType = Config.ProtocolType.Dash;
                    break;
                default:
                    break;
            }
            string pilotName = msg[1];
            string pilotVariant = null;
            if (msg.Length > 3) pilotVariant = msg[3];
            string sceneName = GetSceneNameForPilotName(pilotName, pilotVariant);
            if (loadCoroutine == null) loadCoroutine = StartCoroutine(RefreshAndLoad(sceneName));
        }
        else if (msg[0] == MessageType.READY) {
            // Do something to check if all the users are ready (future implementation)
        }
    }
}
