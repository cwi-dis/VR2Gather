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
    public class NetworkOrchestratorController : OrchestratorController, IOrchestratorResponsesListener, IUserMessagesListener, IUserSessionEventsListener
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

        // Orchestrator Error Response Events
        public override event Action<ResponseStatus> OnErrorEvent;

        // Orchestrator Connection Events
        public override event Action<bool> OnConnectionEvent;
        public override event Action OnConnectingEvent;
        public override event Action<string> OnGetOrchestratorVersionEvent;

        // Orchestrator Login Events
        public override event Action<bool> OnLoginEvent;
        public override event Action<bool> OnLogoutEvent;

        // Orchestrator NTP clock Events
        public override event Action<NtpClock> OnGetNTPTimeEvent;

        // Orchestrator Sessions Events
        public override event Action<Session[]> OnSessionsEvent;
        public override event Action<Session> OnSessionInfoEvent;
        public override event Action<Session> OnAddSessionEvent;
        public override event Action<Session> OnJoinSessionEvent;
        public override event Action OnSessionJoinedEvent;
        public override event Action OnLeaveSessionEvent;
        public override event Action OnDeleteSessionEvent;
        public override event Action<string> OnUserJoinSessionEvent;
        public override event Action<string> OnUserLeaveSessionEvent;

        // Orchestrator User Messages Events
        public override event Action<UserMessage> OnUserMessageReceivedEvent;

        // Orchestrator Scene Events
        public override event Action<UserEvent> OnMasterEventReceivedEvent;
        public override event Action<UserEvent> OnUserEventReceivedEvent;

        // Orchestrator DataStream Events
        public override event Action<UserDataStreamPacket> OnDataStreamReceived;

        // Orchestrator Accessors
        public override void LocalUserSessionForDevelopmentTests()
        {
            userIsMaster = true;
            mySession = new Session()
            {
                scenarioId = "LocalDevelopmentTest",
                sessionId = "0000"
            };
        }

        public override bool ConnectedToOrchestrator { get { return connectedToOrchestrator; } }
        public orchestratorConnectionStatus ConnectionStatus { get { return connectionStatus; } }
        public override bool UserIsLogged { get { return userIsLogged; } }
        public override bool UserIsMaster { get { return userIsMaster; } }
        public override User SelfUser { get { return _me; } set { _me = value; } }
        public override Scenario CurrentScenario { get { return myScenario; } }
        public override Session[] AvailableSessions { get { return availableSessions?.ToArray(); } }
        public override Session CurrentSession { get { return mySession; } }

        #endregion

        #region Unity

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            autoStopOnLeave = VRTConfig.Instance.AutoStartConfig.autoStopAfterLeave;
        }

        protected override void OnDestroy()
        {
            Debug.Log($"{gameObject.name}: NetworkOrchestratorController.OnDestroy() called. Will close orchestrator connection. ");
            orchestratorWrapper?.Disconnect();

            if (mySession != null) {
#if VRT_WITH_STATS
                Statistics.Output("NetworkOrchestratorController", $"stopping=1, sessionId={mySession.sessionId}");
#endif
            }
            _OptionalStopOnLeave();
            base.OnDestroy();
        }

        #endregion

        #region Socket.io connect

        // Connect to the orchestrator
        public override void SocketConnect(string pUrl) {
            if (enableLogging) Debug.Log($"NetworkOrchestratorController: connect to {pUrl}");
#if VRT_WITH_STATS
            Statistics.Output("NetworkOrchestratorController", $"orchestrator_url={pUrl}");
#endif
            orchestratorWrapper = new OrchestratorWrapper(pUrl, this, this, this);
            orchestratorWrapper.OnDataStreamReceived += packet => OnDataStreamReceived?.Invoke(packet);
            orchestratorWrapper.Connect();
        }

        // SockerConnect response callback
        public void OnConnect()
        {
            if (enableLogging) Debug.Log($"NetworkOrchestratorController: connected to orchestrator");
            connectedToOrchestrator = true;
            hasBeenConnectedToOrchestrator = true;
            connectionStatus = orchestratorConnectionStatus.__CONNECTED__;
            OnConnectionEvent?.Invoke(true);
        }

        // SockerConnecting response callback
        public void OnConnecting() {
            if (enableLogging) Debug.Log($"NetworkOrchestratorController: connecting to orchestrator");
            if (hasBeenConnectedToOrchestrator)
            {
                Debug.LogWarning("NetworkOrchestratorController: attempting to reconnect to orchestrator");
            }
            connectionStatus = orchestratorConnectionStatus.__CONNECTING__;
            OnConnectingEvent?.Invoke();
        }

        // Abort Socket connection
        public override void Abort() {
            orchestratorWrapper.Disconnect();
            OnDisconnect();
        }

        // Tear down this controller (destroys the GameObject).
        public override void Shutdown() {
            Destroy(gameObject);
        }

        public override void GetVersion()
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
            Statistics.Output("NetworkOrchestratorController", $"connected=1, orchestrator_version={version}");
