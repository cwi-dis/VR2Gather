using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrchestratorWrapping;
using UnityEngine.SceneManagement;

public enum State {
    Offline, Login, Default, Create, Join, Lobby, InGame
}

public class OrchestrationWindow : MonoBehaviour, IOrchestratorMessageIOListener, IOrchestratorResponsesListener, IMessagesFromOrchestratorListener, IUserSessionEventsListener {

    #region UI

    public bool isDebug = false;
    public bool useTVM = true;
    public bool usePC = false;
    public bool useAudio = false;
    public bool useSocketIOAudio = false;
    public bool useDashAudio = false;
    private int kindRepresentation = 0;
    private int kindAudio = 0;

    [HideInInspector] public bool isMaster = false;
    [HideInInspector] public string userID = "";

    private State state = State.Offline;
    private bool updated = false;
    private bool imJoining = false;

    [Header("Status")]
    public string orchestratorUrl;
    public bool autoRetrieveOrchestratorDataOnConnect;
    [SerializeField] private Text statusText = null;
    [SerializeField] private Text idText = null;
    [SerializeField] private Text nameText = null;

    [Header("Login")]
    public InputField userNameLoginIF;
    public InputField userPasswordLoginIF;
    public InputField connectionURILoginIF;
    public InputField exchangeNameLoginIF;
    public InputField pcDashServerLoginIF;
    public InputField audioDashServerLoginIF;

    [Header("Content")]
    public RectTransform orchestratorSessions;
    public RectTransform usersSession;

    [Header("Info")]
    public InputField exchangeNameIF;
    public InputField connectionURIIF;
    public InputField pcDashServerIF;
    public InputField audioDashServerIF;

    [Header("Create")]
    public InputField sessionNameIF;
    public InputField sessionDescriptionIF;
    public Dropdown scenarioIdDrop;
    public Toggle presenterToggle;
    public Toggle liveToggle;
    public Toggle tvmToggle;
    public Toggle pcToggle;
    public Toggle noAudioToggle;
    public Toggle socketAudioToggle;
    public Toggle dashAudioToggle;

    [Header("Join")]
    public Dropdown sessionIdDrop;

    [Header("Lobby")]
    public Text sessionNameText;
    public Text sessionDescriptionText;
    public Text scenarioIdText;
    public Text sessionNumUsersText;

    [Header("Panels")]
    [SerializeField] private GameObject loginPanel = null;
    [SerializeField] private GameObject infoPanel = null;
    [SerializeField] private GameObject createPanel = null;
    [SerializeField] private GameObject joinPanel = null;
    [SerializeField] private GameObject lobbyPanel = null;
    [SerializeField] private GameObject sessionPanel = null;
    [SerializeField] private GameObject usersPanel = null;

    [Header("Buttons")]
    [SerializeField] private Button createButton = null;
    [SerializeField] private Button joinButton = null;
    [SerializeField] private Button readyLobbyButton = null;
    [SerializeField] private Button PCCalibButton = null;
    [SerializeField] private Button TVMCalibButton = null;
    
    // Logs container
    [Header("Logs container")]
    [SerializeField] private RectTransform logsContainer = null;
    [SerializeField] private ScrollRect logsScrollRect = null;
    private Font ArialFont;

    #endregion

    #region Utils
    private Color onlineCol = new Color(0.15f, 0.78f, 0.15f); // Green
    private Color offlineCol = new Color(0.78f, 0.15f, 0.15f); // Red
    #endregion

    #region orchestration logics

    [HideInInspector] public PilotController controller = null;

    // the wrapper for the orchestrator
    private OrchestratorWrapper orchestratorWrapper;
        
    // lists of items that are availble for the user
    public List<Session> availableSessions;
    public List<Scenario> availableScenarios;
    public List<User> availableUsers;
    public List<RoomInstance> availableRoomInstances;

    public Session activeSession;
    public ScenarioInstance activeScenario;
    public LivePresenterData livePresenterData;

    // user Login state
    private bool userIsLogged = false;

    // auto retrieving data on login: is used on login to chain the commands that allow to get the items available for the user (list of sessions, users, scenarios)
    private bool isAutoRetrievingData = false;

    #endregion

    #region GUI               

    private IEnumerator ScrollLogsToBottom() {
        yield return new WaitForSeconds(0.2f);
        logsScrollRect.verticalScrollbar.value = 0;
    }

