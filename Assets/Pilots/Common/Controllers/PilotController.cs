using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Video;
using VRT.Core;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Base class for controlling a scene, and switch to another scene.
    /// NOTE: when overriding ensure that base.Awake() and base.Start() are called.
    ///
    /// If there is a CameraFader it will be attached somewhere to the camera, and
    /// it will ensure its static Instance attribute is set.
    /// </summary>
    abstract public class PilotController : MonoBehaviour
    {

        [Tooltip("Fade in at start of scene (default only fadeout)")]
        [SerializeField] public bool startFadedOut = false;

        [Tooltip("Text to show during fadeout (default: nothing)")]
        [SerializeField] protected string fadeOutText;

        public static PilotController Instance = null;

        public string Name()
        {
            return $"{GetType().Name}";
        }

        public void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("PilotController: multiple PilotController (subclass) instances in scene");
            }
            Instance = this;
        }

        public static void LoadScene(string newScene)
        {
            if (Instance != null)
            {
                Instance.LoadNewScene(newScene);
            }
            else
            {
                SceneManager.LoadScene(newScene);
            }
        }

        // Start is called before the first frame update
        public virtual void Start()
        {
            if(CameraFader.Instance != null)
            {
                StartCoroutine(CameraFader.Instance.FadeIn());
            }
        }

        public void LoadNewScene(string newScene)
        {
            if (CameraFader.Instance != null)
            {
                if (fadeOutText != null && fadeOutText != "") CameraFader.Instance.SetText(fadeOutText);
                StartCoroutine(CameraFader.Instance.FadeOut());
                StartCoroutine(LoadSceneAfterFade(CameraFader.Instance.FadeDuration, newScene));
            }
            else
            {
                SceneManager.LoadScene(newScene);
            }
        }

        protected IEnumerator LoadSceneAfterFade(float fadeDuration, string newScene)
        {

            yield return new WaitForSeconds(fadeDuration + 0.2f);
            SceneManager.LoadScene(newScene);
        }

        public virtual void OnUserMessageReceived(string message)
        {
            Debug.LogWarning($"{Name()}: OnUserMessageReceived: unexpected message: {message}");
        }
    }
}