#endif
            OnGetOrchestratorVersionEvent?.Invoke(version);
        }

        // SockerDisconnect response callback
        public void OnDisconnect() {
#if VRT_WITH_STATS
            Statistics.Output("NetworkOrchestratorController", $"connected=0");
#endif
            SelfUser = null;
            connectedToOrchestrator = false;
            connectionStatus = orchestratorConnectionStatus.__DISCONNECTED__;
            userIsLogged = false;
            OnConnectionEvent?.Invoke(false);
        }

        #endregion

        #region Login/Logout

        public override void Login(string pName, string pPassword) {
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
                    if (enableLogging) Debug.Log("NetworkOrchestratorController: OnLoginResponse: User logged in.");

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


        public override void Logout() {
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
                    if (enableLogging) Debug.Log("NetworkOrchestratorController: OnLogoutResponse: User logout.");

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

        public override void GetNTPTime() {
            if (enableLogging) Debug.Log("NetworkOrchestratorController: GetNTPTime: DateTimeNow: " + GetClockTimestamp(DateTime.Now));
            if (enableLogging) Debug.Log("NetworkOrchestratorController: GetNTPTime: DateTimeUTC: " + GetClockTimestamp(DateTime.UtcNow));
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            timeOfGetNTPTimeRequest = (long)sinceEpoch.TotalMilliseconds;
            orchestratorWrapper.GetNTPTime();
        }

        public void OnGetNTPTimeResponse(ResponseStatus status, NtpClock ntpTime) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("NetworkOrchestratorController: OnGetNTPTimeResponse: NtpTime: " + ntpTime.Timestamp);
            if (enableLogging) Debug.Log("NetworkOrchestratorController: OnGetNTPTimeResponse: DateTimeUTC: " + GetClockTimestamp(DateTime.UtcNow));
            if (enableLogging) Debug.Log("[NetworkOrchestratorController: OnGetNTPTimeResponse: DateTimeNow: " + GetClockTimestamp(DateTime.Now));
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            long localTimeMs = (long)sinceEpoch.TotalMilliseconds;
            long uncertainty = localTimeMs - timeOfGetNTPTimeRequest;
#if VRT_WITH_STATS
            Statistics.Output("NetworkOrchestratorController", $"orchestrator_ntptime_ms={ntpTime.ntpTimeMs}, localtime_behind_ms={ntpTime.ntpTimeMs - localTimeMs}, uncertainty_interval_ms={uncertainty}");
#endif
            if (OnGetNTPTimeEvent == null) Debug.LogWarning("NetworkOrchestratorController: NTP time response received but nothing listens");
            OnGetNTPTimeEvent?.Invoke(ntpTime);
        }

        #endregion

        #region Sessions

        public override void GetSessions() {
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
                Debug.LogWarning($"NetworkOrchestratorController: Removed {nRemoved} null sessions");
            }
            if (enableLogging) Debug.Log("NetworkOrchestratorController: OnGetSessionsResponse: Number of available sessions:" + sessions.Count);

            // update the list of available sessions
            availableSessions = sessions;

            OnSessionsEvent?.Invoke(sessions.ToArray());


        }

        public override void AddSession(string pScenarioID, Scenario scOrch, string pSessionName, string pSessionDescription, string pSessionProtocol) {
            myScenario = scOrch;
            orchestratorWrapper.AddSession(pScenarioID, scOrch, pSessionName, pSessionDescription, pSessionProtocol);
        }

        public void OnAddSessionResponse(ResponseStatus status, Session session) {
            if (status.Error != 0) {
                mySession = null;
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("NetworkOrchestratorController: OnAddSessionResponse: Session " + session.sessionName + " successfully created by " + session.GetUser(session.sessionAdministrator).userName + ".");
            // success
            mySession = session;
            // We may need to update our own user definition (because the sfuData may have been added)
            User newMe = session.GetUser(SelfUser.userId);
            if (newMe == null)
            {
                Debug.LogError($"NetworkOrchestratorController: OnAddSessionResponse: userId {SelfUser.userId} (which is me) not in session");
                return;
            }
            SelfUser = newMe;
            userIsMaster = session.sessionMaster == SelfUser.userId;
            int  userCount = session.GetUserCount();

#if VRT_WITH_STATS
            Statistics.Output("NetworkOrchestratorController", $"created=1, sessionId={session.sessionId}, sessionName={session.sessionName}, isMaster={(userIsMaster?1:0)}, nUser={userCount}");
#endif

            availableSessions.Add(session);
            OnAddSessionEvent?.Invoke(session);
        }

        public override void GetSessionInfo() {
            orchestratorWrapper.GetSessionInfo();
        }

        public void OnGetSessionInfoResponse(ResponseStatus status, Session session) {
            if (mySession == null || string.IsNullOrEmpty(session.sessionId)) {
                if (enableLogging) Debug.LogError("NetworkOrchestratorController: OnGetSessionInfoResponse: Aborted, current session is null.");
                return;
            }

            if (status.Error != 0) {
                if (enableLogging) Debug.LogError($"NetworkOrchestratorController: OnGetSessionInfoResponse: clear session, status={status}");
                mySession = null;
                OnErrorEvent?.Invoke(status);
                return;
            }


            // success
            mySession = session;
            userIsMaster = session.sessionMaster == SelfUser.userId;
            int userCount = mySession.GetUserCount();
            if (enableLogging) Debug.Log($"NetworkOrchestratorController: OnGetSessionInfoResponse: Get session info of {session.sessionName}, isMaster={(userIsMaster)}, nUser={userCount}");

            OnSessionInfoEvent?.Invoke(session);
        }

        public override void DeleteSession(string pSessionID) {
            orchestratorWrapper.DeleteSession(pSessionID);
        }

        public void OnDeleteSessionResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("NetworkOrchestratorController: OnDeleteSessionResponse: Session succesfully deleted.");

            OnDeleteSessionEvent?.Invoke();
            mySession = null;

            // update the lists of session, anyway the result
            if (_OptionalStopOnLeave()) return;
            orchestratorWrapper.GetSessions();
        }

        public override void JoinSession(string pSessionID) {
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
            if (enableLogging) Debug.Log($"NetworkOrchestratorController: OnJoinSessionResponse: Session {session.sessionName}, isMaster={(userIsMaster)}, nUser={userCount}");

            // Simulate user join a session for each connected users
            foreach (string id in session.sessionUsers) {
                if (id != SelfUser.userId) {
                    OnUserJoinedSession(id, null);
                }
            }

            OnJoinSessionEvent?.Invoke(mySession);
            OnSessionJoinedEvent?.Invoke();
        }

        public override void LeaveSession() {
            orchestratorWrapper?.LeaveSession();
        }

        public void OnLeaveSessionResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("NetworkOrchestratorController: OnLeaveSessionResponse: Session " + mySession.sessionName + " succesfully left.");

            // success
            myScenario = null;
            if (_OptionalStopOnLeave()) return;
            OnLeaveSessionEvent?.Invoke();

            if (mySession != null && SelfUser != null) {

                // As the session creator, the session should be deleted when leaving.
                if (mySession.sessionAdministrator == SelfUser.userId) {
                    if (enableLogging) Debug.Log("NetworkOrchestratorController: OnLeaveSessionResponse: As session creator, delete the current session when its empty.");
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
                Debug.LogError("NetworkOrchestratorController: OnUserJoinedSession: empty userID");
            }
            if (user == null)
            {
                user = mySession.GetUser(userID);
                if (user == null)
                {
                    Debug.LogError($"NetworkOrchestratorController: OnUserJoinedSession: userID {userID} unknown");
                }
            }
            else
            {
                // xxxjack we don't add the user, but we call GetSessionInfo below to get a complete picture.
            }
            if (enableLogging) Debug.Log("NetworkOrchestratorController: OnUserJoinedSession: User " + user.userName + " joined the session.");
            orchestratorWrapper.GetSessionInfo();
            OnUserJoinSessionEvent?.Invoke(userID);
        }

        public void OnUserLeftSession(string userID) {
            if (!string.IsNullOrEmpty(userID)) {
                // If the session creator left, I need to leave also.
                if (mySession.sessionAdministrator == userID) {
                    Debug.Log("NetworkOrchestratorController: OnUserLeftSession: Session creator " + mySession.GetUser(userID).userName + " left the session. Also leaving.");
                    LeaveSession();
                }
                // Otherwise, just proceed to the common user left event.
                else {
                    if (enableLogging) Debug.Log("NetworkOrchestratorController: OnUserLeftSession: User " + mySession.GetUser(userID).userName + " left the session. Getting new session info.");
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

            if (enableLogging) Debug.Log("NetworkOrchestratorController: OnUpdateUserDataResponse: User data key updated.");
        }

        public override void UpdateFullUserData(UserData pUserData) {
            orchestratorWrapper.UpdateUserDataJson(pUserData);
        }

        public void OnUpdateUserDataJsonResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }

            if (enableLogging) Debug.Log("NetworkOrchestratorControler: OnUpdateUserDataJsonResponse: User data fully updated.");
        }

        #endregion

        #region Messages

        public override void SendMessage(string pMessage, string pUserID) {
            orchestratorWrapper.SendMessage(pMessage, pUserID);
        }

        public void OnSendMessageResponse(ResponseStatus status) {
            if (status.Error != 0) {
                OnErrorEvent?.Invoke(status);
                return;
            }
        }

        public override void SendMessageToAll(string pMessage) {
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
                Statistics.Output("NetworkOrchestratorController", $"starting=1, sessionId={mySession?.sessionId}, sessionName={mySession?.sessionName}");
#endif
                if (VRTConfig.Instance.AutoStartConfig.autoLeaveAfter > 0)
                {
#if VRT_WITH_STATS
                    Statistics.Output("NetworkOrchestratorController", $"autoLeaveAfter={VRTConfig.Instance.AutoStartConfig.autoLeaveAfter}");
#endif
                    Invoke("LeaveSession", VRTConfig.Instance.AutoStartConfig.autoLeaveAfter);
                }
            }
            OnUserMessageReceivedEvent?.Invoke(userMessage);
        }

        #endregion

        #region Events

        public override void SendEventToMaster(string pEventData) {
            byte[] lData = Encoding.ASCII.GetBytes(pEventData);

            if (lData != null) {
                orchestratorWrapper.SendSceneEventPacketToMaster(lData);
            }
        }

        public override void SendEventToUser(string pUserID, string pEventData) {
            byte[] lData = Encoding.ASCII.GetBytes(pEventData);

            if (lData != null) {
                orchestratorWrapper.SendSceneEventPacketToUser(pUserID, lData);
            }
        }

        public override void SendEventToAll(string pEventData) {
            if (!userIsMaster)
            {
                Debug.LogError("NetworkOrchestratorController: SendEventToAll() called, but not master user");
            }
            byte[] lData = Encoding.ASCII.GetBytes(pEventData);

            if (lData != null) {
                orchestratorWrapper.SendSceneEventPacketToAllUsers(lData);
            }
        }

        public void OnMasterEventReceived(UserEvent pMasterEventData) {
            if (pMasterEventData.sceneEventFrom != SelfUser.userId) {
                if (enableLogging) Debug.Log("NetworkOrchestratorController: OnMasterEventReceived: Master user: " + pMasterEventData.sceneEventFrom + " sent: " + pMasterEventData.sceneEventData);
                OnMasterEventReceivedEvent?.Invoke(pMasterEventData);
            }
        }

        public void OnUserEventReceived(UserEvent pUserEventData) {
            if (pUserEventData.sceneEventFrom != SelfUser.userId) {
                if (enableLogging) Debug.Log("NetworkOrchestratorController: OnUserEventReceived: User: " + pUserEventData.sceneEventFrom + " sent: " + pUserEventData.sceneEventData);
                OnUserEventReceivedEvent?.Invoke(pUserEventData);
            }
        }

        #endregion

        #region DataStreams

        public override void DeclareDataStream(string streamType) {
            orchestratorWrapper.DeclareDataStream(streamType);
        }

        public override void RemoveDataStream(string streamType) {
            orchestratorWrapper.RemoveDataStream(streamType);
        }

        public override void RegisterForDataStream(string userId, string streamType) {
            orchestratorWrapper.RegisterForDataStream(userId, streamType);
        }

        public override void UnregisterFromDataStream(string userId, string streamType) {
            orchestratorWrapper.UnregisterFromDataStream(userId, streamType);
        }

        public override void SendData(string streamType, byte[] data) {
            orchestratorWrapper.SendData(streamType, data);
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
            if (enableLogging) Debug.Log("NetworkOrchestratorController: OnError: Error code: " + status.Error + "::Error message: " + status.Message);

            OnErrorEvent?.Invoke(status);
        }

        #endregion
    }
}
