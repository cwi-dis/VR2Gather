using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using OrchestratorWrapping;

public class OrchestratorController : MonoBehaviour, IOrchestratorMessageIOListener, IOrchestratorResponsesListener, IMessagesFromOrchestratorListener, IUserSessionEventsListener
{
    #region orchestration logics

    // the wrapper for the orchestrator
    private OrchestratorWrapper orchestratorWrapper;
    // the reference controller for singleton
    private static OrchestratorController instance;

    //Users
    private User me;
    private List<User> connectedUsers;
    private List<User> availableUserAccounts;

    //Session
    private Session mySession;
    private List<Session> availableSessions;

    //Scenario
    private ScenarioInstance myScenario;
    private List<Scenario> availableScenarios;

    //Rooms
    private List<RoomInstance> availableRoomInstances;

    // user Login state
    private bool userIsLogged = false;

    // user Login state
    private bool userIsMaster = false;

    // orchestrator connection state
    private bool connectedToOrchestrator = false;

    // auto retrieving data on login: is used on login to chain the commands that allow to get the items available for the user (list of sessions, users, scenarios)
    private bool isAutoRetrievingData = false;

    #endregion

    #region public

    //Orchestrator Controller Singleton
    public static OrchestratorController Instance 
    { 
        get 
        { 
            if(instance is null)
            {
                instance = new GameObject("OrchestratorController").AddComponent<OrchestratorController>();
            }
            return instance; 
        } 
    }

    // Orchestrator Connection Events
    public Action<bool> OnConnectionEvent;

    // Orchestrator Messages Events
    public Action<string> OnOrchestratorRequestEvent;
    public Action<string> OnOrchestratorResponseEvent;

    // Orchestrator Login Events
    public Action<bool> OnLoginEvent;
    public Action<bool> OnLogoutEvent;

    // Orchestrator Sessions Events
    public Action<Session[]> OnGetSessionsEvent;
    public Action<Session> OnAddSessionEvent;
    public Action<Session> OnJoinSessionEvent;
    public Action OnSessionJoinedEvent;
    public Action OnLeaveSessionEvent;
    public Action OnDeleteSessionEvent;
    public Action<string> OnUserJoinSessionEvent;
    public Action<string> OnUserLeaveSessionEvent;

    // Orchestrator Scenarios Events
    public Action<ScenarioInstance> OnGetScenarioEvent;
    public Action<Scenario[]> OnGetScenariosEvent;

    // Orchestrator Live Events
    public Action<LivePresenterData> OnGetLiveDataEvent;

    // Orchestrator User Events
    public Action<User[]> OnGetUsersEvent;
    public Action<User> OnGetUserInfoEvent;
    public Action<User> OnAddUserEvent;

    // Orchestrator Rooms Events
    public Action<RoomInstance[]> OnGetRoomsEvent;
    public Action<bool> OnJoinRoomEvent;
    public Action OnLeaveRoomEvent;

    // Orchestrator User Messages Events
    public Action<UserMessage> OnUserMessageReceivedEvent;

    // Orchestrator User Messages Events
    public Action<UserEvent> OnMasterEventReceivedEvent;
    public Action<UserEvent> OnUserEventReceivedEvent;

    // Orchestrator Accessors
    public bool IsAutoRetrievingData { set { isAutoRetrievingData = connectedToOrchestrator; } }
    public bool ConnectedToOrchestrator { get { return connectedToOrchestrator; } }
    public bool UserIsLogged { get { return userIsLogged; } }
    public bool UserIsMaster { get { return userIsMaster; } }
    public User SelfUser { get { return me; } set { me = value; } }
    public User[] AvailableUserAccounts { get { return availableUserAccounts?.ToArray(); } }
    public User[] ConnectedUsers { get { return connectedUsers?.ToArray(); } }
    public Scenario[] AvailableScenarios { get { return availableScenarios?.ToArray(); } }
    public Session[] AvailableSessions {  get { return availableSessions?.ToArray(); } }
    public RoomInstance[] AvailableRooms { get { return availableRoomInstances?.ToArray(); } }

