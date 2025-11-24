using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VRT.Core;

using VRT.Orchestrator.Responses;
using VRT.Orchestrator.Interfaces;
using VRT.Orchestrator.Elements;

#if UNITY_EDITOR
using UnityEditor.Search;
#endif

#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Orchestrator.Wrapping
{
    public class OrchestratorController : MonoBehaviour, IOrchestratorResponsesListener, IUserMessagesListener, IUserSessionEventsListener
    {
        [Tooltip("Enable trace logging output")]
        [SerializeField] private bool enableLogging = true;

        #region enum

        public enum orchestratorConnectionStatus {
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
        private User _me; // Accessed via SelfUser

        //Session
        private Session mySession;
        private List<Session> availableSessions = new List<Session>();

        //Scenario
        private Scenario myScenario;

        // user Login state
        private bool userIsLogged = false;

        // user Login state
        private bool userIsMaster = false;

        // orchestrator connection state
        private bool connectedToOrchestrator = false;
        private bool hasBeenConnectedToOrchestrator = false;
        private bool autoStopOnLeave = false;

        #endregion

        #region public

        //Orchestrator Controller Singleton
        public static OrchestratorController Instance {
            get {
                if (instance is null) {
                    Debug.LogError("OrchestratorController.Instance: No OrchestratorController yet");
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
        
        // Orchestrator NTP clock Events
        public Action<NtpClock> OnGetNTPTimeEvent;

        // Orchestrator Sessions Events
        public Action<Session[]> OnSessionsEvent;
        public Action<Session> OnSessionInfoEvent;
        public Action<Session> OnAddSessionEvent;
        public Action<Session> OnJoinSessionEvent;
        public Action OnSessionJoinedEvent;
        public Action OnLeaveSessionEvent;
        public Action OnDeleteSessionEvent;
        public Action<string> OnUserJoinSessionEvent;
        public Action<string> OnUserLeaveSessionEvent;

        // Orchestrator User Messages Events
        public Action<UserMessage> OnUserMessageReceivedEvent;

        // Orchestrator User Messages Events
        public Action<UserEvent> OnMasterEventReceivedEvent;
        public Action<UserEvent> OnUserEventReceivedEvent;
        // Orchestrator Accessors
        public void LocalUserSessionForDevelopmentTests()
        {
            userIsMaster = true;
            mySession = new Session()
            {
                scenarioId = "LocalDevelopmentTest",
                sessionId = "0000"
            };
        }

        public bool ConnectedToOrchestrator { get { return connectedToOrchestrator; } }
        public orchestratorConnectionStatus ConnectionStatus { get { return connectionStatus; } }
        public bool UserIsLogged { get { return userIsLogged; } }
        public bool UserIsMaster { get { return userIsMaster; } }
        public User SelfUser { get { return _me; } set { _me = value; } }
        public Scenario CurrentScenario { get { return myScenario; } }
        public Session[] AvailableSessions { get { return availableSessions?.ToArray(); } }
        public Session CurrentSession { get { return mySession; } }

        #endregion

        #region Unity

        private void Awake() {
            if (instance == null) {
                DontDestroyOnLoad(this.gameObject);
                this.gameObject.name = this.gameObject.name + "_keep";
                instance = this;
            } else if (instance != this) {
#if UNITY_EDITOR
                string newName = SearchUtils.GetHierarchyPath(gameObject, false);
                string oldName = SearchUtils.GetHierarchyPath(instance.gameObject, false);
#else
                string newName = gameObject.name;
                string oldName = instance.gameObject.name;
#endif
                Debug.LogWarning($"OrchestratorController: attempt to create second instance from {newName}. Keep first one, from {oldName}.");
                // xxxjack Destroy(gameObject);
            }
        }

        void Start()
        {
            autoStopOnLeave = VRTConfig.Instance.AutoStart.autoStopAfterLeave;
        }

        private void OnDestroy() {
            Debug.Log($"{gameObject.name}: OrchestratorController.OnDestroy() called. Will close orchestrator connection. ");
            orchestratorWrapper?.Disconnect();

            if (mySession != null) {
#if VRT_WITH_STATS
                Statistics.Output("OrchestratorController", $"stopping=1, sessionId={mySession.sessionId}");
#endif
            }
            _OptionalStopOnLeave();
        }

        #endregion

        #region Socket.io connect

        // Connect to the orchestrator
        public void SocketConnect(string pUrl) {
            if (enableLogging) Debug.Log($"OrchestratorController: connect to {pUrl}");
#if VRT_WITH_STATS
            Statistics.Output("OrchestratorController", $"orchestrator_url={pUrl}");
#endif
            orchestratorWrapper = new OrchestratorWrapper(pUrl, this, this, this);
            orchestratorWrapper.Connect();
        }

        // SockerConnect response callback
        public void OnConnect()
        {
            if (enableLogging) Debug.Log($"OrchestratorController: connected to orchestrator");
            connectedToOrchestrator = true;
            hasBeenConnectedToOrchestrator = true;
            connectionStatus = orchestratorConnectionStatus.__CONNECTED__;
            OnConnectionEvent?.Invoke(true);
        }

        // SockerConnecting response callback
        public void OnConnecting() {
            if (enableLogging) Debug.Log($"OrchestratorController: connecting to orchestrator");
            if (hasBeenConnectedToOrchestrator)
            {
                Debug.LogWarning("OrchestratorController: attempting to reconnect to orchestrator");
            }
            connectionStatus = orchestratorConnectionStatus.__CONNECTING__;
            OnConnectingEvent?.Invoke();
        }

        // Abort Socket connection
        public void Abort() {
            orchestratorWrapper.Disconnect();
            OnDisconnect();
        }

        public void GetVersion()
        {
            orchestratorWrapper.GetOrchestratorVersion();
        }

        // Get connected Orchestrator version
        public void OnGetOrchestratorVersionResponse(ResponseStatus status, string version) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }
#if VRT_WITH_STATS
            Statistics.Output("OrchestratorController", $"orchestrator_version={version}");
#endif
            OnGetOrchestratorVersionEvent?.Invoke(version);
        }

        // SockerDisconnect response callback
        public void OnDisconnect() {
            Debug.LogWarning($"OrchestratorController: disconnected from orchestrator");
            SelfUser = null;
            connectedToOrchestrator = false;
            connectionStatus = orchestratorConnectionStatus.__DISCONNECTED__;
            userIsLogged = false;
            OnConnectionEvent?.Invoke(false);
        }

        #endregion

        #region Login/Logout

        public void Login(string pName, string pPassword) {
            SelfUser = new User();
            SelfUser.userName = pName;
            SelfUser.userPassword = pPassword;
            orchestratorWrapper.Login(pName, pPassword);
        }

        public void OnLoginResponse(ResponseStatus status, string userId) {
            bool userLoggedSucessfully = (status.Error == 0);

            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (!userIsLogged) {
                //user was not logged before request
                if (userLoggedSucessfully) {
                    if (enableLogging) Debug.Log("OrchestratorController: OnLoginResponse: User logged in.");

                    userIsLogged = true;
                    SelfUser.userId = userId;
                } else {
                    userIsLogged = false;
                }
            } else {
                //user was logged before previously
                if (!userLoggedSucessfully) {
                    // normal, user previopusly logged, nothing to do
                } else {
                    // should not occur
                }
            }

            OnLoginEvent?.Invoke(userLoggedSucessfully);
        }


        public void Logout() {
            orchestratorWrapper.Logout();
        }

        public void OnLogoutResponse(ResponseStatus status) {
            bool userLoggedOutSucessfully = (status.Error == 0);

            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (!userIsLogged) {
                //user was not logged before request
                if (!userLoggedOutSucessfully) {
                    // normal, was not logged, nothing to do
                } else {
                    // should not occur
                }
            } else {
                //user was logged before request
                if (userLoggedOutSucessfully) {
                    if (enableLogging) Debug.Log("OrchestratorController: OnLogoutResponse: User logout.");

                    //normal
                    SelfUser = null;
                    userIsLogged = false;
                } else {
                    // problem while logout
                    userIsLogged = true;
                }
            }

            OnLogoutEvent?.Invoke(userLoggedOutSucessfully);
        }

        #endregion

        #region NTP clock

        long timeOfGetNTPTimeRequest = 0;

        public static double GetClockTimestamp(System.DateTime pDate)
        {
            return pDate.Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds;
        }

        public void GetNTPTime() {
            if (enableLogging) Debug.Log("OrchestratorController: GetNTPTime: DateTimeNow: " + GetClockTimestamp(DateTime.Now));
            if (enableLogging) Debug.Log("OrchestratorController: GetNTPTime: DateTimeUTC: " + GetClockTimestamp(DateTime.UtcNow));
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            timeOfGetNTPTimeRequest = (long)sinceEpoch.TotalMilliseconds;
            orchestratorWrapper.GetNTPTime();
        }

        public void OnGetNTPTimeResponse(ResponseStatus status, NtpClock ntpTime) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("OrchestratorController: OnGetNTPTimeResponse: NtpTime: " + ntpTime.Timestamp);
            if (enableLogging) Debug.Log("OrchestratorController: OnGetNTPTimeResponse: DateTimeUTC: " + GetClockTimestamp(DateTime.UtcNow));
            if (enableLogging) Debug.Log("[OrchestratorController: OnGetNTPTimeResponse: DateTimeNow: " + GetClockTimestamp(DateTime.Now));
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            long localTimeMs = (long)sinceEpoch.TotalMilliseconds;
            long uncertainty = localTimeMs - timeOfGetNTPTimeRequest;
#if VRT_WITH_STATS
            Statistics.Output("OrchestratorController", $"orchestrator_ntptime_ms={ntpTime.ntpTimeMs}, localtime_behind_ms={ntpTime.ntpTimeMs - localTimeMs}, uncertainty_interval_ms={uncertainty}");
#endif
            if (OnGetNTPTimeEvent == null) Debug.LogWarning("OrchestratorController: NTP time response received but nothing listens");
            OnGetNTPTimeEvent?.Invoke(ntpTime);
        }

        #endregion

        #region Sessions

        public void GetSessions() {
            orchestratorWrapper.GetSessions();
        }

        public void OnGetSessionsResponse(ResponseStatus status, List<Session> sessions) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }
            int nRemoved = sessions.RemoveAll(item => item == null);
            if (nRemoved > 0)
            {
                Debug.LogWarning($"OrchestratorController: Removed {nRemoved} null sessions");
            }
            if (enableLogging) Debug.Log("OrchestratorController: OnGetSessionsResponse: Number of available sessions:" + sessions.Count);

            // update the list of available sessions
            availableSessions = sessions;

            OnSessionsEvent?.Invoke(sessions.ToArray());

    
        }

        public void AddSession(string pScenarioID, Scenario scOrch, string pSessionName, string pSessionDescription, string pSessionProtocol) {
            myScenario = scOrch;
            orchestratorWrapper.AddSession(pScenarioID, scOrch, pSessionName, pSessionDescription, pSessionProtocol);
        }

        public void OnAddSessionResponse(ResponseStatus status, Session session) {
            if (status.Error != 0) {
                mySession = null;
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("OrchestratorController: OnAddSessionResponse: Session " + session.sessionName + " successfully created by " + session.GetUser(session.sessionAdministrator).userName + ".");
            // success
            mySession = session;
            // We may need to update our own user definition (because the sfuData may have been added)
            User newMe = session.GetUser(SelfUser.userId);
            if (newMe == null)
            {
                Debug.LogError($"OrchestratorController: OnAddSessionResponse: userId {SelfUser.userId} (which is me) not in session");
                return;
            }
            SelfUser = newMe;
            userIsMaster = session.sessionMaster == SelfUser.userId;
            int  userCount = session.GetUserCount();
            
#if VRT_WITH_STATS
            Statistics.Output("OrchestratorController", $"created=1, sessionId={session.sessionId}, sessionName={session.sessionName}, isMaster={(userIsMaster?1:0)}, nUser={userCount}");
#endif

            availableSessions.Add(session);
            OnAddSessionEvent?.Invoke(session);
        }

        public void GetSessionInfo() {
            orchestratorWrapper.GetSessionInfo();
        }

        public void OnGetSessionInfoResponse(ResponseStatus status, Session session) {
            if (mySession == null || string.IsNullOrEmpty(session.sessionId)) {
                if (enableLogging) Debug.LogError("OrchestratorController: OnGetSessionInfoResponse: Aborted, current session is null.");
                return;
            }

            if (status.Error != 0) {
                if (enableLogging) Debug.LogError($"OrchestratorController: OnGetSessionInfoResponse: clear session, status={status}");
                mySession = null;
                OnErrorEvent?.Invoke(status);
                return;
            }

           
            // success
            mySession = session;
            userIsMaster = session.sessionMaster == SelfUser.userId;
            int userCount = mySession.GetUserCount();
            if (enableLogging) Debug.Log($"OrchestratorController: OnGetSessionInfoResponse: Get session info of {session.sessionName}, isMaster={(userIsMaster)}, nUser={userCount}");

            OnSessionInfoEvent?.Invoke(session);
        }

        public void DeleteSession(string pSessionID) {
            orchestratorWrapper.DeleteSession(pSessionID);
        }

        public void OnDeleteSessionResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("OrchestratorController: OnDeleteSessionResponse: Session succesfully deleted.");

            OnDeleteSessionEvent?.Invoke();
            mySession = null;

            // update the lists of session, anyway the result
            if (_OptionalStopOnLeave()) return;
            orchestratorWrapper.GetSessions();
        }

        public void JoinSession(string pSessionID) {
            orchestratorWrapper.JoinSession(pSessionID);
        }

        public void OnJoinSessionResponse(ResponseStatus status, Session session) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            
            // success
            mySession = session;
            userIsMaster = session.sessionMaster == SelfUser.userId;
            int userCount = session.GetUserCount();
            if (enableLogging) Debug.Log($"OrchestratorController: OnJoinSessionResponse: Session {session.sessionName}, isMaster={(userIsMaster)}, nUser={userCount}");

            // Simulate user join a session for each connected users
            foreach (string id in session.sessionUsers) {
                if (id != SelfUser.userId) {
                    OnUserJoinedSession(id, null);
                }
            }

            OnJoinSessionEvent?.Invoke(mySession);
            OnSessionJoinedEvent?.Invoke();
        }

        public void LeaveSession() {
            orchestratorWrapper?.LeaveSession();
        }

        public void OnLeaveSessionResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("OrchestratorController: OnLeaveSessionResponse: Session " + mySession.sessionName + " succesfully left.");

            // success
            myScenario = null;
            OnLeaveSessionEvent?.Invoke();

            if (mySession != null && SelfUser != null) {
 
                // As the session creator, the session should be deleted when leaving.
                if (mySession.sessionAdministrator == SelfUser.userId) {
                    if (enableLogging) Debug.Log("OrchestratorController: OnLeaveSessionResponse: As session creator, delete the current session when its empty.");
                    StartCoroutine(WaitForEmptySessionToDelete());
                    return;
                }
            }

            // Set this at the end and for the session creator, when the session has been deleted.
            mySession = null;
            _OptionalStopOnLeave();
        }

        bool _OptionalStopOnLeave()
        {
            // If wanted: stop playing (in editor), or quit application
            if (autoStopOnLeave)
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                return true;
            }

            return false;
        }

        public void OnUserJoinedSession(string userID, User user) {
            // Someone has joined the session
            if (string.IsNullOrEmpty(userID))
            {
                Debug.LogError("OrchestratorController: OnUserJoinedSession: empty userID");
            }
            if (user == null)
            {
                user = mySession.GetUser(userID);
                if (user == null)
                {
                    Debug.LogError($"OrchestratorController: OnUserJoinedSession: userID {userID} unknown");
                }
            }
            else
            {
                // xxxjack we don't add the user, but we call GetSessionInfo below to get a complete picture.
            }
            if (enableLogging) Debug.Log("OrchestratorController: OnUserJoinedSession: User " + user.userName + " joined the session.");
            orchestratorWrapper.GetSessionInfo();
            OnUserJoinSessionEvent?.Invoke(userID);
        }

        public void OnUserLeftSession(string userID) {
            if (!string.IsNullOrEmpty(userID)) {
                // If the session creator left, I need to leave also.
                if (mySession.sessionAdministrator == userID) {
                    Debug.Log("OrchestratorController: OnUserLeftSession: Session creator " + mySession.GetUser(userID).userName + " left the session. Also leaving.");
                    LeaveSession();
                }
                // Otherwise, just proceed to the common user left event.
                else {
                    if (enableLogging) Debug.Log("OrchestratorController: OnUserLeftSession: User " + mySession.GetUser(userID).userName + " left the session. Getting new session info.");
                    // Required to update the list of connect users.
                    orchestratorWrapper.GetSessionInfo();
                    OnUserLeaveSessionEvent?.Invoke(userID);
                }
            }
        }

        #endregion

        #region Users

        // xxxjack can go
        public void UpdateUserDataKey(string pKey, string pValue) {
        }

        public void OnUpdateUserDataResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("OrchestratorController: OnUpdateUserDataResponse: User data key updated.");
        }

        public void UpdateFullUserData(UserData pUserData) {
            orchestratorWrapper.UpdateUserDataJson(pUserData);
        }

        public void OnUpdateUserDataJsonResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("OrchestratorControler: OnUpdateUserDataJsonResponse: User data fully updated.");
        }

        #endregion

        #region Messages

        public void SendMessage(string pMessage, string pUserID) {
            orchestratorWrapper.SendMessage(pMessage, pUserID);
        }

        public void OnSendMessageResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }
        }

        public void SendMessageToAll(string pMessage) {
            orchestratorWrapper.SendMessageToAll(pMessage);
        }

        public void OnSendMessageToAllResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }
        }

        // Message from a user received spontaneously from the Orchestrator         
        public void OnUserMessageReceived(UserMessage userMessage) {
            if (userMessage.message.Substring(0,6) == "START_")
            {
                // xxxjack this is gross. We have to print the stats line for "session started" , because
                // in LoginController we don't know the session ID.
#if VRT_WITH_STATS
                Statistics.Output("OrchestratorController", $"starting=1, sessionId={mySession?.sessionId}, sessionName={mySession?.sessionName}");
#endif
                if (VRTConfig.Instance.AutoStart.autoLeaveAfter > 0)
                {
#if VRT_WITH_STATS
                    Statistics.Output("OrchestratorController", $"autoLeaveAfter={VRTConfig.Instance.AutoStart.autoLeaveAfter}");
#endif
                    Invoke("LeaveSession", VRTConfig.Instance.AutoStart.autoLeaveAfter);
                }
            }
            OnUserMessageReceivedEvent?.Invoke(userMessage);
        }

        #endregion

        #region Events

        public void SendEventToMaster(string pEventData) {
            byte[] lData = Encoding.ASCII.GetBytes(pEventData);

            if (lData != null) {
                orchestratorWrapper.SendSceneEventPacketToMaster(lData);
            }
        }

        public void SendEventToUser(string pUserID, string pEventData) {
            byte[] lData = Encoding.ASCII.GetBytes(pEventData);

            if (lData != null) {
                orchestratorWrapper.SendSceneEventPacketToUser(pUserID, lData);
            }
        }

        public void SendEventToAll(string pEventData) {
            if (!userIsMaster)
            {
                Debug.LogError("OrchestratorController: SendEventToAll() called, but not master user");
            }
            byte[] lData = Encoding.ASCII.GetBytes(pEventData);

            if (lData != null) {
                orchestratorWrapper.SendSceneEventPacketToAllUsers(lData);
            }
        }

        public void OnMasterEventReceived(UserEvent pMasterEventData) {
            if (pMasterEventData.sceneEventFrom != SelfUser.userId) {
                if (enableLogging) Debug.Log("OrchestratorController: OnMasterEventReceived: Master user: " + pMasterEventData.sceneEventFrom + " sent: " + pMasterEventData.sceneEventData);
                OnMasterEventReceivedEvent?.Invoke(pMasterEventData);
            }
        }

        public void OnUserEventReceived(UserEvent pUserEventData) {
            if (pUserEventData.sceneEventFrom != SelfUser.userId) {
                if (enableLogging) Debug.Log("OrchestratorController: OnUserEventReceived: User: " + pUserEventData.sceneEventFrom + " sent: " + pUserEventData.sceneEventData);
                OnUserEventReceivedEvent?.Invoke(pUserEventData);
            }
        }

        #endregion

        #region Logics

        private IEnumerator WaitForEmptySessionToDelete() {
            if (mySession == null) {
                _OptionalStopOnLeave();
                yield break;
            }

            // Check frequently if there is users connected and ensure a null session (from the delete command) is escaped.
            while (mySession.sessionUsers.Length > 0) {
                GetSessionInfo();
                yield return new WaitForSeconds(1.0f);
            }

            // When the session is free of users, delete it.
            if (mySession.sessionUsers.Length == 0) {
                DeleteSession(mySession.sessionId);
            }
            _OptionalStopOnLeave();
        }

        #endregion

        #region Errors

        public void OnError(ResponseStatus status) {
            if (enableLogging) Debug.Log("OrchestratorController: OnError: Error code: " + status.Error + "::Error message: " + status.Message);

            OnErrorEvent?.Invoke(status);
        }

        #endregion
    }
}