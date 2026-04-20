using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Video;
using VRT.Core;
using System;
using VRT.Orchestrator.Implementation;

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
        [Header("Pilot settings")]
        [Tooltip("Fade in at start of scene (default only fadeout)")]
        [SerializeField] public bool startFadedOut = false;

        [Tooltip("Text to show during fadeout (default: nothing)")]
        [SerializeField] protected string fadeOutText;

        [Tooltip("Allow direct interaction in this scene (default: ray-based and keyboard/mouse")]
        [SerializeField] protected bool allowDirectInteractionInScene;

        [Tooltip("Next scene when session ends. Empty to stop playback.")]
        public string NextSceneOnSessionEnd = "VRTLoginManager";

        [Header("Developer settings")]
        [SerializeField]
        [Tooltip("Allow scene to be playable in the editor")]
        protected bool playableInEditor = true;
        [Tooltip("Prefab for configuration")]
        [SerializeField]
        protected GameObject configurationPrefab;
        [Tooltip("Prefab for orchestrator")]
        [SerializeField]
        protected GameObject orchestratorPrefab;
        /// <summary>
        /// True once the session leave process has started. Components that use
        /// VRTOrchestratorSingleton.Comm should check this before sending messages.
        /// </summary>
        [NonSerialized] public bool IsLeavingSession = false;
        [Header("Introspection")]
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
            if (playableInEditor && !VRTConfig.InstanceExists())
            {
                Debug.LogWarning($"{Name()}: scene started in isolation. Creating config and orchestrator");
                CreatePlayableEditorScene();
            }
        }

        void CreatePlayableEditorScene()
        {
            if (configurationPrefab == null || orchestratorPrefab == null)
            {
                Debug.LogError($"{Name()}: cannot play scene, missing configuration or orchestrator prefab");
            }
            // First instantiate the VRTConfig
            var config = Instantiate(configurationPrefab);
            // Ensure that that worked.
            if (VRTConfig.Instance == null)
            {
                Debug.LogError($"{Name()}: could not create config, VRTConfig.Instance is null");
                return;
            }
            var orch = Instantiate(orchestratorPrefab);
            var orchController  = orch.GetComponent<StandaloneOrchestratorController>();
            orchController.autoCreateSession = true;
        }
        
        /// <summary>
        /// Call when the session is ending (button press, session creator leaving, etc.).
        /// Sets IsLeavingSession and triggers the scene transition.
        /// If sessionAlreadyLeft is false (default), asks the SessionController to leave
        /// the session first; the scene load happens when the leave is confirmed via
        /// OnLeaveSessionHandler. If sessionAlreadyLeft is true (e.g. forced out because
        /// the master left), skips the leave step and loads the new scene immediately.
        /// </summary>
        public void TerminateScene(bool sessionAlreadyLeft = false)
        {
            IsLeavingSession = true;
            if (!sessionAlreadyLeft)
            {
                SessionController ctrl = GetComponent<SessionController>();
                if (ctrl != null)
                {
                    ctrl.LeaveSession();
                    return; // LoadNewScene called from OnLeaveSessionHandler
                }
            }
            LoadNewScene();
        }

        private void OnApplicationQuit()
        {
            Debug.Log($"{Name()}: OnApplicationQuit: Leaving session.");
            TerminateScene();
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

        public void LoadNewScene(string newScene=null)
        {
            if (newScene == null) {
                newScene = NextSceneOnSessionEnd;
            }
            if (string.IsNullOrEmpty(newScene)) {
                Debug.Log($"{Name()}: No next scene, quit application");
                StopApplication();
            }
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
                TerminateScene();
                return true;
            }
            if (command == "exit")
            {
                StopApplication();
                return true;
            }
            return false;
        }

        public virtual void OnUserCommand_ext(string command)
        {
            bool ok = OnUserCommand(command);
            if (!ok)
            {
                Debug.LogWarning($"{Name()}: OnUserCommand_ext: unexpected command: {command}");
            }
        }

        public void StopApplication() {
#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}