    #endregion

    #region Unity

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    #endregion

    #region Commands

    #region Socket.io connect

    // Connect to the orchestrator
    public void SocketConnect(string pUrl)
    {
        orchestratorWrapper = new OrchestratorWrapper(pUrl, this, this, this, this);
        orchestratorWrapper.Connect();
    }

    // SockerConnect response callback
    public void OnConnect()
    {
        connectedToOrchestrator = true;
        OnConnectionEvent?.Invoke(connectedToOrchestrator);
    }

    // Disconnect from the orchestrator
    public void socketDisconnect()
    {
        orchestratorWrapper.Disconnect();
    }

    // SockerDisconnect response callback
    public void OnDisconnect()
    {
        me = null;
        connectedToOrchestrator = false;
        userIsLogged = false;
        OnConnectionEvent?.Invoke(connectedToOrchestrator);
    }

    #endregion

    #region Orchestrator Logs

    // Display the sent message in the logs
    public void OnOrchestratorRequest(string request)
    {
        OnOrchestratorRequestEvent?.Invoke(request);
    }

    // Display the received message in the logs
    public void OnOrchestratorResponse(int status, string response)
    {
        OnOrchestratorResponseEvent?.Invoke(response);
    }

    #endregion

    #region Login/Logout

    public void Login(string pName, string pPassword)
    {
        orchestratorWrapper.Login(pName, pPassword);
    }

    public void OnLoginResponse(ResponseStatus status, string userId)
    {
        bool userLoggedSucessfully = (status.Error == 0);

        if (!userIsLogged)
        {
            //user was not logged before request
            if (userLoggedSucessfully)
            {
                Debug.Log("[OrchestratorController][OnLoginResponse] User logged.");

                userIsLogged = true;
                //orchestratorWrapper.UpdateUserDataJson("", "");
            }
            else
            {
                userIsLogged = false;
            }
        }
        else
        {
            //user was logged before previously
            if (!userLoggedSucessfully)
            {
                // normal, user previopusly logged, nothing to do
            }
            else
            {
                // should not occur
            }
        }

        OnLoginEvent?.Invoke(userLoggedSucessfully);
    }


    public void Logout()
    {
        orchestratorWrapper.Logout();
    }

    public void OnLogoutResponse(ResponseStatus status)
    {
        bool userLoggedOutSucessfully = (status.Error == 0);

        if (!userIsLogged)
        {
            //user was not logged before request
            if (!userLoggedOutSucessfully)
            {
                // normal, was not logged, nothing to do
            }
            else
            {
                // should not occur
            }
        }
        else
        {
            //user was logged before request
            if (userLoggedOutSucessfully)
            {
                Debug.Log("[OrchestratorController][OnLogoutResponse] User logout.");

                //normal
                me = null;
                userIsLogged = false;
            }
            else
            {
                // problem while logout
                userIsLogged = true;
            }
        }

        OnLogoutEvent?.Invoke(userLoggedOutSucessfully);
    }

    #endregion

    #region NTP clock

    public void GetNTPTime()
    {
        Debug.Log("[OrchestratorController][GetNTPTime]::DateTimeUTC::" + DateTime.UtcNow + DateTime.Now.Millisecond.ToString());
        orchestratorWrapper.GetNTPTime();
    }

    public void OnGetNTPTimeResponse(ResponseStatus status, string time)
    {
        Debug.Log("[OrchestratorController][OnGetNTPTimeResponse]::NtpTime::" + time);
        Debug.Log("[OrchestratorController][OnGetNTPTimeResponse]::DateTimeUTC::" + DateTime.UtcNow + DateTime.Now.Millisecond.ToString());
    }

    #endregion

    #region Sessions

    public void GetSessions()
    {
        orchestratorWrapper.GetSessions();
    }