    // Fill a scroll view with a text item
    public void AddTextComponentOnContent(Transform container, string value) {
        GameObject textGO = new GameObject();
        textGO.name = "Text-" + value;
        textGO.transform.SetParent(container);
        Text item = textGO.AddComponent<Text>();
        item.font = ArialFont;
        item.fontSize = 18;
        item.color = Color.black;

        ContentSizeFitter lCsF = textGO.AddComponent<ContentSizeFitter>();
        lCsF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform rectTransform;
        rectTransform = item.GetComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(0, 0, 0);
        rectTransform.sizeDelta = new Vector2(2000, 30);
        rectTransform.localScale = Vector3.one;
        item.horizontalOverflow = HorizontalWrapMode.Wrap;
        item.verticalOverflow = VerticalWrapMode.Overflow;

        item.text = value;
    }

    // remove a component from a list
    public void removeComponentsFromList(Transform container) {
        for (var i = container.childCount - 1; i >= 0; i--) {
            var obj = container.GetChild(i);
            obj.transform.SetParent(null);
            Destroy(obj.gameObject);
        }
    }

    #endregion
    
    #region Commands

    #region Socket.io connect

    // Connect to the orchestrator
    public void SocketConnect() {
        orchestratorWrapper = new OrchestratorWrapper(orchestratorUrl, this, this, this, this);
        orchestratorWrapper.Connect();
    }

    // implementation des callbacks de retour de l'interface
    public void OnConnect() {
        statusText.text = "Online";
        statusText.color = onlineCol;
        state = State.Login;
        PanelChanger();
    }


    // Disconnect from the orchestrator
    private void socketDisconnect() {
        orchestratorWrapper.Disconnect();
    }

    public void OnDisconnect() {
        userIsLogged = false;
        userID = "";
        idText.text = "";
        nameText.text = "";
        statusText.text = "Offline";
        statusText.color = offlineCol;
    }

    #endregion

    #region Orchestrator Logs

    // Display the received message in the logs
    public void OnOrchestratorResponse(int status, string response) {
        AddTextComponentOnContent(logsContainer.transform, "<<< " + response);
        StartCoroutine(ScrollLogsToBottom());
    }

    // Display the sent message in the logs
    public void OnOrchestratorRequest(string request) {
        AddTextComponentOnContent(logsContainer.transform, ">>> " + request);
    }

    #endregion

    #region Login/Logout

    private void Login() {
        orchestratorWrapper.Login(userNameLoginIF.text, userPasswordLoginIF.text);
    }

    public void OnLoginResponse(ResponseStatus status, string _userId) {
        bool userLoggedSucessfully = (status.Error == 0);

        if (!userIsLogged) {
            //user was not logged before request
            if (userLoggedSucessfully) {
                userIsLogged = true;
                if (autoRetrieveOrchestratorDataOnConnect) {
                    // tag to warn other callbacks that we are auto retrieving data, each call back will call the next one
                    // to get a full state of the orchestrator
                    isAutoRetrievingData = true;
                }
                else {
                    isAutoRetrievingData = false;
                }

                orchestratorWrapper.UpdateUserDataJson(exchangeNameLoginIF.text, connectionURILoginIF.text);
                userID = _userId;
                idText.text = _userId;
                state = State.Default;
                PanelChanger();
            }
            else {
                userIsLogged = false;
                userID = "";
                idText.text = "";
                nameText.text = "";
            }
        }
        else {
            //user was logged before previously
            if (!userLoggedSucessfully) {
                // normal, user previopusly logged, nothing to do
            }
            else {
                // should not occur
            }
        }
    }
    
    private void Logout() {
        orchestratorWrapper.Logout();
    }

    public void OnLogoutResponse(ResponseStatus status) {
        bool userLoggedOutSucessfully = (status.Error == 0);

        if (!userIsLogged) {
            //user was not logged before request
            if (!userLoggedOutSucessfully) {
                // normal, was not logged, nothing to do
            }
            else {
                // should not occur
            }
        }
        else {
            //user was logged before request
            if (userLoggedOutSucessfully) {
                //normal
                userIsLogged = false;
                userID = "";
                idText.text = "";
                nameText.text = "";
                state = State.Login;
                PanelChanger();
            }
            else {
                // problem while logout
                userIsLogged = true;
            }
        }
    }

    #endregion

    #region NTP clock

    private void GetNTPTime() {
        Debug.Log("GetNTPTime::DateTimeUTC::" + DateTime.UtcNow + DateTime.Now.Millisecond.ToString());
        orchestratorWrapper.GetNTPTime();
    }

    public void OnGetNTPTimeResponse(ResponseStatus status, string time) {
        Debug.Log("OnGetNTPTimeResponse::NtpTime::" + time);
        Debug.Log("OnGetNTPTimeResponse::DateTimeUTC::" + DateTime.UtcNow + DateTime.Now.Millisecond.ToString());
    }

    #endregion

    #region Sessions

    public void GetSessions() {
        orchestratorWrapper.GetSessions();
    }

