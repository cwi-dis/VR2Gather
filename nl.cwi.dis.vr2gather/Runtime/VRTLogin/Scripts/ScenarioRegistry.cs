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

    }
}