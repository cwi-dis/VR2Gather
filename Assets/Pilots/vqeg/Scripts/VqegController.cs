using UnityEditor.Recorder.Input;
using UnityEngine;
using VRT.Orchestrator.Wrapping;
using VRT.Pilots.Common;


public class VqegController : PilotController
{

    [Tooltip("Objects only visible to confederate user")]
    public GameObject[] confederateOnlyObjects;
    [Tooltip("Are we the confederate?")]
    public bool weAreConfederate;

    public virtual void Start()
    {
        base.Start();
        weAreConfederate = !OrchestratorController.Instance.UserIsMaster;
        foreach (var obj in confederateOnlyObjects)
        {
            obj.SetActive(weAreConfederate);
        }
    }
  
}