    public void OnGetSessionsResponse(ResponseStatus status, List<Session> sessions) {
        // update the list of available sessions
        availableSessions = sessions;
        removeComponentsFromList(orchestratorSessions.transform);
        sessions.ForEach(delegate (Session element) {
            AddTextComponentOnContent(orchestratorSessions.transform, element.GetGuiRepresentation());
        });

        // update the dropdown
        Dropdown dd = sessionIdDrop;
        dd.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        availableSessions.ForEach(delegate (Session session) {
            /*
            Debug.Log("Session:" + session.GetGuiRepresentation());
            Debug.Log("users:" + session.sessionUsers.Length);
            Array.ForEach<string>(session.sessionUsers, delegate (string user) {
                Debug.Log("userId:" + user);
            });
            */
            options.Add(new Dropdown.OptionData(session.GetGuiRepresentation()));
        });
        dd.AddOptions(options);

        if (isAutoRetrievingData) {
            // auto retriving phase: this was the last call
            isAutoRetrievingData = false;
        }
    }
    
    private void AddSession() {
        orchestratorWrapper.AddSession( availableScenarios[scenarioIdDrop.value].scenarioId,
                                        sessionNameIF.text, sessionDescriptionIF.text);
    }

    public void OnAddSessionResponse(ResponseStatus status, Session session) {
        // Is equal to AddSession + Join Session, except that session is returned (not on JoinSession)
        if (status.Error == 0) {
            // success
            availableSessions.Add(session);
            // update the list of available sessions
            removeComponentsFromList(orchestratorSessions.transform);
            availableSessions.ForEach(delegate (Session element) {
                AddTextComponentOnContent(orchestratorSessions.transform, element.GetGuiRepresentation());
            });

            // update the dropdown
            Dropdown dd = sessionIdDrop;
            dd.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            availableSessions.ForEach(delegate (Session sess) {
                options.Add(new Dropdown.OptionData(sess.GetGuiRepresentation()));
            });
            dd.AddOptions(options);

            sessionNameText.text = session.sessionName;
            sessionDescriptionText.text = session.sessionDescription;
            sessionNumUsersText.text = session.sessionUsers.Length.ToString() + "/" + "4"; // To change the max users depending the pilot
            //scenarioIdText.text = session.scenarioId;

            orchestratorWrapper.GetScenarioInstanceInfo(session.scenarioId);

            activeSession = session;

            if (AudioManager.Instance != null) { // Load Socket.io Audio
                AudioManager.Instance.StartRecordAudio();
            }

            removeComponentsFromList(usersSession.transform);
            for (int i = 0; i < activeSession.sessionUsers.Length; i++) {
                // Make this to show the real name of the user, not the id
                foreach (User u in availableUsers) {
                    if (u.userId == activeSession.sessionUsers[i])
                        AddTextComponentOnContent(usersSession.transform, u.userName);
                }
            }

            state = State.Lobby;
            PanelChanger();
        }
        else {
            sessionNameText.text = "";
            sessionDescriptionText.text = "";
            sessionNumUsersText.text = "X/X";
            scenarioIdText.text = "";
            removeComponentsFromList(usersSession.transform);

            activeSession = null;
        }
    }
    
    public void OnGetScenarioInstanceInfoResponse(ResponseStatus status, ScenarioInstance scenario) {
        if (status.Error == 0) {
            scenarioIdText.text = scenario.scenarioName;
            // now retrieve the list of the available rooms
            //orchestratorWrapper.GetRooms();

            // now retrieve the url of the Live presenter stream
            orchestratorWrapper.GetLivePresenterData();

            activeScenario = scenario;
        }
        else {
            scenarioIdText.text = "";
        }
    }
    
    private void DeleteSession() {
        Dropdown dd = sessionIdDrop;
        orchestratorWrapper.DeleteSession(availableSessions[dd.value].sessionId);
    }

    public void OnDeleteSessionResponse(ResponseStatus status) {
        // update the lists of session, anyway the result
        orchestratorWrapper.GetSessions();
    }
    
    private void JoinSession() {
        Dropdown dd = sessionIdDrop;
        string sessionIdToJoin = availableSessions[dd.value].sessionId;
        //userSession.text = sessionIdToJoin;
        orchestratorWrapper.JoinSession(sessionIdToJoin);        
    }

    public void OnJoinSessionResponse(ResponseStatus status) {
        if (status.Error == 0) {
            imJoining = true;
            // now we wwill need the session info with the sceanrio instance used for this session
            orchestratorWrapper.GetSessionInfo();

            state = State.Lobby;
            PanelChanger();
        }
        else {
            sessionNameText.text = "";
            sessionDescriptionText.text = "";
            sessionNumUsersText.text = "X/X";
            scenarioIdText.text = "";
        }
    }

