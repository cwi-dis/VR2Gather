using System;
using VRT.Orchestrator.Responses;

namespace VRT.Orchestrator.Wrapping
{
    /// <summary>
    /// Orchestrator interface for within-session communication: sending and
    /// receiving messages and events between participants. This is the interface
    /// that would be replaced when switching to a different transport (e.g. Fishnet).
    /// </summary>
    public interface IVRTOrchestratorComm : IVRTOrchestratorSessionState
    {
        // Incoming scene events
        event Action<UserEvent> OnMasterEventReceivedEvent;
        event Action<UserEvent> OnUserEventReceivedEvent;

        // Outgoing messages and scene events
        void SendMessage(string message, string userId);
        void SendEventToMaster(string eventData);
        void SendEventToAll(string eventData);
        void SendEventToUser(string userId, string eventData);
    }
}
