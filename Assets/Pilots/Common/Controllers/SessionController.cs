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
            OrchestratorController.Instance.OnGetSessionInfoEvent += OnGetSessionInfoHandler;
            OrchestratorController.Instance.OnLeaveSessionEvent += OnLeaveSessionHandler;
            OrchestratorController.Instance.OnUserJoinSessionEvent += OnUserJoinedSessionHandler;
            OrchestratorController.Instance.OnUserLeaveSessionEvent += OnUserLeftSessionHandler;
            OrchestratorController.Instance.OnErrorEvent += OnErrorHandler;

            OrchestratorController.Instance.RegisterMessageForwarder();
        }

        // Un-Subscribe to Orchestrator Wrapper Events
        private void TerminateControllerEvents()
        {
            OrchestratorController.Instance.OnGetSessionInfoEvent -= OnGetSessionInfoHandler;
            OrchestratorController.Instance.OnLeaveSessionEvent -= OnLeaveSessionHandler;
            OrchestratorController.Instance.OnUserJoinSessionEvent -= OnUserJoinedSessionHandler;
            OrchestratorController.Instance.OnUserLeaveSessionEvent -= OnUserLeftSessionHandler;
            OrchestratorController.Instance.OnErrorEvent -= OnErrorHandler;

            OrchestratorController.Instance.UnregisterMessageForwarder();
        }

   
        private void OnGetSessionInfoHandler(Session session)
        {
            if (session != null)
            {
            }
            else
            {
            }
        }

        private void LeaveSession()
        {
            OrchestratorController.Instance.LeaveSession();
        }

        private void OnLeaveSessionHandler()
        {
            Debug.Log("OrchestratorPilot0: left session, loading LoginManager scene");
            PilotController.LoadScene("LoginManager");
        }

        private void OnUserJoinedSessionHandler(string userID)
        {
            if (!string.IsNullOrEmpty(userID))
            {
                Debug.Log($"OrchestratorPilot0: user joined session: {userID}");
            }
        }

        private void OnUserLeftSessionHandler(string userID)
        {
            if (!string.IsNullOrEmpty(userID))
            {
                Debug.Log($"OrchestratorPilot0: user left session: {userID}");
            }
        }

  
        private void OnErrorHandler(ResponseStatus status)
        {
            Debug.Log($"OrchestratorPilot0: OnErrorHandler: {status.Error}, Error message: {status.Message}");
            ErrorManager.Instance.EnqueueOrchestratorError(status.Error, status.Message);
        }
    }
}