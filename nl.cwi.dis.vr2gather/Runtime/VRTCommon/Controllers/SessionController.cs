using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VRT.Orchestrator.Wrapping;
using VRT.Orchestrator.Responses;
using VRT.Pilots.Common;
using VRT.Core;

namespace VRT.Pilots.Common
{
    public class SessionController : MonoBehaviour
    {
        
        public string Name()
        {
            return $"{GetType().Name}";
        }

        // Start is called before the first frame update
        void Start()
        {
         
            InitialiseControllerEvents();
        }

        private void OnDestroy()
        {
            TerminateControllerEvents();
        }

        // Subscribe to Orchestrator Wrapper Events
        private void InitialiseControllerEvents()
        {
            OrchestratorController.Instance.OnLeaveSessionEvent += OnLeaveSessionHandler;
            OrchestratorController.Instance.OnUserJoinSessionEvent += OnUserJoinedSessionHandler;
            OrchestratorController.Instance.OnUserLeaveSessionEvent += OnUserLeftSessionHandler;
            OrchestratorController.Instance.OnErrorEvent += OnErrorHandler;
            OrchestratorController.Instance.OnConnectionEvent += OnConnectionEventHandler;

            OrchestratorController.Instance.RegisterMessageForwarder();
        }

        // Un-Subscribe to Orchestrator Wrapper Events
        private void TerminateControllerEvents()
        {
            OrchestratorController.Instance.OnLeaveSessionEvent -= OnLeaveSessionHandler;
            OrchestratorController.Instance.OnUserJoinSessionEvent -= OnUserJoinedSessionHandler;
            OrchestratorController.Instance.OnUserLeaveSessionEvent -= OnUserLeftSessionHandler;
            OrchestratorController.Instance.OnErrorEvent -= OnErrorHandler;
            OrchestratorController.Instance.OnConnectionEvent -= OnConnectionEventHandler;
            OrchestratorController.Instance.UnregisterMessageForwarder();
        }

        public void LeaveSession()
        {
            OrchestratorController.Instance.LeaveSession();
        }

        private void OnLeaveSessionHandler()
        {
#if xxxjack_not
            // Code disabled: the session controller should handle deleting the session for the master user.
            if (OrchestratorController.Instance.UserIsMaster)
            {
                Debug.Log($"{Name()}: left session as master, deleting.");
                OrchestratorController.Instance.DeleteSession(OrchestratorController.Instance.MySession.sessionId);
            }
#endif
            Debug.Log($"{Name()}: left session, loading LoginManager scene");
            PilotController.LoadScene("LoginManager");
        }

        private void OnUserJoinedSessionHandler(string userID)
        {            
            Debug.LogWarning($"{Name()}: user joined session: {userID}");
        }

        private void OnUserLeftSessionHandler(string userID)
        {
            Debug.Log($"{Name()}: user left session: {userID}");
        }

        private void OnConnectionEventHandler(bool connected)
        {
            Debug.LogWarning($"{Name()}: Unexpected Connection event, connected={connected}");
            if (!connected)
            {
                // We should handle this more gracefully, by letting the user continue is disconnected mode or something...
                Debug.LogError($"{Name()}: Orchestrator disconnect. Quit application, sorry...");
#if UNITY_EDITOR
                // Application.Quit() does not work in the editor so
                // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif

            }
        }

  
        private void OnErrorHandler(ResponseStatus status)
        {
            Debug.LogError($"{Name()}: Orchestrator error {status.Error}, {status.Message}");
            // ErrorManager.Instance.EnqueueOrchestratorError(status.Error, status.Message);
        }
    }
}