    public void OnGetSessionsResponse(ResponseStatus status, List<Session> sessions)
    {
        Debug.Log("[OrchestratorController][OnGetSessionsResponse] Number of available sessions:" + sessions.Count);

        // update the list of available sessions
        availableSessions = sessions;

        OnGetSessionsEvent?.Invoke(sessions.ToArray());
        
        if (isAutoRetrievingData)
        {
            // auto retriving phase: this was the last call
            isAutoRetrievingData = false;
        }
    }

    public void AddSession(string pSessionID, string pSessionName, string pSessionDescription)
    {
        orchestratorWrapper.AddSession(pSessionID, pSessionName, pSessionDescription);
    }

    public void OnAddSessionResponse(ResponseStatus status, Session session)
    {
        // Is equal to AddSession + Join Session, except that session is returned (not on JoinSession)
        if (status.Error == 0)
        {
            // success
            mySession = session;
            userIsMaster = session.sessionMaster == me.userId;

            availableSessions.Add(session);
            OnAddSessionEvent?.Invoke(session);
            OnSessionJoinedEvent?.Invoke();
            orchestratorWrapper.GetScenarioInstanceInfo(session.scenarioId);
        }
        else
        {
            mySession = null;
        }
    }

    public void OnGetScenarioInstanceInfoResponse(ResponseStatus status, ScenarioInstance scenario)
    {
        if (status.Error == 0)
        {       
            // now retrieve the url of the Live presenter stream
            orchestratorWrapper.GetLivePresenterData();
            myScenario = scenario;
            OnGetScenarioEvent?.Invoke(myScenario);
        }
    }

    public void DeleteSession(string pSessionID)
    {
        orchestratorWrapper.DeleteSession(pSessionID);
    }

    public void OnDeleteSessionResponse(ResponseStatus status)
    {
        OnDeleteSessionEvent?.Invoke();

        // update the lists of session, anyway the result
        orchestratorWrapper.GetSessions();
    }

    public void JoinSession(string pSessionID)
    {
        orchestratorWrapper.JoinSession(pSessionID);
    }

    public void OnJoinSessionResponse(ResponseStatus status)
    {
        if (status.Error == 0)
        {
            // now we will need the session info with the sceanrio instance used for this session
            orchestratorWrapper.GetSessionInfo();
        }
    }

    public void OnGetSessionInfoResponse(ResponseStatus status, Session session)
    {
        if (status.Error == 0)
        {
            // success
            mySession = session;
            userIsMaster = session.sessionMaster == me.userId;

            // now retrieve the secnario instance infos
            orchestratorWrapper.GetScenarioInstanceInfo(session.scenarioId);

            foreach(string id in session.sessionUsers)
            {
                if(id != me.userId)
                {
                    OnUserJoinedSession(id);
                }
            }

            OnJoinSessionEvent?.Invoke(mySession);
            OnSessionJoinedEvent?.Invoke();
        }
        else
        {
            mySession = null;
        }
    }

    public void LeaveSession()
    {
        orchestratorWrapper.LeaveSession();
    }

    public void OnLeaveSessionResponse(ResponseStatus status)
    {
        if (status.Error == 0)
        {
            // success
            mySession = null;
            myScenario = null;
            connectedUsers?.Clear();
            connectedUsers = null;
            OnLeaveSessionEvent?.Invoke();
        }
    }

    public void OnUserJoinedSession(string userID)
    {
        if (!string.IsNullOrEmpty(userID))
        {
            AddConnectedUser(userID);
            OnUserJoinSessionEvent?.Invoke(userID);
        }
    }

    public void OnUserLeftSession(string userID)
    {
        if (!string.IsNullOrEmpty(userID))
        {
            DeletedConnectedUser(userID);
            OnUserLeaveSessionEvent?.Invoke(userID);
        }
    }

    #endregion

    #region Scenarios

    public void GetScenarios()
    {
        orchestratorWrapper.GetScenarios();
    }

