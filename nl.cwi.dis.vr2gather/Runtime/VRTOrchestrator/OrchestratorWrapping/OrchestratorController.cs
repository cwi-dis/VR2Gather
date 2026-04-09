using System;
using UnityEngine;
using VRT.Orchestrator.Elements;
using VRT.Orchestrator.Responses;

#if UNITY_EDITOR
using UnityEditor.Search;
#endif

namespace VRT.Orchestrator.Wrapping
{
    /// <summary>
    /// Abstract base class for orchestrator controllers. Holds the singleton Instance
    /// and declares the full IVRTOrchestrator contract as abstract members.
    ///
    /// Concrete implementations:
    /// - NetworkOrchestratorController: connects to the real orchestrator server over SocketIO
    /// - StandaloneOrchestratorController: single-user local session, no network required
    /// </summary>
    public abstract class OrchestratorController : MonoBehaviour, IVRTOrchestrator
    {
        private static OrchestratorController instance;

        public static IVRTOrchestrator Instance => instance;

        public static double GetClockTimestamp(System.DateTime pDate)
        {
            return pDate.Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds;
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                DontDestroyOnLoad(gameObject);
                instance = this;
            }
            else if (instance != this)
            {
#if UNITY_EDITOR
                string newName = SearchUtils.GetHierarchyPath(gameObject, false);
                string oldName = SearchUtils.GetHierarchyPath(instance.gameObject, false);
#else
                string newName = gameObject.name;
                string oldName = instance.gameObject.name;
#endif
                Debug.LogError($"OrchestratorController: attempt to create second instance from {newName}. Keep first one, from {oldName}.");
            }
        }

        protected virtual void OnDestroy()
        {
            instance = null;
        }

        // ── IVRTOrchestratorSessionState ────────────────────────────────────────
        public abstract event Action<ResponseStatus> OnErrorEvent;
        public abstract event Action<bool> OnConnectionEvent;
        public abstract event Action OnLeaveSessionEvent;
        public abstract event Action<string> OnUserJoinSessionEvent;
        public abstract event Action<string> OnUserLeaveSessionEvent;
        public abstract User SelfUser { get; set; }
        public abstract bool UserIsMaster { get; }
        public abstract Session CurrentSession { get; }
        public abstract bool ConnectedToOrchestrator { get; }
        public abstract void LeaveSession();

        // ── IVRTOrchestratorLogin ───────────────────────────────────────────────
        public abstract event Action OnConnectingEvent;
        public abstract event Action<string> OnGetOrchestratorVersionEvent;
        public abstract event Action<bool> OnLoginEvent;
        public abstract event Action<bool> OnLogoutEvent;
        public abstract event Action<NtpClock> OnGetNTPTimeEvent;
        public abstract event Action<Session[]> OnSessionsEvent;
        public abstract event Action<Session> OnSessionInfoEvent;
        public abstract event Action<Session> OnAddSessionEvent;
        public abstract event Action<Session> OnJoinSessionEvent;
        public abstract event Action OnSessionJoinedEvent;
        public abstract event Action OnDeleteSessionEvent;
        public abstract event Action<UserMessage> OnUserMessageReceivedEvent;
        public abstract Scenario CurrentScenario { get; }
        public abstract bool UserIsLogged { get; }
        public abstract Session[] AvailableSessions { get; }
        public abstract void SocketConnect(string url);
        public abstract void Abort();
        public abstract void GetVersion();
        public abstract void Shutdown();
        public abstract void Login(string name, string password);
        public abstract void Logout();
        public abstract void GetNTPTime();
        public abstract void GetSessions();
        public abstract void AddSession(string scenarioId, Scenario scenario, string name, string description, string protocol);
        public abstract void JoinSession(string sessionId);
        public abstract void DeleteSession(string sessionId);
        public abstract void GetSessionInfo();
        public abstract void UpdateFullUserData(UserData userData);
        public abstract void SendMessageToAll(string message);
        public abstract void LocalUserSessionForDevelopmentTests();

        // ── IVRTOrchestratorComm ────────────────────────────────────────────────
        public abstract event Action<UserEvent> OnMasterEventReceivedEvent;
        public abstract event Action<UserEvent> OnUserEventReceivedEvent;
        public abstract void SendMessage(string message, string userId);
        public abstract void SendEventToMaster(string eventData);
        public abstract void SendEventToAll(string eventData);
        public abstract void SendEventToUser(string userId, string eventData);

        // ── IVRTOrchestratorDataStream ──────────────────────────────────────────
        public abstract event Action<UserDataStreamPacket> OnDataStreamReceived;
        public abstract void DeclareDataStream(string streamType);
        public abstract void RemoveDataStream(string streamType);
        public abstract void RegisterForDataStream(string userId, string streamType);
        public abstract void UnregisterFromDataStream(string userId, string streamType);
        public abstract void SendData(string streamType, byte[] data);
    }
}
