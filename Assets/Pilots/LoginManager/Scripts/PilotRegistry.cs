using VRT.Orchestrator.Wrapping;
using UnityEngine;
using VRT.Core;

public class PilotRegistry
{

    public static string GetSceneNameForPilotName(string pilotName, string pilotVariant)
    {
        // Note: Pilot scenes need to be registered here, but also added to the "scenes in build"
        // through Unity Editor File->Build Settings dialog.
        switch (pilotName)
        {
            case "Pilot 0":
                return "Cinema";
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
            case "Technical Playground":
                return "TechnicalPlayground";
            case "Development":
                return "TractionLobby";
            default:
                throw new  System.Exception($"Selected scenario \"{pilotName}\" not implemented in this player");
                return null;
        }
    }

}