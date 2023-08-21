using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.Pilots.Common;

public class SceneTransition : MonoBehaviour
{
    [Tooltip("Scene to go to ")]
    public string nextSceneName = "ExerciseScene";

    public void GoToNextScene()
    {
        PilotController.LoadScene(nextSceneName);
    }
}
