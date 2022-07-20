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
                return "Pilot0";
            case "Technical Playground":
                return "TechnicalPlayground";
            default:
                throw new  System.Exception($"Selected scenario \"{pilotName}\" not implemented in this player");
        }
    }

}