    public void OnGetSessionInfoResponse(ResponseStatus status, Session session) {
        if (status.Error == 0) {
            // success
            sessionNameText.text = session.sessionName;
            sessionDescriptionText.text = session.sessionDescription;
            sessionNumUsersText.text = session.sessionUsers.Length.ToString() + "/" + "4"; // To change the max users depending the pilot
            //scenarioIdText.text = session.scenarioId;            

            if (!updated) {
                if (AudioManager.Instance != null) { // Load Socket.io Audio
                    AudioManager.Instance.StartRecordAudio();
                }
                foreach (string id in session.sessionUsers) {
                    if (id != idText.text) {
                        OnUserJoinedSession(id);
                    }
                }
            }

            // now retrieve the secnario instance infos
            if (!updated) orchestratorWrapper.GetScenarioInstanceInfo(session.scenarioId);
            else updated = false;

            activeSession = session;
            imJoining = false;

            removeComponentsFromList(usersSession.transform);
            for (int i = 0; i < activeSession.sessionUsers.Length; i++) {
                // Make this to show the real name of the user, not the id
                foreach (User u in availableUsers) {
                    if (u.userId == activeSession.sessionUsers[i])
                        AddTextComponentOnContent(usersSession.transform, u.userName);
                }
            }   
        }
        else {
            sessionNameText.text = "";
            sessionDescriptionText.text = "";
            sessionNumUsersText.text = "X/X";
            scenarioIdText.text = "";

            activeSession = null;
        }
    }
    
    public void LeaveSession() {
        orchestratorWrapper.LeaveSession();
    }

    public void OnLeaveSessionResponse(ResponseStatus status) {
        if (status.Error == 0) {
            // success
            sessionNameText.text = "";
            sessionDescriptionText.text = "";
            sessionNumUsersText.text = "X/X";
            scenarioIdText.text = "";

            activeSession = null;
            activeScenario = null;
            livePresenterData = null;

            removeComponentsFromList(usersSession.transform);

            state = State.Default;
            PanelChanger();
        }
    }

    public void OnUserJoinedSession(string _userID) {
        if (!string.IsNullOrEmpty(_userID)) {
            if (AudioManager.Instance != null) {
                AudioManager.Instance.StartListeningAudio2(_userID);
            }
            updated = true;  
            if (!imJoining) orchestratorWrapper.GetUserInfo(_userID);
            /*
            foreach (User u in availableUsers) {
                if (u.userId == _userID)
                    Debug.Log(u.userName + " Joined");
            }
            */
        }
    }

    public void OnUserLeftSession(string _userID) {
        if (!string.IsNullOrEmpty(_userID)) {
            updated = true;
            //orchestratorWrapper.GetUserInfo(_userID);
            /*
            foreach (User u in availableUsers) {
                if (u.userId == _userID)
                    Debug.Log(u.userName + " Leaved");
            }
            */
        }
    }

    #endregion

    #region Scenarios

    private void GetScenarios() {
        orchestratorWrapper.GetScenarios();
    }

    public void OnGetScenariosResponse(ResponseStatus status, List<Scenario> scenarios) {
//        Debug.Log("OnGetScenariosResponse:" + scenarios.Count);

        availableScenarios = scenarios;

        //update the data in the dropdown
        Dropdown dd = scenarioIdDrop;
        dd.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        scenarios.ForEach(delegate (Scenario scenario) {
            /*
            Debug.Log("Scenario:" + scenario.GetGuiRepresentation());
            Debug.Log("ScenarioRooms:" + scenario.scenarioRooms.Count);
            scenario.scenarioRooms.ForEach(delegate (Room room) {
                Debug.Log("ScenarioRoom:" + room.GetGuiRepresentation());
            });
            */
            options.Add(new Dropdown.OptionData(scenario.GetGuiRepresentation()));
        });
        dd.AddOptions(options);

        if (isAutoRetrievingData) {
            // auto retriving phase: call next
            orchestratorWrapper.GetSessions();
        }
    }

    #endregion

    #region Live 

    public void OnGetLivePresenterDataResponse(ResponseStatus status, LivePresenterData liveData) {
        if (livePresenterData == null) livePresenterData = new LivePresenterData();
        livePresenterData.liveAddress = liveData.liveAddress;
        livePresenterData.vodAddress = liveData.vodAddress;
        /*
        Debug.Log("Live: " + liveData.liveAddress);
        Debug.Log("VoD: " + liveData.vodAddress);
        */
        orchestratorWrapper.GetRooms();
    }

    #endregion

    #region Users

    public bool watingForUser = false;

