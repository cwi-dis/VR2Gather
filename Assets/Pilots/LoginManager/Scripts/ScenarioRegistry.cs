using VRT.Orchestrator.Wrapping;
using UnityEngine;
using VRT.Core;
using System.Collections.Generic;
using System;

namespace VRT.Pilots.LoginManager
{
    public class ScenarioRegistry : MonoBehaviour
    {
        [System.Serializable]
        public class ScenarioInfo
        {
            [Tooltip("Short name for scenario")]
            public string scenarioName;
            [Tooltip("Unique ID of scenario. Create only once.")]
            public string scenarioId;
            [Tooltip("Short description of the scenario")]
            public string scenarioDescription;
            [Tooltip("The name of the scene that implements this scenario")]
            public string scenarioSceneName;
            [Tooltip("Higher numbered scenarios will appear higher in the list")]
            public int scenarioPriority;
        }

        [Tooltip("Scenarios supported by this VR2Gather player")]
        public List<ScenarioInfo> scenarios = new List<ScenarioInfo>();

      
        public static ScenarioRegistry Instance;

        private void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            
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
                    return null;
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