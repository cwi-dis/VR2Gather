using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VRT.Orchestrator.Wrapping;
using VRT.Pilots.Common;
using VRT.Core;

namespace VRT.Pilots.Common
{
    public class SessionController : MonoBehaviour
    {
        public static SessionController Instance { get; private set; }

        public string Name()
        {
            return $"{GetType().Name}";
        }

        // Start is called before the first frame update
        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }
       
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

            OrchestratorController.Instance.RegisterMessageForwarder();
        }

        // Un-Subscribe to Orchestrator Wrapper Events
        private void TerminateControllerEvents()
        {
            OrchestratorController.Instance.OnLeaveSessionEvent -= OnLeaveSessionHandler;
            OrchestratorController.Instance.OnUserJoinSessionEvent -= OnUserJoinedSessionHandler;
            OrchestratorController.Instance.OnUserLeaveSessionEvent -= OnUserLeftSessionHandler;
            OrchestratorController.Instance.OnErrorEvent -= OnErrorHandler;

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
            if (!string.IsNullOrEmpty(userID))
            {
                Debug.Log($"{Name()}: user joined session: {userID}");
            }
        }

        private void OnUserLeftSessionHandler(string userID)
        {
            if (!string.IsNullOrEmpty(userID))
            {
                Debug.Log($"{Name()}: user left session: {userID}");
            }
        }

  
        private void OnErrorHandler(ResponseStatus status)
        {
            Debug.Log($"{Name()}: OnErrorHandler: {status.Error}, Error message: {status.Message}");
            ErrorManager.Instance.EnqueueOrchestratorError(status.Error, status.Message);
        }
    }
}