    public void GetUsers() {
        watingForUser = true;
        orchestratorWrapper.GetUsers();
    }

    public void OnGetUsersResponse(ResponseStatus status, List<User> users) {
        watingForUser = false;
        // update the list of available users
        availableUsers = users;
/*
        users.ForEach(delegate (User user) {
            Debug.Log("Name: " + user.userName + " -- URL: " + user.sfuData.url_pcc);
        });
*/
        if (isAutoRetrievingData) {
            // auto retriving phase: call next
            orchestratorWrapper.GetScenarios();
        }
        else if (updated) {
            orchestratorWrapper.GetSessionInfo();
        }
    }
    
    private void AddUser() {
        //orchestratorWrapper.AddUser(userNamePanel.GetComponentInChildren<InputField>().text,
        //    userPasswordPanel.GetComponentInChildren<InputField>().text,
        //    userAdminPanel.GetComponentInChildren<Toggle>().isOn);
    }

    public void OnAddUserResponse(ResponseStatus status, User user) {
        // update the lists of user, anyway the result
        GetUsers();
    }
    
    private void UpdateUserData() {
        orchestratorWrapper.UpdateUserDataJson(exchangeNameIF.text, connectionURIIF.text);
    }

    public void OnUpdateUserDataJsonResponse(ResponseStatus status) {
        if (status.Error == 0) {
            orchestratorWrapper.GetUserInfo();
        }
    }

    private void GetUserInfo(string name) {
        foreach(User u in availableUsers) {
            if (u.userName == name) {
                orchestratorWrapper.GetUserInfo(u.userId);
            }
        }
    }

    public void OnGetUserInfoResponse(ResponseStatus status, User user) {
        if (status.Error == 0) {
            if (string.IsNullOrEmpty(idText.text) || user.userId == idText.text) {
                userID = user.userId;
                idText.text = user.userId;
                nameText.text = user.userName;
                exchangeNameIF.text = user.userData.userMQexchangeName;
                connectionURIIF.text = user.userData.userMQurl;
                pcDashServerIF.text = user.sfuData.url_pcc;
                audioDashServerIF.text = user.sfuData.url_audio;
            }


            GetUsers();
            //if (isAutoRetrievingData || updated) {
            //}
        }
    }
    
    private void DeleteUser() {
        //Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        //orchestratorWrapper.DeleteUser(availableUsers[dd.value].userId);
    }

    public void OnDeleteUserResponse(ResponseStatus status) {
        // update the lists of user, anyway the result
        GetUsers();
    }

    #endregion

    #region Rooms

    private void GetRooms() {
        orchestratorWrapper.GetRooms();
    }

    public void OnGetRoomsResponse(ResponseStatus status, List<RoomInstance> rooms) {
        // update the list of available rooms
        availableRoomInstances = rooms;
        //updateListComponent(orchestratorUserSessions.transform, Orchestrator.orchestrator.orchestratorUserSessions);

        //update the data in the dropdown
        //Dropdown dd = roomIdPanel.GetComponentInChildren<Dropdown>();
        //dd.ClearOptions();
        //List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        //rooms.ForEach(delegate (RoomInstance room) {
        //    Debug.Log("Room:" + room.GetGuiRepresentation());
        //    options.Add(new Dropdown.OptionData(room.GetGuiRepresentation()));
        //});
        //dd.AddOptions(options);

        orchestratorWrapper.GetUserInfo(userID);
    }

    public void OnJoinRoomResponse(ResponseStatus status) {
        
    }

    public void OnLeaveRoomResponse(ResponseStatus status) {
        

    }

    #endregion

    #region Messages

    private void SendMessage() {
        //Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        //orchestratorWrapper.SendMessage(messagePanel.GetComponentInChildren<InputField>().text,
        //    availableUsers[dd.value].userId);
    }

    public void OnSendMessageResponse(ResponseStatus status) {
        // nothing to do on the GUI
    }
    
    public void SendMessageToAll(string _message) {
        orchestratorWrapper.SendMessageToAll(_message);
    }

    public void OnSendMessageToAllResponse(ResponseStatus status) {
        // nothing to do on the GUI
    }
    
    // Message from a user received spontaneously from the Orchestrator         
    public void OnUserMessageReceived(UserMessage userMessage) {
        AddTextComponentOnContent(logsContainer.transform, "<<< USER MESSAGE RECEIVED: " + userMessage.fromName + "[" + userMessage.fromId + "]: " + userMessage.message.ToString());
        StartCoroutine(ScrollLogsToBottom());

        controller.MessageActivation(userMessage.message.ToString());
    }

    #endregion

    #region Utilities

    public void SendPing(string _message, string _userId) {
        orchestratorWrapper.SendMessage(_message, _userId);
    }

