using UnityEngine;
using VRT.Orchestrator;
using VRT.OrchestratorComm;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Handle session events coming from the orchestrator.
    /// In reality this script doesn't do much, at the moment: really only stopping
    /// if the orchestrator drops the connection, or if the session has been closed
    /// by the remote end.
    /// </summary>
    public class SessionController : MonoBehaviour
    {
        private bool orchestratorInitialized = false;
        
        public string Name()
        {
            return $"{GetType().Name}";
        }

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
            VRTOrchestratorSingleton.Comm.OnLeaveSessionEvent += OnLeaveSessionHandler;
            VRTOrchestratorSingleton.Comm.OnUserJoinSessionEvent += OnUserJoinedSessionHandler;
            VRTOrchestratorSingleton.Comm.OnUserLeaveSessionEvent += OnUserLeftSessionHandler;
            VRTOrchestratorSingleton.Comm.OnErrorEvent += OnErrorHandler;
            VRTOrchestratorSingleton.Comm.OnConnectionEvent += OnConnectionEventHandler;

            VRTOrchestratorSingleton.Comm.RegisterMessageForwarder();
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
            if (VRTOrchestratorSingleton.Comm == null)
            {
                Debug.LogWarning($"{Name()}: ClearOrchestratorEvents: Orchestrator apparently already cleaned up.");
                return;
            }
            VRTOrchestratorSingleton.Comm.OnLeaveSessionEvent -= OnLeaveSessionHandler;
            VRTOrchestratorSingleton.Comm.OnUserJoinSessionEvent -= OnUserJoinedSessionHandler;
            VRTOrchestratorSingleton.Comm.OnUserLeaveSessionEvent -= OnUserLeftSessionHandler;
            VRTOrchestratorSingleton.Comm.OnErrorEvent -= OnErrorHandler;
            VRTOrchestratorSingleton.Comm.OnConnectionEvent -= OnConnectionEventHandler;
            VRTOrchestratorSingleton.Comm.UnregisterMessageForwarder();
        }

        public void LeaveSession()
        {
            VRTOrchestratorSingleton.Comm.LeaveSession();
        }

        private void OnLeaveSessionHandler()
        {
#if xxxjack_not
            // Code disabled: the session controller should handle deleting the session for the master user.
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
            {
                Debug.Log($"{Name()}: left session as master, deleting.");
                VRTOrchestratorSingleton.Comm.DeleteSession(VRTOrchestratorSingleton.Comm.MySession.sessionId);
            }
#endif
            ClearOrchestratorEvents();
            Debug.Log($"{Name()}: left session, loading LoginManager scene");
            PilotController.Instance.TerminateScene(sessionAlreadyLeft: true);
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