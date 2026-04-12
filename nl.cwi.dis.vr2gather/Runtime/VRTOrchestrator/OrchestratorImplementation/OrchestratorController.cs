using System;
using UnityEngine;
using VRT.Orchestrator;

#if UNITY_EDITOR
using UnityEditor.Search;
#endif

namespace VRT.Orchestrator.Implementation
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
        /// <summary>
        /// Obsolete: use VRTOrchestratorSingleton.Login, .Comm, or .Streams instead.
        /// </summary>
        [Obsolete("Use VRTOrchestratorSingleton.Login, .Comm, or .Streams instead of the full interface.")]
        public static IVRTOrchestrator Instance => VRTOrchestratorSingleton.Comm as IVRTOrchestrator;

        /// <summary>
        /// Obsolete: use VRTOrchestratorSingleton.GetClockTimestamp instead.
        /// </summary>
        [Obsolete("Use VRTOrchestratorSingleton.GetClockTimestamp instead.")]
        public static double GetClockTimestamp(System.DateTime pDate)
        {
            return VRTOrchestratorSingleton.GetClockTimestamp(pDate);
        }

        protected virtual void Awake()
        {
            if (VRTOrchestratorSingleton.Comm == null)
            {
                DontDestroyOnLoad(gameObject);
                VRTOrchestratorSingleton.Register(this);
            }
            else if (!object.ReferenceEquals(VRTOrchestratorSingleton.Comm, this))
            {
#if UNITY_EDITOR
                string newName = SearchUtils.GetHierarchyPath(gameObject, false);
                string oldName = SearchUtils.GetHierarchyPath((VRTOrchestratorSingleton.Comm as MonoBehaviour)?.gameObject, false);
#else
                string newName = gameObject.name;
                string oldName = (VRTOrchestratorSingleton.Comm as MonoBehaviour)?.gameObject.name ?? "unknown";
#endif
                Debug.LogError($"OrchestratorController: attempt to create second instance from {newName}. Keep first one, from {oldName}.");
            }
        }

        protected virtual void OnDestroy()
        {
            VRTOrchestratorSingleton.Unregister(this);
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
        public abstract void Login(string name);
        public abstract void Logout();
        public abstract void GetNTPTime();
        public abstract void GetSessions();
        public abstract void AddSession(string scenarioId, Scenario scenario, string name, string description, string protocol);
        public abstract void JoinSession(string sessionId);
        public abstract void DeleteSession(string sessionId);
        public abstract void GetSessionInfo();
        public abstract void UpdateFullUserData(UserData userData);
        public abstract void SendMessageToAll(string message);

        // ── IVRTOrchestratorComm ────────────────────────────────────────────────
        public virtual bool TraceCalls => false;
        public virtual bool WarnOnUnhandledEvents => false;
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
