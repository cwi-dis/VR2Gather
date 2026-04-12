using System;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
using VRT.Orchestrator;

#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Orchestrator.Implementation
{
    /// <summary>
    /// Standalone orchestrator controller. Does not connect to any server.
    /// Provides a single-user local session for running scenarios without
    /// internet connectivity or an orchestrator server.
    ///
    /// All login/session methods complete synchronously by firing their response
    /// events immediately, driving the OrchestratorLogin state machine without
    /// any network round-trips. Communication methods (SendEvent*, SendData, etc.)
    /// are no-ops since there are no other participants.
    /// </summary>
    public class StandaloneOrchestratorController : OrchestratorController
    {
        // ── Events ──────────────────────────────────────────────────────────────
        public override event Action<ResponseStatus> OnErrorEvent;
        public override event Action<bool> OnConnectionEvent;
        public override event Action OnLeaveSessionEvent;
        public override event Action<string> OnUserJoinSessionEvent;
        public override event Action<string> OnUserLeaveSessionEvent;
        public override event Action OnConnectingEvent;
        public override event Action<string> OnGetOrchestratorVersionEvent;
        public override event Action<bool> OnLoginEvent;
        public override event Action<bool> OnLogoutEvent;
        public override event Action<NtpClock> OnGetNTPTimeEvent;
        public override event Action<Session[]> OnSessionsEvent;
        public override event Action<Session> OnSessionInfoEvent;
        public override event Action<Session> OnAddSessionEvent;
        public override event Action<Session> OnJoinSessionEvent;
        public override event Action OnSessionJoinedEvent;
        public override event Action OnDeleteSessionEvent;
        public override event Action<UserMessage> OnUserMessageReceivedEvent;
        public override event Action<UserEvent> OnMasterEventReceivedEvent;
        public override event Action<UserEvent> OnUserEventReceivedEvent;
        public override event Action<UserDataStreamPacket> OnDataStreamReceived;

        // ── Tracing ──────────────────────────────────────────────────────────────
        [Tooltip("Log all orchestrator calls and events to the console")]
        public bool traceCalls = false;
        [Tooltip("Warn when an event is received but no handler is registered")]
        public bool warnOnUnhandledEvents = true;
        // Set to true in source to also trace high-frequency calls (SendEvent*, SendData)
        private const bool traceHighFrequency = false;

        private void Trace(string direction, string name)
        {
            if (traceCalls) Debug.Log($"StandaloneOrchestratorController: {direction}:{name}");
        }

        // ── State ────────────────────────────────────────────────────────────────
        private User _selfUser;
        private Session _currentSession;
        private Scenario _currentScenario;

        // ── IVRTOrchestratorComm ─────────────────────────────────────────────────
        public override bool TraceCalls => traceCalls;
        public override bool WarnOnUnhandledEvents => warnOnUnhandledEvents;

        // ── IVRTOrchestratorSessionState ─────────────────────────────────────────
        public override User SelfUser { get { return _selfUser; } set { _selfUser = value; } }
        public override bool UserIsMaster => true;
        public override Session CurrentSession => _currentSession;

        // ── IVRTOrchestratorLogin ────────────────────────────────────────────────
        public override Scenario CurrentScenario => _currentScenario;
        public override bool UserIsLogged => _selfUser != null;
        public override Session[] AvailableSessions => new Session[0];

        /// <summary>
        /// "Connect" to the standalone session. Fires OnConnectionEvent(true) immediately,
        /// which drives the OrchestratorLogin state machine into the login step.
        /// </summary>
        public override void SocketConnect(string url)
        {
            Trace("send", nameof(SocketConnect));
#if VRT_WITH_STATS
            Statistics.Output("OrchestratorController", $"orchestrator_url=standalone");
#endif
            Trace("recv", nameof(OnConnectionEvent));
            OnConnectionEvent?.Invoke(true);
        }

        public override void Login(string name)
        {
            Trace("send", nameof(Login));
            var config = VRTConfig.Instance.RepresentationConfig;
            _selfUser = new User
            {
                userId = "standalone-userid",
                userName = name,
                userData = new UserData
                {
                    userRepresentation = config.representation,
                    hasVoice = !string.IsNullOrEmpty(config.microphoneName) && config.microphoneName != "None",
                    userRepresentationTCPUrl = config.userRepresentationTCPUrl,
                }
            };
            Trace("recv", nameof(OnLoginEvent));
            OnLoginEvent?.Invoke(true);
        }

        public override void GetVersion()
        {
            Trace("send", nameof(GetVersion));
#if VRT_WITH_STATS
            Statistics.Output("OrchestratorController", $"connected=1, orchestrator_version=standalone");
#endif
            Trace("recv", nameof(OnGetOrchestratorVersionEvent));
            OnGetOrchestratorVersionEvent?.Invoke("standalone");
        }

        public override void GetNTPTime()
        {
            Trace("send", nameof(GetNTPTime));
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            var ntpTime = new NtpClock
            {
                ntpDate = System.DateTime.UtcNow.ToString("o"),
                ntpTimeMs = (long)sinceEpoch.TotalMilliseconds,
            };
#if VRT_WITH_STATS
            Statistics.Output("OrchestratorController", $"orchestrator_ntptime_ms={ntpTime.ntpTimeMs}, localtime_behind_ms=0, uncertainty_interval_ms=0");
#endif
            Trace("recv", nameof(OnGetNTPTimeEvent));
            OnGetNTPTimeEvent?.Invoke(ntpTime);
        }

        public override void AddSession(string scenarioId, Scenario scenario, string name, string description, string protocol)
        {
            Trace("send", nameof(AddSession));
            _currentScenario = scenario;
            _currentSession = new Session
            {
                sessionId = "standalone-sessionid",
                sessionName = name,
                sessionDescription = description,
                scenarioId = scenarioId,
                sessionAdministrator = _selfUser.userId,
                sessionMaster = _selfUser.userId,
                sessionUsers = new[] { _selfUser.userId },
                sessionUserDefinitions = new List<User> { _selfUser },
            };
#if VRT_WITH_STATS
            Statistics.Output("OrchestratorController", $"created=1, sessionId={_currentSession.sessionId}, sessionName={_currentSession.sessionName}, isMaster=1, nUser=1");
#endif
            Trace("recv", nameof(OnAddSessionEvent));
            OnAddSessionEvent?.Invoke(_currentSession);
        }

        public override void LeaveSession()
        {
            Trace("send", nameof(LeaveSession));
#if VRT_WITH_STATS
            if (_currentSession != null)
                Statistics.Output("OrchestratorController", $"stopping=1, sessionId={_currentSession.sessionId}");
#endif
            _currentSession = null;
            _currentScenario = null;
            Trace("recv", nameof(OnLeaveSessionEvent));
            OnLeaveSessionEvent?.Invoke();
        }

        /// <summary>
        /// In standalone mode there is no server to echo the message back, so fire
        /// OnUserMessageReceivedEvent locally. This is what triggers scene loading
        /// when OrchestratorLogin sends "START_...".
        /// </summary>
        public override void SendMessageToAll(string message)
        {
            Trace("send", nameof(SendMessageToAll));
            if (message.StartsWith("START_"))
            {
#if VRT_WITH_STATS
                Statistics.Output("OrchestratorController", $"starting=1, sessionId={_currentSession?.sessionId}, sessionName={_currentSession?.sessionName}");
#endif
                if (VRTConfig.Instance.AutoStartConfig.autoLeaveAfter > 0)
                {
#if VRT_WITH_STATS
                    Statistics.Output("OrchestratorController", $"autoLeaveAfter={VRTConfig.Instance.AutoStartConfig.autoLeaveAfter}");
#endif
                    Invoke("LeaveSession", VRTConfig.Instance.AutoStartConfig.autoLeaveAfter);
                }
            }
            var userMessage = new UserMessage(
                _selfUser?.userId ?? "",
                _selfUser?.userName ?? "",
                message);
            Trace("recv", nameof(OnUserMessageReceivedEvent));
            OnUserMessageReceivedEvent?.Invoke(userMessage);
        }

        public override void Shutdown()
        {
            Trace("send", nameof(Shutdown));
            Destroy(gameObject);
        }

        public override void UpdateFullUserData(UserData userData)
        {
            Trace("send", nameof(UpdateFullUserData));
            if (_selfUser != null)
                _selfUser.userData = userData;
        }

        public override void Abort() { Trace("send", nameof(Abort)); }
        public override void Logout() { Trace("send", nameof(Logout)); Trace("recv", nameof(OnLogoutEvent)); OnLogoutEvent?.Invoke(true); }
        public override void GetSessions() { Trace("send", nameof(GetSessions)); Trace("recv", nameof(OnSessionsEvent)); OnSessionsEvent?.Invoke(new Session[0]); }
        public override void JoinSession(string sessionId) { Trace("send", nameof(JoinSession)); }
        public override void DeleteSession(string sessionId) { Trace("send", nameof(DeleteSession)); }
        public override void GetSessionInfo() { Trace("send", nameof(GetSessionInfo)); }

        // ── IVRTOrchestratorComm: no-ops (no other participants) ─────────────────
        public override void SendMessage(string message, string userId) { Trace("send", nameof(SendMessage)); }
        public override void SendEventToMaster(string eventData) { if (traceHighFrequency) Trace("send", nameof(SendEventToMaster)); }
        public override void SendEventToAll(string eventData) { if (traceHighFrequency) Trace("send", nameof(SendEventToAll)); }
        public override void SendEventToUser(string userId, string eventData) { if (traceHighFrequency) Trace("send", nameof(SendEventToUser)); }

        // ── IVRTOrchestratorDataStream: no-ops ───────────────────────────────────
        public override void DeclareDataStream(string streamType) { Trace("send", nameof(DeclareDataStream)); }
        public override void RemoveDataStream(string streamType) { Trace("send", nameof(RemoveDataStream)); }
        public override void RegisterForDataStream(string userId, string streamType) { Trace("send", nameof(RegisterForDataStream)); }
        public override void UnregisterFromDataStream(string userId, string streamType) { Trace("send", nameof(UnregisterFromDataStream)); }
        public override void SendData(string streamType, byte[] data) { if (traceHighFrequency) Trace("send", nameof(SendData)); }
    }
}
