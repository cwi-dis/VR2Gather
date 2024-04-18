using VRT.Orchestrator.Wrapping;
using UnityEngine;
using VRT.Core;
using System.Collections.Generic;

namespace VRT.Login
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

            public Scenario AsScenario()
            {
                Scenario scOrch = new Scenario();
                scOrch.scenarioId = this.scenarioId;
                scOrch.scenarioName = this.scenarioName;
                scOrch.scenarioDescription = this.scenarioDescription;
                return scOrch;
            }
         }

        [Tooltip("Scenarios supported by this VR2Gather player. Order them for the creation popup.")]
        [SerializeField] protected List<ScenarioInfo> scenarios = new List<ScenarioInfo>();

        public List<ScenarioInfo> Scenarios {
            get
            {
                return scenarios;
            }
        }
      
        public static ScenarioRegistry Instance;

        private void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            
        }

        public ScenarioInfo GetScenarioByName(string name)
        {
            foreach (ScenarioInfo sc in scenarios)
            {
                if (name == sc.scenarioName)
                {
                    return sc;
                }
            }
            return null;
        }
        public ScenarioInfo GetScenarioById(string id)
        {
            foreach (ScenarioInfo sc in scenarios)
            {
                if (id == sc.scenarioId)
                {
                    return sc;
                }
            }
            return null;
        }

        public string GetSceneNameForSession(SessionConfig sessionConfig)
        {
            foreach(ScenarioInfo sc in scenarios)
            {
                if (sessionConfig.scenarioName == sc.scenarioName)
                {
                    return sc.scenarioSceneName;
                }
            }
            return null;
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