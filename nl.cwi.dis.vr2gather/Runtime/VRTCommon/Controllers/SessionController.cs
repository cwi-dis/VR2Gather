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
        private bool orchestratorInitialized = false;
        
        public string Name()
        {
            return $"{GetType().Name}";
        }

        // Start is called before the first frame update
        void Start()
        {
            InitialiseOrchestratorEvents();
        }

        private void OnDestroy()
        {
            ClearOrchestratorEvents();
        }

        // Subscribe to Orchestrator Wrapper Events
        private void InitialiseOrchestratorEvents()
        {
            if (orchestratorInitialized)
            {
                Debug.LogError($"{Name()}: Orchestrator already initialized");
                return;
            }
            VRTOrchestrator.Comm.OnLeaveSessionEvent += OnLeaveSessionHandler;
            VRTOrchestrator.Comm.OnUserJoinSessionEvent += OnUserJoinedSessionHandler;
            VRTOrchestrator.Comm.OnUserLeaveSessionEvent += OnUserLeftSessionHandler;
            VRTOrchestrator.Comm.OnErrorEvent += OnErrorHandler;
            VRTOrchestrator.Comm.OnConnectionEvent += OnConnectionEventHandler;

            VRTOrchestrator.Comm.RegisterMessageForwarder();
            orchestratorInitialized = true;
        }

        // Un-Subscribe to Orchestrator Wrapper Events
        private void ClearOrchestratorEvents()
        {
            if (!orchestratorInitialized)
            {
                return;
            }
            orchestratorInitialized = false;
            VRTOrchestrator.Comm.OnLeaveSessionEvent -= OnLeaveSessionHandler;
            VRTOrchestrator.Comm.OnUserJoinSessionEvent -= OnUserJoinedSessionHandler;
            VRTOrchestrator.Comm.OnUserLeaveSessionEvent -= OnUserLeftSessionHandler;
            VRTOrchestrator.Comm.OnErrorEvent -= OnErrorHandler;
            VRTOrchestrator.Comm.OnConnectionEvent -= OnConnectionEventHandler;
            VRTOrchestrator.Comm.UnregisterMessageForwarder();
        }

        public void LeaveSession()
        {
            VRTOrchestrator.Comm.LeaveSession();
        }

        private void OnLeaveSessionHandler()
        {
#if xxxjack_not
            // Code disabled: the session controller should handle deleting the session for the master user.
            if (VRTOrchestrator.Comm.UserIsMaster)
            {
                Debug.Log($"{Name()}: left session as master, deleting.");
                VRTOrchestrator.Comm.DeleteSession(VRTOrchestrator.Comm.MySession.sessionId);
            }
#endif
            ClearOrchestratorEvents();
            Debug.Log($"{Name()}: left session, loading LoginManager scene");
            PilotController.Instance.LoadNewScene();
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
                PilotController.Instance.StopApplication();

            }
        }

  
        private void OnErrorHandler(ResponseStatus status)
        {
            Debug.LogError($"{Name()}: Orchestrator error {status.Error}, {status.Message}");
            // ErrorManager.Instance.EnqueueOrchestratorError(status.Error, status.Message);
        }
    }
}