    public void OnGetScenariosResponse(ResponseStatus status, List<Scenario> scenarios)
    {
        Debug.Log("[OrchestratorController][OnGetScenariosResponse] Number of available scenarios:" + scenarios.Count);

        // update the list of available scenarios
        availableScenarios = scenarios;
        OnGetScenariosEvent?.Invoke(scenarios.ToArray());

        if (isAutoRetrievingData)
        {
            // auto retriving phase: call next
            orchestratorWrapper.GetSessions();
        }
    }

    #endregion

    #region Live

    public void OnGetLivePresenterDataResponse(ResponseStatus status, LivePresenterData liveData)
    {
        //Debug.Log("[OrchestratorGui][OnGetLivePresenterDataResponse] Live stream url: " + liveData.liveAddress);

        OnGetLiveDataEvent?.Invoke(liveData);
        orchestratorWrapper.GetRooms();
    }

    #endregion

    #region Users

    public void GetUsers()
    {
        orchestratorWrapper.GetUsers();
    }

    public void OnGetUsersResponse(ResponseStatus status, List<User> users)
    {
        Debug.Log("[OrchestratorControler][OnGetUsersResponse] Users count:" + users.Count);

        availableUserAccounts = users;
        OnGetUsersEvent?.Invoke(users.ToArray());

        if (isAutoRetrievingData)
        {
            // auto retriving phase: call next
            orchestratorWrapper.GetScenarios();
        }
    }

    public void AddUser(string pUserName, string pUserPassword, bool pAdmin = false)
    {
        orchestratorWrapper.AddUser(pUserName, pUserPassword, pAdmin);
    }

    public void OnAddUserResponse(ResponseStatus status, User user)
    {
        OnAddUserEvent?.Invoke(user);
        // update the lists of user, anyway the result
        orchestratorWrapper.GetUsers();
    }

    public void UpdateUserData(string pMQname, string pMQurl)
    {
        orchestratorWrapper.UpdateUserDataJson(pMQname, pMQurl);
    }

    public void OnUpdateUserDataJsonResponse(ResponseStatus status)
    {
        if (status.Error == 0)
        {
            Debug.Log("[OrchestratorControler][OnUpdateUserDataJsonResponse] User data successfully updated.");
            orchestratorWrapper.GetUserInfo();
        }
    }

    public void GetUserInfo(string pUserID)
    {
        orchestratorWrapper.GetUserInfo(pUserID);
    }

    public void OnGetUserInfoResponse(ResponseStatus status, User user)
    {
        Debug.Log("[OrchestratorController][OnGetUserInfoResponse] Info of user ID: " + user.userId);

        if (status.Error == 0)
        {
            OnGetUserInfoEvent?.Invoke(user);

            if (isAutoRetrievingData)
            {
                // auto retriving phase: call next
                orchestratorWrapper.GetUsers();
            }
        }
    }

    public void DeleteUser(string pUserID)
    {
        orchestratorWrapper.DeleteUser(pUserID);
    }

    public void OnDeleteUserResponse(ResponseStatus status)
    {
        Debug.Log("[OrchestratorController][OnDeleteUserResponse]");

        // update the lists of user, anyway the result
        orchestratorWrapper.GetUsers();
    }

    #endregion

    #region Rooms

    public void GetRooms()
    {
        orchestratorWrapper.GetRooms();
    }

    public void OnGetRoomsResponse(ResponseStatus status, List<RoomInstance> rooms)
    {
        Debug.Log("[OrchestratorController][OnGetRoomsResponse] Rooms count:" + rooms.Count);

        // update the list of available rooms
        availableRoomInstances = rooms;
        OnGetRoomsEvent?.Invoke(rooms.ToArray());
    }

    public void JoinRoom(string pRoomID)
    {
        orchestratorWrapper.JoinRoom(pRoomID);
    }

    public void OnJoinRoomResponse(ResponseStatus status)
    {
        OnJoinRoomEvent?.Invoke(status.Error == 0);
    }

    public void LeaveRoom()
    {
        orchestratorWrapper.LeaveRoom();
    }

    public void OnLeaveRoomResponse(ResponseStatus status)
    {
        if (status.Error == 0)
        {
            OnLeaveRoomEvent?.Invoke();
        }
    }