    #endregion

    #endregion

    #region Unity

    void Start() {
        //Hardcoded ClockSync on Windows machines
        //if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) SyncTool.SyncSystemClock();

        ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

        updated = false;

        tvmToggle.isOn = true;
        pcToggle.isOn = false;
        noAudioToggle.isOn = true;
        socketAudioToggle.isOn = false;
        dashAudioToggle.isOn = false;

        // Set status to offline
        OnDisconnect();
        PanelChanger();

        // Try to connect
        SocketConnect();

        DontDestroyOnLoad(this);
    }
    
    void Update() {
        if (SceneManager.GetActiveScene().name == "LoginManager") {
            gameObject.GetComponent<Canvas>().enabled = true;


        }
        else {
            gameObject.GetComponent<Canvas>().enabled = false;
        }

        if(state== State.Create){
            // Check Pilot number to show optional toggles
            Pilot2PresenterToggles();
            Pilot2UserRepresentationToggle();
            Pilot2AudioToggle();
        }
    }

    public void PanelChanger() {
        switch (state) {
            case State.Offline:
                loginPanel.SetActive(false);
                infoPanel.SetActive(false);
                createPanel.SetActive(false);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                sessionPanel.SetActive(false);
                usersPanel.SetActive(false);
                createButton.gameObject.SetActive(false);
                joinButton.gameObject.SetActive(false);
                PCCalibButton.gameObject.SetActive(true);
                TVMCalibButton.gameObject.SetActive(true);
                break;
            case State.Login:
                loginPanel.SetActive(true);
                infoPanel.SetActive(false);
                createPanel.SetActive(false);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                sessionPanel.SetActive(false);
                usersPanel.SetActive(false);
                createButton.gameObject.SetActive(false);
                joinButton.gameObject.SetActive(false);
                PCCalibButton.gameObject.SetActive(true);
                TVMCalibButton.gameObject.SetActive(true);
                break;
            case State.Default:
                loginPanel.SetActive(false);
                infoPanel.SetActive(true);
                createPanel.SetActive(false);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                sessionPanel.SetActive(true);
                usersPanel.SetActive(false);
                createButton.gameObject.SetActive(true);
                joinButton.gameObject.SetActive(true);
                createButton.interactable = true;
                joinButton.interactable = true;
                PCCalibButton.gameObject.SetActive(true);
                TVMCalibButton.gameObject.SetActive(true);
                PCCalibButton.interactable = true;
                TVMCalibButton.interactable = true;
                break;
            case State.Create:
                loginPanel.SetActive(false);
                infoPanel.SetActive(true);
                createPanel.SetActive(true);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                sessionPanel.SetActive(true);
                usersPanel.SetActive(false);
                createButton.gameObject.SetActive(true);
                joinButton.gameObject.SetActive(true);
                createButton.interactable = false;
                joinButton.interactable = true;
                PCCalibButton.gameObject.SetActive(true);
                TVMCalibButton.gameObject.SetActive(true);
                PCCalibButton.interactable = true;
                TVMCalibButton.interactable = true;
                break;
            case State.Join:
                loginPanel.SetActive(false);
                infoPanel.SetActive(true);
                createPanel.SetActive(false);
                joinPanel.SetActive(true);
                lobbyPanel.SetActive(false);
                sessionPanel.SetActive(true);
                usersPanel.SetActive(false);
                createButton.gameObject.SetActive(true);
                joinButton.gameObject.SetActive(true);
                createButton.interactable = true;
                joinButton.interactable = false;
                PCCalibButton.gameObject.SetActive(true);
                TVMCalibButton.gameObject.SetActive(true);
                PCCalibButton.interactable = true;
                TVMCalibButton.interactable = true;
                break;
            case State.Lobby:
                loginPanel.SetActive(false);
                infoPanel.SetActive(true);
                createPanel.SetActive(false);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(true);
                if (isMaster) readyLobbyButton.interactable = true;
                else readyLobbyButton.interactable = false;
                sessionPanel.SetActive(false);
                usersPanel.SetActive(true);
                createButton.gameObject.SetActive(true);
                joinButton.gameObject.SetActive(true);
                createButton.interactable = false;
                joinButton.interactable = false;
                PCCalibButton.gameObject.SetActive(true);
                TVMCalibButton.gameObject.SetActive(true);
                PCCalibButton.interactable = false;
                TVMCalibButton.interactable = false;
                break;
            case State.InGame:
                break;
            default:
                break;
        }
    }

