using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Pilots.Common;

public class VqegSceneChangeController : MonoBehaviour
{
    [Tooltip("Scene to go to ")]
    public string nextSceneName = "Vqeg";

    public void GoToVQEGScene()
    {
        PilotController.LoadScene(nextSceneName);
    }
}