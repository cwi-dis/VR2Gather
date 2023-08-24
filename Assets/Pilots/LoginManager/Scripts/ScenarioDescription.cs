using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioDescription : MonoBehaviour
{
    [Tooltip("Unique ID of scenario. Create only once.")]
    public string scenarioId;
    [Tooltip("Short name for scenario")]
    public string scenarioName;
    [Tooltip("Short description of the scenario")]
    public string scenarioDescription;
    [Tooltip("Higher numbered scenarios will appear higher in the list")]
    public int scenarioPriority;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
