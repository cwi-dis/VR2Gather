using System;
using VRT.Orchestrator.Elements;
using VRT.Orchestrator.Responses;

namespace VRT.Orchestrator.Wrapping
{
    /// <summary>
    /// Session state that is relevant both during session creation/login and
    /// throughout the session lifetime. Used by both IVRTOrchestratorLogin and
    /// IVRTOrchestratorComm.
    /// </summary>
    public interface IVRTOrchestratorSessionState
    {
        // Error and connection events
        event Action<ResponseStatus> OnErrorEvent;
        event Action<bool> OnConnectionEvent;

        // Session lifecycle events (needed by both login UI and in-session code)
        event Action OnLeaveSessionEvent;
        event Action<string> OnUserJoinSessionEvent;
        event Action<string> OnUserLeaveSessionEvent;

        // Session state
        User SelfUser { get; set; }
        bool UserIsMaster { get; }
        Session CurrentSession { get; }
        bool ConnectedToOrchestrator { get; }

        void LeaveSession();
    }
}