    private void Pilot2PresenterToggles() {
        if (availableScenarios != null && availableScenarios[scenarioIdDrop.value].scenarioName == "Pilot 2") {
            presenterToggle.gameObject.SetActive(true);
            // Check if presenter is active to show live option
            if (presenterToggle.isOn) liveToggle.gameObject.SetActive(true);
            else liveToggle.gameObject.SetActive(false);
        }
        else presenterToggle.gameObject.SetActive(false);
    }

    private void Pilot2UserRepresentationToggle() {
        if (availableScenarios != null && availableScenarios[scenarioIdDrop.value].scenarioName == "Pilot 2") {
            tvmToggle.gameObject.SetActive(true);
            pcToggle.gameObject.SetActive(true);

            useTVM = tvmToggle.isOn;
            usePC = pcToggle.isOn;

            if (useTVM) kindRepresentation = 0;
            else kindRepresentation = 1;
        }
        else {
            tvmToggle.gameObject.SetActive(false);
            pcToggle.gameObject.SetActive(false);
        }
    }

    private void Pilot2AudioToggle() {
        if (availableScenarios != null && availableScenarios[scenarioIdDrop.value].scenarioName == "Pilot 2") {
            noAudioToggle.gameObject.SetActive(true);
            socketAudioToggle.gameObject.SetActive(true);
            dashAudioToggle.gameObject.SetActive(true);

            useAudio = !noAudioToggle.isOn;
            useSocketIOAudio = socketAudioToggle.isOn;
            useDashAudio = dashAudioToggle.isOn;

            socketAudioToggle.interactable = false; //TODO: Until audio over socket will be multi-threaded with new Queue system

            if (noAudioToggle.isOn) noAudioToggle.interactable = false;
            else noAudioToggle.interactable = true;
            //if (socketAudioToggle.isOn) socketAudioToggle.interactable = false;
            //else socketAudioToggle.interactable = true;
            if (dashAudioToggle.isOn) dashAudioToggle.interactable = false;
            else dashAudioToggle.interactable = true;
        }
        else {
            noAudioToggle.gameObject.SetActive(false);
            socketAudioToggle.gameObject.SetActive(false);
            dashAudioToggle.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Buttons

    public void GetUsersButton() {
        GetUsers();
    }

    public void LoginButton() {
        Login();
    }

    public void CreateButton() {
        state = State.Create;
        PanelChanger();
    }

    public void JoinButton() {
        state = State.Join;
        PanelChanger();
    }

    public void DoneButton() {
        if (state == State.Create) {
            isMaster = true;
            AddSession();
        }
        else {
            isMaster = false;
            JoinSession();
        }
    }

    public void LeaveButton() {
        isMaster = false;
        LeaveSession();
    }

    public void ReadyButton() {
        if (isMaster) {
            if (activeScenario.scenarioName == "Pilot 1") SendMessageToAll(MessageType.START + "_" + activeScenario.scenarioName);
            else if (activeScenario.scenarioName == "Pilot 2") SendMessageToAll(MessageType.START + "_" + activeScenario.scenarioName + "_" +
                                                                                kindRepresentation + "_" + kindAudio + "_" + presenterToggle.isOn + "_" + liveToggle.isOn);
            //else if (activeScenario.scenarioName == "Pilot 2") SendMessageToAll(MessageType.START + "_" + activeScenario.scenarioName + "_" + presenterToggle.isOn + "_" + liveToggle.isOn);
        }
        else SendMessageToAll(MessageType.READY);
    }

    public void PCCalib() {
        SceneManager.LoadScene("PCCalibration");
    }

    public void TVMCalib() {
        SceneManager.LoadScene("TVMCalibration");
    }
    
    public void AutoFillButtons(int user) {
        if (user == 0) {
            userNameLoginIF.text = "Marc@i2CAT";
            userPasswordLoginIF.text = "i2CAT2020";
            connectionURILoginIF.text = "amqp://tofis:tofis@91.126.37.138:5672";
            exchangeNameLoginIF.text = "210TVM";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-Marc/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-Marc/audio.mpd";
        }
        else if (user == 1) {
            userNameLoginIF.text = "Luca@i2CAT";
            userPasswordLoginIF.text = "i2CAT2020";
            connectionURILoginIF.text = "amqp://tofis:tofis@91.126.37.137:5672";
            exchangeNameLoginIF.text = "110TVM";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-Luca/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-Luca/audio.mpd";
        }
        else if (user == 2) {
            userNameLoginIF.text = "Spiros@CERTH";
            userPasswordLoginIF.text = "CERTH2020";
            connectionURILoginIF.text = "amqp://tofis:tofis@91.126.37.138:5672";
            exchangeNameLoginIF.text = "Fake1";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-Spiros/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-Spiros/audio.mpd";
        }
        else if (user == 3) {
            userNameLoginIF.text = "Argyris@CERTH";
            userPasswordLoginIF.text = "CERTH2020";
            connectionURILoginIF.text = "amqp://tofis:tofis@91.126.37.138:5672";
            exchangeNameLoginIF.text = "Fake1";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-Argyris/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-Argyris/audio.mpd";
        }
        else if (user == 4) {
            userNameLoginIF.text = "Jack@CWI";
            userPasswordLoginIF.text = "CWI2020";
            connectionURILoginIF.text = "amqp://volumetric:capture@192.168.37.127:5672";
            exchangeNameLoginIF.text = "TVMmpeg-pc";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-Jack/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-Jack/audio.mpd";
        }
        else if (user == 5) {
            userNameLoginIF.text = "Shishir@CWI";
            userPasswordLoginIF.text = "CWI2020";
            connectionURILoginIF.text = "amqp://volumetric:capture@192.168.37.161:5672";
            exchangeNameLoginIF.text = "TVMarecibo";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-Shishir/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-Shishir/audio.mpd";
        }
        else if (user == 6) {
            userNameLoginIF.text = "Fernando@ENTROPY";
            userPasswordLoginIF.text = "ENTROPY2020";
            connectionURILoginIF.text = "amqp://tofis:tofis@192.168.11.122:5672";
            exchangeNameLoginIF.text = "fernando";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-Fernando/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-Fernando/audio.mpd";
        }
        else if (user == 7) {
            userNameLoginIF.text = "Vincent@VO";
            userPasswordLoginIF.text = "VO2020";
            connectionURILoginIF.text = "amqp://tofis:tofis@192.168.11.122:5672";
            exchangeNameLoginIF.text = "vincent";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-Vincent/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-Vincent/audio.mpd";
        }
        else if (user == 8) {
            userNameLoginIF.text = "cwibig";
            userPasswordLoginIF.text = "CWI2020";
            connectionURILoginIF.text = "amqp://volumetric:capture@192.168.37.127:5672";
            exchangeNameLoginIF.text = "TVMarecibo";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-cwibig/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-cwibig/audio.mpd";
        }
        else if (user == 9) {
            userNameLoginIF.text = "cwismall";
            userPasswordLoginIF.text = "CWI2020";
            connectionURILoginIF.text = "amqp://volumetric:capture@192.168.37.161:5672";
            exchangeNameLoginIF.text = "TVMmpeg-pc";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-cwismall/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-cwismall/audio.mpd";
        }
        else if (user == 10) {
            userNameLoginIF.text = "cwitiny";
            userPasswordLoginIF.text = "CWI2020";
            connectionURILoginIF.text = "amqp://volumetric:capture@192.168.37.112:5672";
            exchangeNameLoginIF.text = "TVMgargamel";
            pcDashServerLoginIF.text = "https://vrt-pcl2dash.viaccess-orca.com/pc-cwitiny/testBed.mpd";
            audioDashServerLoginIF.text = "https://vrt-evanescent.viaccess-orca.com/audio-cwitiny/audio.mpd";
        }
    }

    #endregion

    #region Toggles
    public void SetTVM() {
        useTVM = tvmToggle.isOn;
        pcToggle.isOn = !tvmToggle.isOn;
    }

    public void SetPC() {
        usePC = pcToggle.isOn;
        tvmToggle.isOn = !pcToggle.isOn;
    }

    public void SetAudio(int state) {
        switch (state) {
            case 0: // No
                if (noAudioToggle.isOn) {
                    useAudio = false;
                    useSocketIOAudio = false;
                    useDashAudio = false;

                    socketAudioToggle.isOn = false;
                    dashAudioToggle.isOn = false;

                    kindAudio = 0;
                }
                break;
            case 1: // Socket
                if (socketAudioToggle.isOn) {
                    useAudio = true;
                    useSocketIOAudio = true;
                    useDashAudio = false;

                    noAudioToggle.isOn = false;
                    dashAudioToggle.isOn = false;

                    kindAudio = 1;
                }
                break;
            case 2: // Dash
                if (dashAudioToggle.isOn) {
                    useAudio = true;
                    useSocketIOAudio = false;
                    useDashAudio = true;

                    noAudioToggle.isOn = false;
                    socketAudioToggle.isOn = false;

                    kindAudio = 2;
                }
                break;
            default:
                break;
        }
    }
    #endregion


    public void OnGetAvailableDataStreams( ResponseStatus status, List<DataStream> dataStreams ) {

    }

    public void OnGetRegisteredDataStreams(ResponseStatus status, List<DataStream> dataStreams) {

    }

    public void OnMasterEventReceived(UserEvent pSceneEventData) {

    }

    public void OnUserEventReceived(UserEvent pSceneEventData) {

    }
       
}