    #endregion

    #region Messages

    public void SendMessage(string pMessage, string pUserID)
    {
        orchestratorWrapper.SendMessage(pMessage, pUserID);
    }

    public void OnSendMessageResponse(ResponseStatus status)
    {
    }

    public void SendMessageToAll(string pMessage)
    {
        orchestratorWrapper.SendMessageToAll(pMessage);
    }

    public void OnSendMessageToAllResponse(ResponseStatus status)
    {
    }

    // Message from a user received spontaneously from the Orchestrator         
    public void OnUserMessageReceived(UserMessage userMessage)
    {
        OnUserMessageReceivedEvent?.Invoke(userMessage);
    }

    #endregion

    #region Events

    public void SendEventToMaster(string pEventData)
    {
        byte[] lData = Encoding.ASCII.GetBytes(pEventData);
        
        if (lData != null)
        {
            orchestratorWrapper.SendSceneEventPacketToMaster(lData);
        }
    }

    public void SendEventToUser(string pUserID, string pEventData)
    {
        byte[] lData = Encoding.ASCII.GetBytes(pEventData);

        if (lData != null)
        {
            orchestratorWrapper.SendSceneEventPacketToUser(pUserID, lData);
        }
    }

    public void SendEventToAll(string pEventData)
    {
        byte[] lData = Encoding.ASCII.GetBytes(pEventData);

        if (lData != null)
        {
            orchestratorWrapper.SendSceneEventPacketToAllUsers(lData);
        }
    }

    public void OnMasterEventReceived(UserEvent pMasterEventData)
    {
        if (pMasterEventData.fromId != me.userId)
        {
            //Debug.Log("[OrchestratorController][OnMasterEventReceived] Master user: " + pMasterEventData.fromId + " sent: " + pMasterEventData.message);
            OnMasterEventReceivedEvent?.Invoke(pMasterEventData);
        }
    }

    public void OnUserEventReceived(UserEvent pUserEventData)
    {
        if(pUserEventData.fromId != me.userId)
        {
            //Debug.Log("[OrchestratorController][OnUserEventReceived] User: " + pUserEventData.fromId + " sent: " + pUserEventData.message);
            OnUserEventReceivedEvent?.Invoke(pUserEventData);
        }
    }

    #endregion

    #region Data bit-stream

    public void GetAvailableDataStreams(string pDataStreamUserId)
    {
        OrchestratorWrapper.instance.GetAvailableDataStreams(pDataStreamUserId);
    }

    public void OnGetAvailableDataStreams(ResponseStatus status, List<DataStream> dataStreams)
    {
        Debug.Log("[OrchestratorController][OnGetAvailableDataStreams] Available DataStream list count: " + dataStreams.Count);
    }

    public void GetRegisteredDataStreams()
    {
        OrchestratorWrapper.instance.GetRegisteredDataStreams();
    }

    public void OnGetRegisteredDataStreams(ResponseStatus status, List<DataStream> dataStreams)
    {
        Debug.Log("[OrchestratorController][OnGetRegisteredDataStreams] Registered DataStream list count: " + dataStreams.Count);
    }

    #endregion

    #region Logics

    private void AddConnectedUser(string pUserID)
    {
        if(connectedUsers == null)
        {
            connectedUsers = new List<User>();
        }

        foreach(User u in availableUserAccounts)
        {
            if(u.userId == pUserID)
            {
                connectedUsers.Add(u);
            }
        }
    }

    private void DeletedConnectedUser(string pUserID)
    {
        if (connectedUsers == null || connectedUsers.Count == 0)
        {
            return;
        }

        User lUserToRemove = null;

        foreach (User u in connectedUsers)
        {
            if (u.userId == pUserID)
            {
                lUserToRemove = u;
                break;
            }
        }

        if(lUserToRemove != null)
        {
            connectedUsers.Remove(lUserToRemove);
        }
    }

    #endregion

    #endregion
}