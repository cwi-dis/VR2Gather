using System;
using System.Collections.Generic;

//Interfaces to be implemented to supervise the orchestrator
namespace OrchestratorWrapping
{
    // Interface for clients that will use the orchestrator wrapper
    // each function is the response of a command and contains the data returned by the orchestrator
    // functions are called by the wrapper upon the response of the orchestrator
    public interface IOrchestratorResponsesListener
    {
        void OnConnect();
        void OnDisconnect();

        void OnLoginResponse(ResponseStatus status, string userId);
        void OnLogoutResponse(ResponseStatus status);

        void OnGetSessionsResponse(ResponseStatus status, List<Session> sessions);
        void OnAddSessionResponse(ResponseStatus status, Session session);
        void OnGetSessionInfoResponse(ResponseStatus status, Session session);
        void OnDeleteSessionResponse(ResponseStatus status);
        void OnJoinSessionResponse(ResponseStatus status);
        void OnLeaveSessionResponse(ResponseStatus status);

        void OnGetScenariosResponse(ResponseStatus status, List<Scenario> scenarios);
        void OnGetScenarioInstanceInfoResponse(ResponseStatus status, ScenarioInstance scenario);

        void OnGetUsersResponse(ResponseStatus status, List<User> scenarios);
        void OnAddUserResponse(ResponseStatus status, User user);
        void OnGetUserInfoResponse(ResponseStatus status, User user);
        void OnGetUserDataResponse(ResponseStatus status, UserData userData);
        void OnUpdateUserDataResponse(ResponseStatus status);
        void OnDeleteUserResponse(ResponseStatus status);

        void OnGetRoomsResponse(ResponseStatus status, List<RoomInstance> rooms);
        void OnJoinRoomResponse(ResponseStatus status);
        void OnLeaveRoomResponse(ResponseStatus status);

        void OnSendMessageResponse(ResponseStatus status);
        void OnSendMessageToAllResponse(ResponseStatus status);
    }

    // interface to implement to be updated from messages exchanged on the socketio
    public interface IOrchestratorMessageListener
    {
        void OnOrchestratorResponse(int status, string response);
        void OnOrchestratorRequest(string request);
    }
}