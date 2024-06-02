using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Video;
using VRT.Core;
using System;

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

        [Tooltip("Allow direct interaction in this scene (default: ray-based and keyboard/mouse")]
        [SerializeField] protected bool allowDirectInteractionInScene;

        [Tooltip("Set for scenes that are not networked")]
        public bool sceneIsSingleUser = false;

        [Tooltip("Direct interaction disabled now because of UI visible (introspection/debug)")]
        [DisableEditing] [SerializeField] protected bool m_directInteractionDisabled;
        /// <summary>
        /// Return true if direct interaction can currently be enabled.
        /// </summary>
        public bool directInteractionAllowed { get => allowDirectInteractionInScene && !m_directInteractionDisabled; }
        public static PilotController Instance = null;

        public string Name()
        {
            return $"{GetType().Name}";
        }

        public virtual void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("PilotController: multiple PilotController (subclass) instances in scene");
            }
            Instance = this;
            Debug.Log($"{Name()}: Awake.");
        }

        private void OnApplicationQuit()
        {
            SessionController ctrl = GetComponent<SessionController>();
            if (ctrl != null)
            {
                Debug.Log($"{Name()}: OnApplicationQuit: Leaving session.");

                ctrl.LeaveSession();
            }
        }

        /// <summary>
        /// Call this method to load a new scene (optionally after fading out the current scene).
        /// </summary>
        /// <param name="newScene"></param>
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

        /// <summary>
        /// Call this method when showing a UI or other object that requires ray interaction.
        /// </summary>
        public void DisableDirectInteraction()
        {
            m_directInteractionDisabled = true;
        }

        /// <summary>
        /// Call this method when a UI is hidden, so ray interaction is no longer required.
        /// </summary>
        public void EnableDirectInteraction()
        {
            m_directInteractionDisabled = false;
        }

        // Start is called before the first frame update
        public virtual void Start()
        {
            Debug.Log($"{Name()}: Started.");
            if(CameraFader.Instance != null)
            {
                CameraFader.Instance.StartFadeIn();
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
                Debug.Log($"{Name()}: Loading new scene {newScene}");
                SceneManager.LoadScene(newScene);
            }
        }

        protected IEnumerator LoadSceneAfterFade(float fadeDuration, string newScene)
        {
            Debug.Log($"{Name()}: Fading out for transition to {newScene}");
            yield return new WaitForSeconds(fadeDuration + 0.2f);
            Debug.Log($"{Name()}: Loading new scene {newScene}");
            SceneManager.LoadScene(newScene);
        }

        /// <summary>
        /// Called by orchestrator stubs when a user-message has been received. Should be
        /// overridden by controllers for scenes that support user messages.
        /// </summary>
        /// <param name="message"></param>
        public virtual void OnUserMessageReceived(string message)
        {
            Debug.LogWarning($"{Name()}: OnUserMessageReceived: unexpected message: {message}");
        }

        /// <summary>
        /// Called by HUD and other UI methods to implement per-pilot commands.
        /// If overridden by subclasses they must call this base method too
        /// </summary>
        /// <param name="command"></param>
        /// <returns>True if the command is implemented.</returns>
        public virtual bool OnUserCommand(string command)
        {
            if (command == "leave")
            {
                SessionController ctrl = GetComponent<SessionController>();
                if (ctrl != null)
                {
                    ctrl.LeaveSession();
                }
                else
                {
                    LoadNewScene("VRTLoginManager");
                }
                return true;
            }
            if (command == "exit")
            {
#if UNITY_EDITOR
                // Application.Quit() does not work in the editor so
                // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                return true;
            }
            return false;
        }
    }
}