using System.Collections.Generic;
using VRT.Orchestrator.Responses;

//Interfaces to be implemented to supervise the orchestrator
namespace VRT.Orchestrator.Responses
{
    public interface IOrchestratorConnectionListener
    {
        void OnSocketConnect();
        void OnSocketConnecting();
        void OnSocketDisconnect();
        void OnSocketError(ResponseStatus message);
    }

    // Interface to implement to listen the user messages emitted spontaneously
    // by the orchestrator
    public interface IUserMessagesListener
    {
        void OnUserMessageReceived(UserMessage userMessage);
        void OnMasterEventReceived(UserEvent pSceneEventData);
        void OnUserEventReceived(UserEvent pSceneEventData);
    }

    // Interface to implement to listen the user events emitted spontaneously
    // from the session updates by the orchestrator
    public interface IUserSessionEventsListener
    {
        void OnUserJoinedSession(string userID, User user);
        void OnUserLeftSession(string userID);
    }

    // Interface for clients that will use the orchestrator wrapper
    // each function is the response of a command and contains the data returned by the orchestrator
    // functions are called by the wrapper upon the response of the orchestrator
    public interface IOrchestratorResponsesListener
    {
        void OnError(ResponseStatus status);
        void OnConnect();
        void OnConnecting();
        void OnGetOrchestratorVersionResponse(ResponseStatus status, string version);
        void OnDisconnect();

        void OnLoginResponse(ResponseStatus status, string userId);
        void OnLogoutResponse(ResponseStatus status);

        void OnGetNTPTimeResponse(ResponseStatus status, NtpClock ntpTime);

        void OnGetSessionsResponse(ResponseStatus status, List<Session> sessions);
        void OnAddSessionResponse(ResponseStatus status, Session session);
        void OnGetSessionInfoResponse(ResponseStatus status, Session session);
        void OnDeleteSessionResponse(ResponseStatus status);
        void OnJoinSessionResponse(ResponseStatus status, Session session);
        void OnLeaveSessionResponse(ResponseStatus status);
        void OnUpdateUserDataJsonResponse(ResponseStatus status);

        void OnSendMessageResponse(ResponseStatus status);
        void OnSendMessageToAllResponse(ResponseStatus status);

    }
}