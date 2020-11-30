using OrchestratorWrapping;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class OrchestratorController : MonoBehaviour, IOrchestratorMessagesListener, IOrchestratorResponsesListener, IUserMessagesListener, IUserSessionEventsListener
{
    #region enum

    public enum orchestratorConnectionStatus
    {
        __DISCONNECTED__,
        __CONNECTING__,
        __CONNECTED__
    }

    #endregion

    #region orchestration logics

    // the wrapper for the orchestrator
    private OrchestratorWrapper orchestratorWrapper;
    // the reference controller for singleton
    private static OrchestratorController instance;

    private orchestratorConnectionStatus connectionStatus;

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

    //LivePresenter
    private LivePresenterData livePresenterData;

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

    // Orchestrator Error Response Events
    public Action<ResponseStatus> OnErrorEvent;

    // Orchestrator Connection Events
    public Action<bool> OnConnectionEvent;
    public Action OnConnectingEvent;
    public Action<string> OnGetOrchestratorVersionEvent;

    // Orchestrator Messages Events
    public Action<string> OnOrchestratorRequestEvent;
    public Action<string> OnOrchestratorResponseEvent;

    // Orchestrator Login Events
    public Action<bool> OnLoginEvent;
    public Action<bool> OnLogoutEvent;
    public Action OnSignInEvent;

    // Orchestrator NTP clock Events
    public Action<NtpClock> OnGetNTPTimeEvent;

    // Orchestrator Sessions Events
    public Action<Session[]> OnGetSessionsEvent;
    public Action<Session> OnGetSessionInfoEvent;
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
    public orchestratorConnectionStatus ConnectionStatus { get { return connectionStatus; } }
    public bool UserIsLogged { get { return userIsLogged; } }
    public bool UserIsMaster { get { return userIsMaster; } }
    public User SelfUser { get { return me; } set { me = value; } }
    public User[] AvailableUserAccounts { get { return availableUserAccounts?.ToArray(); } }
    public User[] ConnectedUsers { get { return connectedUsers?.ToArray(); } }
    public Scenario[] AvailableScenarios { get { return availableScenarios?.ToArray(); } }
    public ScenarioInstance MyScenario { get { return myScenario; } }
    public Session[] AvailableSessions {  get { return availableSessions?.ToArray(); } }
    public Session MySession { get { return mySession; } }
    public RoomInstance[] AvailableRooms { get { return availableRoomInstances?.ToArray(); } }
    public LivePresenterData LivePresenterData { get { return livePresenterData; } }

    #endregion

    #region Unity

    private void Awake() 
    {
        DontDestroyOnLoad(this);

        if (instance == null) 
        {
            instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if(!(mySession is null))
        {
            Collect_SFU_Logs(mySession.sessionId);
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
        connectionStatus = orchestratorConnectionStatus.__CONNECTED__;
        OnConnectionEvent?.Invoke(true);

        orchestratorWrapper.GetOrchestratorVersion();
    }

    // SockerConnecting response callback
    public void OnConnecting()
    {
        connectionStatus = orchestratorConnectionStatus.__CONNECTING__;
        OnConnectingEvent?.Invoke();
    }

    // Abort Socket connection
    public void Abort()
    {
        orchestratorWrapper.Disconnect();
        OnDisconnect();
    }

    // Get connected Orchestrator version
    public void OnGetOrchestratorVersionResponse(ResponseStatus status, string version)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }
        OnGetOrchestratorVersionEvent?.Invoke(version);
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
        connectionStatus = orchestratorConnectionStatus.__DISCONNECTED__;
        userIsLogged = false;
        OnConnectionEvent?.Invoke(false);
    }

    #endregion

    #region Orchestrator Logs

    // Display the sent message in the logs
    public void OnOrchestratorRequest(string request)
    {
        OnOrchestratorRequestEvent?.Invoke(request);
    }

    // Display the received message in the logs
    public void OnOrchestratorResponse(int commandID, int status, string response)
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

        if(status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        if (!userIsLogged)
        {
            //user was not logged before request
            if (userLoggedSucessfully)
            {
                Debug.Log("[OrchestratorController][OnLoginResponse] User logged.");

                userIsLogged = true;

                // Replaced by UpdateUserData to update the IP adress field of the user on the Login.
                //orchestratorWrapper.GetUserInfo();

                UserData lData = new UserData();
                lData.userIP = GetIPAddress();
                UpdateUserData(lData);
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

        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

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

    public void SignIn(string pName, string pPassword)
    {
        orchestratorWrapper.AddUser(pName, pPassword, false);
    }

    #endregion

    #region NTP clock

    public void GetNTPTime()
    {
        Debug.Log("[OrchestratorController][GetNTPTime]::DateTimeNow::" + Helper.GetClockTimestamp(DateTime.Now));
        Debug.Log("[OrchestratorController][GetNTPTime]::DateTimeUTC::" + Helper.GetClockTimestamp(DateTime.UtcNow));
        orchestratorWrapper.GetNTPTime();
    }

    public void OnGetNTPTimeResponse(ResponseStatus status, NtpClock ntpTime)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        Debug.Log("[OrchestratorController][OnGetNTPTimeResponse]::NtpTime::" + ntpTime.Timestamp);
        Debug.Log("[OrchestratorController][OnGetNTPTimeResponse]::DateTimeUTC::" + Helper.GetClockTimestamp(DateTime.UtcNow));
        Debug.Log("[OrchestratorController][OnGetNTPTimeResponse]::DateTimeNow::" + Helper.GetClockTimestamp(DateTime.Now));

        OnGetNTPTimeEvent?.Invoke(ntpTime);
    }

    #endregion

    #region Sessions

    public void GetSessions()
    {
        orchestratorWrapper.GetSessions();
    }

    public void OnGetSessionsResponse(ResponseStatus status, List<Session> sessions)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

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
        if (status.Error != 0)
        {
            mySession = null;
            OnErrorEvent?.Invoke(status);
            return;
        }

        Debug.Log("[OrchestratorController][OnAddSessionResponse] Session " + session.sessionName + " successfully created by " + GetUser(session.sessionAdministrator).userName + ".");

        // success
        mySession = session;
        userIsMaster = session.sessionMaster == me.userId;
        connectedUsers = ExtractConnectedUsers(session.sessionUsers);

        availableSessions.Add(session);
        OnAddSessionEvent?.Invoke(session);

        // now retrieve the secnario instance infos
        orchestratorWrapper.GetScenarioInstanceInfo(session.scenarioId);
    }

    public void GetSessionInfo()
    {
        orchestratorWrapper.GetSessionInfo();
    }

    public void OnGetSessionInfoResponse(ResponseStatus status, Session session)
    {
        if(mySession == null || string.IsNullOrEmpty(session.sessionId))
        {
            Debug.Log("[OrchestratorController][OnGetSessionInfoResponse] Aborted, current session is null.");
            return;
        }

        if (status.Error != 0)
        {
            mySession = null;
            OnErrorEvent?.Invoke(status);
            return;
        }

        Debug.Log("[OrchestratorController][OnGetSessionInfoResponse] Get session info of " + session.sessionName + ".");

        // success
        mySession = session;
        userIsMaster = session.sessionMaster == me.userId;
        connectedUsers = ExtractConnectedUsers(session.sessionUsers);

        OnGetSessionInfoEvent?.Invoke(session);
    }

    public void OnGetScenarioInstanceInfoResponse(ResponseStatus status, ScenarioInstance scenario)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        Debug.Log("[OrchestratorController][OnGetScenarioInstanceInfoResponse] Scenario instance succesfully retrieved: " + scenario.scenarioName + ".");

        // now retrieve the url of the Live presenter stream
        orchestratorWrapper.GetLivePresenterData();
        myScenario = scenario;
        OnGetScenarioEvent?.Invoke(myScenario);
    }

    public void DeleteSession(string pSessionID)
    {
        orchestratorWrapper.DeleteSession(pSessionID);
    }

    public void OnDeleteSessionResponse(ResponseStatus status)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        Debug.Log("[OrchestratorController][OnDeleteSessionResponse] Session succesfully deleted.");

        OnDeleteSessionEvent?.Invoke();
        mySession = null;

        // update the lists of session, anyway the result
        orchestratorWrapper.GetSessions();
    }

    public void JoinSession(string pSessionID)
    {
        orchestratorWrapper.JoinSession(pSessionID);
    }

    public void OnJoinSessionResponse(ResponseStatus status, Session session)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        Debug.Log("[OrchestratorController][OnJoinSessionResponse] Session " + session.sessionName + " succesfully joined.");

        // success
        mySession = session;
        userIsMaster = session.sessionMaster == me.userId;
        connectedUsers = ExtractConnectedUsers(session.sessionUsers);

        // Simulate user join a session for each connected users
        foreach (string id in session.sessionUsers)
        {
            if (id != me.userId)
            {
                OnUserJoinedSession(id);
            }
        }

        OnJoinSessionEvent?.Invoke(mySession);
        OnSessionJoinedEvent?.Invoke();

        // now retrieve the secnario instance infos
        orchestratorWrapper.GetScenarioInstanceInfo(session.scenarioId);
    }

    public void LeaveSession()
    {
        orchestratorWrapper.LeaveSession();
    }

    public void OnLeaveSessionResponse(ResponseStatus status)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        Debug.Log("[OrchestratorController][OnLeaveSessionResponse] Session " + mySession.sessionName + " succesfully leaved.");

        // success
        myScenario = null;
        connectedUsers?.Clear();
        connectedUsers = null;
        OnLeaveSessionEvent?.Invoke();

        if (mySession != null && me != null)
        {
            Collect_SFU_Logs(mySession.sessionId);

            // As the session creator, the session should be deleted when leaving.
            if (mySession.sessionAdministrator == me.userId)
            {
                Debug.Log("[OrchestratorController][OnLeaveSessionResponse] As session creator, delete the current session when its empty.");
                StartCoroutine(WaitForEmptySessionToDelete());
                return;
            }
        }

        // Set this at the end and for the session creator, when the session has been deleted.
        mySession = null;
    }

    public void OnUserJoinedSession(string userID)
    {
        // Someone as joined the session
        if (!string.IsNullOrEmpty(userID))
        {
            Debug.Log("[OrchestratorController][OnUserJoinedSession] User " + GetUser(userID).userName + " joined the session.");
            orchestratorWrapper.GetSessionInfo();
            OnUserJoinSessionEvent?.Invoke(userID);
        }
    }

    public void OnUserLeftSession(string userID)
    {
        if (!string.IsNullOrEmpty(userID))
        {
            // If the session creator left, I need to leave also.
            if (mySession.sessionAdministrator == userID)
            {
                Debug.Log("[OrchestratorController][OnUserLeftSession] Session creator " + GetUser(userID).userName + " leaved the session.");
                LeaveSession();
            }
            // Otherwise, just proceed to the common user left event.
            else
            {
                Debug.Log("[OrchestratorController][OnUserLeftSession] User " + GetUser(userID).userName + " leaved the session.");
                // Required to update the list of connect users.
                orchestratorWrapper.GetSessionInfo();
                OnUserLeaveSessionEvent?.Invoke(userID);
            }
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
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

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
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        //Debug.Log("[OrchestratorController][OnGetLivePresenterDataResponse] Live stream url: " + liveData.liveAddress);
        livePresenterData = liveData;

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
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

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
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        if (userIsLogged)
        {
            Debug.Log("[OrchestratorController][OnAddUserResponse] User successfully added.");
            OnAddUserEvent?.Invoke(user);
            // update the lists of user, anyway the result
            orchestratorWrapper.GetUsers();
        }
        else
        {
            Debug.Log("[OrchestratorController][OnAddUserResponse] User successfully registered.");
            OnSignInEvent.Invoke();
        }
    }

    public void UpdateUserData(UserData pUserData)
    {
        orchestratorWrapper.UpdateUserDataJson(pUserData);
    }

    public void OnUpdateUserDataJsonResponse(ResponseStatus status)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        Debug.Log("[OrchestratorControler][OnUpdateUserDataJsonResponse] User data successfully updated.");
        orchestratorWrapper.GetUserInfo();
    }

    public void ClearUserData()
    {
        orchestratorWrapper.ClearUserData();
    }

    public void OnClearUserDataResponse(ResponseStatus status)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }
        
        Debug.Log("[OrchestratorControler][OnClearUserDataResponse] User data successfully cleaned-up.");
        orchestratorWrapper.GetUserInfo();
    }

    public void GetUserInfo(string pUserID)
    {
        orchestratorWrapper.GetUserInfo(pUserID);
    }

    public void OnGetUserInfoResponse(ResponseStatus status, User user)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        Debug.Log("[OrchestratorController][OnGetUserInfoResponse] Get info of user ID: " + user.userId);

        OnGetUserInfoEvent?.Invoke(user);

        if (isAutoRetrievingData)
        {
            // auto retriving phase: call next
            orchestratorWrapper.GetUsers();
        }
    }

    public void DeleteUser(string pUserID)
    {
        orchestratorWrapper.DeleteUser(pUserID);
    }

    public void OnDeleteUserResponse(ResponseStatus status)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

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
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

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
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
        }

        Debug.Log("[OrchestratorController][OnJoinRoomResponse] Room joined.");

        OnJoinRoomEvent?.Invoke(status.Error == 0);
    }

    public void LeaveRoom()
    {
        orchestratorWrapper.LeaveRoom();
    }

    public void OnLeaveRoomResponse(ResponseStatus status)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }

        Debug.Log("[OrchestratorController][OnLeaveRoomResponse] Room leaved.");

        OnLeaveRoomEvent?.Invoke();
    }

    #endregion

    #region Messages

    public void SendMessage(string pMessage, string pUserID)
    {
        orchestratorWrapper.SendMessage(pMessage, pUserID);
    }

    public void OnSendMessageResponse(ResponseStatus status)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }
    }

    public void SendMessageToAll(string pMessage)
    {
        orchestratorWrapper.SendMessageToAll(pMessage);
    }

    public void OnSendMessageToAllResponse(ResponseStatus status)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }
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
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }
        Debug.Log("[OrchestratorController][OnGetAvailableDataStreams] Available DataStream list count: " + dataStreams.Count);
    }

    public void GetRegisteredDataStreams()
    {
        OrchestratorWrapper.instance.GetRegisteredDataStreams();
    }

    public void OnGetRegisteredDataStreams(ResponseStatus status, List<DataStream> dataStreams)
    {
        if (status.Error != 0)
        {
            OnErrorEvent?.Invoke(status);
            return;
        }
        Debug.Log("[OrchestratorController][OnGetRegisteredDataStreams] Registered DataStream list count: " + dataStreams.Count);
    }

    #endregion

    #region Logics

    public User GetUser(string masterUuid)
    {
        if (availableUserAccounts != null)
        {
            for (int i = 0; i < availableUserAccounts.Count; i++)
            {
                if (availableUserAccounts[i].userId == masterUuid)
                    return availableUserAccounts[i];
            }
        }
        return null;
    }

    private List<User> ExtractConnectedUsers(string[] UserUUIDs)
    {
        List<User> users = new List<User>();

        for (int i = 0; i < UserUUIDs.Length; i++)
        {
            foreach (User u in availableUserAccounts)
            {
                if (UserUUIDs[i] == u.userId)
                {
                    users.Add(u);
                }
            }
        }

        return users;
    }

    private IEnumerator WaitForEmptySessionToDelete()
    {
        if (mySession == null)
        {
            yield break;
        }

        // Check frequently if there is users connected and ensure a null session (from the delete command) is escaped.
        while (mySession.sessionUsers.Length > 0)
        {
            GetSessionInfo();
            yield return new WaitForSeconds(1.0f);
        }

        // When the session is free of users, delete it.
        if (mySession.sessionUsers.Length == 0)
        {
            DeleteSession(mySession.sessionId);
        }
    }

    #endregion

    #region Utils

    public string GetIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                Debug.Log("[OrchestratorController][GetIPAdress] IPv4 adress: " + ip.ToString());
                return ip.ToString();
            }
        }
        Debug.Log("[OrchestratorController][GetIPAdress] Cannot retrieve IPv4 adress of the network adapater.");
        return "";
    }

    #endregion

    #region Logs

    private void Collect_SFU_Logs(string pSessionID)
    {
        string dnsURL = "https://vrt-orch-sfu-logs.viaccess-orca.com/";
        string requestURL = dnsURL + "?id=" + pSessionID + "&kind=sfu&download=1"; 
        Application.OpenURL(requestURL);
    }

    #endregion

    #region Errors

    public void OnError(ResponseStatus status)
    {
        Debug.Log("[OrchestratorController][OnError]::Error code: " + status.Error + "::Error message: " + status.Message);

        OnErrorEvent?.Invoke(status);
    }

    #endregion

    #endregion
}