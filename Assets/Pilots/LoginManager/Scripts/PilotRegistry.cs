using VRT.Orchestrator.Wrapping;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.LoginManager
{
    public class PilotRegistry : MonoBehaviour
    {
        public static PilotRegistry Instance;

        private void Awake()
        {
            Instance = this;
        }

        public string GetSceneNameForPilotName(string pilotName, string pilotVariant)
        {
            // Note: Pilot scenes need to be registered here, but also added to the "scenes in build"
            // through Unity Editor File->Build Settings dialog.
            //
            // And new pilot names must be added to the scenarios.json of the orchestrator.
            switch (pilotName)
            {
                case "Pilot 0":
                    return "Pilot0";
                case "Technical Playground":
                    return "TechnicalPlayground";
                case "Development":
                    return "Vqeg";
                case "Development2":
                    return null;
                case "Development3":
                    return null;
                case "Mediascape":
                    return null;
                case "MedicalExamination":
                    return null;

                default:
                    return null;
            }
        }
    }
}