using UnityEditor.Recorder.Input;
using UnityEngine;
using VRT.Orchestrator.Wrapping;
using VRT.Pilots.Common;
using UnityEngine.XR;
using UnityEditor.Recorder.AOV;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif


public class VqegController : PilotController
{

    [Tooltip("Objects only visible to confederate user")]
    public GameObject[] confederateOnlyObjects;
    [Tooltip("Are we the confederate?")]
    public bool weAreConfederate;
    [Tooltip("Adjust latencies. Set this in the exercise scene. Will not do anything for the confederate.")]
    public bool adjustLatencies = false;
    [Tooltip("Requested point cloud latency for this scene")]
    public int pc_latency_ms;
    [Tooltip("Requested voice latency for this scene")]
    public int voice_latency_ms;

    

    public override void Start()
    {
        base.Start();
        weAreConfederate = !OrchestratorController.Instance.UserIsMaster;
        foreach (var obj in confederateOnlyObjects)
        {
            obj.SetActive(weAreConfederate);
        }
        if (weAreConfederate)
        {
            loadLatencies();
            Statistics.Output("VqegController", $"scene={gameObject.scene.name},confederate=1");
            setLatencies();

        }
        else
        {
            if (adjustLatencies)
            {
                loadLatencies();
            }
            Statistics.Output("VqegController", $"scene={gameObject.scene.name},confederate=0,pc_latency={pc_latency_ms},voice_latency={voice_latency_ms}");
            setLatencies();
        }
    }

    public void loadLatencies()
    {
        // This method should implement loading the correct latencies for this exercise
    }

    public void setLatencies()
    {
        // This method should implement setting the pc/voice pipelines to the right latencies.
    }
}