using System;
using VRT.Orchestrator.Elements;
using VRT.Orchestrator.Responses;

namespace VRT.Orchestrator.Wrapping
{
    /// <summary>
    /// Orchestrator interface for session creation, joining, and introspection.
    /// Used exclusively (or almost exclusively) by VRTLoginManager.
    /// </summary>
    public interface IVRTOrchestratorLogin : IVRTOrchestratorSessionState
    {
        // Connection events
        event Action OnConnectingEvent;
        event Action<string> OnGetOrchestratorVersionEvent;

        // Login events
        event Action<bool> OnLoginEvent;
        event Action<bool> OnLogoutEvent;

        // NTP event
        event Action<NtpClock> OnGetNTPTimeEvent;

        // Session events
        event Action<Session[]> OnSessionsEvent;
        event Action<Session> OnSessionInfoEvent;
        event Action<Session> OnAddSessionEvent;
        event Action<Session> OnJoinSessionEvent;
        event Action OnSessionJoinedEvent;
        event Action OnDeleteSessionEvent;

        // User message event (used by login to handle the START_* handshake for now)
        event Action<UserMessage> OnUserMessageReceivedEvent;

        // Session state visible to login
        Scenario CurrentScenario { get; }
        bool UserIsLogged { get; }
        Session[] AvailableSessions { get; }

        // Connection management
        void SocketConnect(string url);
        void Abort();
        void GetVersion();
        void Shutdown();

        // Login
        void Login(string name, string password);
        void Logout();

        // NTP
        void GetNTPTime();

        // Session management
        void GetSessions();
        void AddSession(string scenarioId, Scenario scenario, string name, string description, string protocol);
        void JoinSession(string sessionId);
        void DeleteSession(string sessionId);
        void GetSessionInfo();

        // User data
        void UpdateFullUserData(UserData userData);

        // Send to all (used by login to broadcast START_* for now)
        void SendMessageToAll(string message);

        // Development / testing
        void LocalUserSessionForDevelopmentTests();
    }
}
