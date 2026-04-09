using System;
using VRT.Orchestrator.Elements;
using VRT.Orchestrator.Responses;

/// <summary>
/// Interface for the orchestrator controller. All code outside the VRTOrchestratorImplementation
/// assembly should use this interface rather than referring to OrchestratorController directly.
/// </summary>
namespace VRT.Orchestrator.Wrapping
{
    public interface IVRTOrchestrator
    {
        // Error event
        event Action<ResponseStatus> OnErrorEvent;

        // Connection events
        event Action<bool> OnConnectionEvent;
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
        event Action OnLeaveSessionEvent;
        event Action OnDeleteSessionEvent;
        event Action<string> OnUserJoinSessionEvent;
        event Action<string> OnUserLeaveSessionEvent;

        // User message/event events
        event Action<UserMessage> OnUserMessageReceivedEvent;
        event Action<UserEvent> OnMasterEventReceivedEvent;
        event Action<UserEvent> OnUserEventReceivedEvent;

        // Properties
        User SelfUser { get; set; }
        bool UserIsMaster { get; }
        Session CurrentSession { get; }
        Scenario CurrentScenario { get; }
        bool ConnectedToOrchestrator { get; }
        bool UserIsLogged { get; }
        Session[] AvailableSessions { get; }

        // Connection
        void SocketConnect(string url);
        void Abort();
        void GetVersion();
        /// <summary>Tear down the orchestrator controller (e.g. destroy the GameObject).</summary>
        void Shutdown();

        // Login
        void Login(string name, string password);
        void Logout();

        // NTP
        void GetNTPTime();

        // Sessions
        void GetSessions();
        void AddSession(string scenarioId, Scenario scenario, string name, string description, string protocol);
        void JoinSession(string sessionId);
        void LeaveSession();
        void DeleteSession(string sessionId);
        void GetSessionInfo();

        // User data
        void UpdateFullUserData(UserData userData);

        // Messages
        void SendMessage(string message, string userId);
        void SendMessageToAll(string message);

        // Scene events
        void SendEventToMaster(string eventData);
        void SendEventToAll(string eventData);
        void SendEventToUser(string userId, string eventData);

        // Development / testing
        void LocalUserSessionForDevelopmentTests();
    }
}
