using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrchestratorWrapping;

public delegate void FunctionToCallOnSendCommandButton();

class GuiCommandDescription
{
    public string CommandName;
    public List<RectTransform> VisibleEditionPanels;
    public FunctionToCallOnSendCommandButton FunctionToCall;

    public GuiCommandDescription(string commandName, List<RectTransform> visibleEditionPanels, FunctionToCallOnSendCommandButton functionToCall)
    {
        CommandName = commandName;
        VisibleEditionPanels = visibleEditionPanels;
        FunctionToCall = functionToCall;
    }
}

/**
 * Main Gui class
 * **/
public class OrchestratorGui : MonoBehaviour, IOrchestratorResponsesListener, IOrchestratorMessageListener
{
    #region gui components

    //Connection and login components
    [SerializeField]
    private InputField orchestratorUrlIF;
    [SerializeField]
    private Button connectButton;
    [SerializeField]
    private Button disconnectButton;
    [SerializeField]
    private Toggle autoRetrieveOrchestratorDataOnConnect;
    [SerializeField]
    private InputField userNameIF;
    [SerializeField]
    private InputField userPasswordIF;
    [SerializeField]
    private Button loginButton;
    [SerializeField]
    private Button logoutButton;

    // Logs container
    [SerializeField]
    private RectTransform logsContainer;
    private Font ArialFont;

    // user GUI components
    [SerializeField]
    private Text userLogged;
    [SerializeField]
    private Text userId;
    [SerializeField]
    private Text userName;
    [SerializeField]
    private Text userAdmin;
    [SerializeField]
    private Text userSession;
    [SerializeField]
    private Text userScenario;
    [SerializeField]
    private Text userRoom;

    // orchestrator GUI components
    [SerializeField]
    private Text orchestratorConnected;
    [SerializeField]
    private RectTransform orchestratorUsers;
    [SerializeField]
    private RectTransform orchestratorScenarios;
    [SerializeField]
    private RectTransform orchestratorSessions;

    #endregion

    #region Gui components panel to select the commands to send and their parameters
    
    // The list of available commands
    private List<Dropdown.OptionData> commandsListData = new List<Dropdown.OptionData>();

    // dropdown to display the list of availbale commands
    [SerializeField]
    private Dropdown commandDropdown;

    // button to send the command
    [SerializeField]
    private Button sendCommandButton;

    // container that displays the list of parameters for the selected command
    [SerializeField]
    private RectTransform paramsContainer;
    
    // parameters panels
    [SerializeField]
    private RectTransform userIdPanel;
    [SerializeField]
    private RectTransform userAdminPanel;
    [SerializeField]
    private RectTransform userNamePanel;
    [SerializeField]
    private RectTransform userPasswordPanel;
    [SerializeField]
    private RectTransform sessionIdPanel;
    [SerializeField]
    private RectTransform sessionNamePanel;
    [SerializeField]
    private RectTransform sessionDescriptionPanel;
    [SerializeField]
    private RectTransform scenarioIdPanel;
    [SerializeField]
    private RectTransform roomIdPanel;
    [SerializeField]
    private RectTransform messagePanel;

    #endregion

    #region orchestration logics
    [SerializeField]
    private OrchestrationTest test;

    // the wrapper for the orchestrator
    private OrchestratorWrapper orchestratorWrapper;

    // available commands
    private List<GuiCommandDescription> GuiCommands;

    // selected commands
    private GuiCommandDescription selectedCommand;

    // lists of items that are availble for the user
    public List<Session> availableSessions;
    public List<Scenario> availableScenarios;
    public List<User> availableUsers;
    public List<RoomInstance> availableRoomInstances;

    // user Login state
    public bool userIsLogged = false;

    // orchestrator connection state
    public bool connectedToOrchestrator = false;

    public ScenarioInstance activeScenario;
    public Session activeSession;

    // auto retrieving data on login: is used on login to chain the commands that allow to get the items available for the user (list of sessions, users, scenarios)
    public bool isAutoRetrievingData = false;

    #endregion

    // Use this for initialization
    void Start () {

        // font to build gui components for logs!
        ArialFont = (Font) Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

        // buttons listeners
        connectButton.onClick.AddListener(delegate { socketConnect(); });
        disconnectButton.onClick.AddListener(delegate { socketDisconnect(); });
        loginButton.onClick.AddListener(delegate { HeadLogin(); });
        logoutButton.onClick.AddListener(delegate { Logout(); });

        // build the commands available at the GUI level
        BuildCommandsPanels();

        //// Add the commands to the options data : will be used to build the dropdown
        GuiCommands.ForEach(delegate (GuiCommandDescription commandDescription)
        {
            commandsListData.Add(new Dropdown.OptionData(commandDescription.CommandName));
        });

        // Remove all the parameters panels from the parameters container
        RemoveAllParamsPanels();

        // clear the dropdown then add the commands
        commandDropdown.ClearOptions();
        commandDropdown.AddOptions(commandsListData);
        commandDropdown.onValueChanged.AddListener(delegate { SelectCommand(commandDropdown.value); });
        SelectCommand(commandDropdown.value); //init first command

        // Add listener on the send button (call the function related to the selected command)
        sendCommandButton.onClick.AddListener(delegate { selectedCommand.FunctionToCall();});

        // update the states of the enabled or disabled items according to the connection and log states
        UpdateEnabledItems();
    }

    // Build the commands available
    // TODO : add the messages commands
    private void BuildCommandsPanels()
    {
        GuiCommands = new List<GuiCommandDescription>
        {
            new GuiCommandDescription("Login", new List<RectTransform> { userNamePanel, userPasswordPanel }, Login),
            new GuiCommandDescription("Logout", null, Logout),

            new GuiCommandDescription("AddSession", new List<RectTransform> { scenarioIdPanel, sessionNamePanel, sessionDescriptionPanel }, AddSession),
            new GuiCommandDescription("GetSessions", null, GetSessions),
            //new GuiCommandDescription("GetSessionInfo", null, GetSessionInfo),
            new GuiCommandDescription("DeleteSession", new List<RectTransform> { sessionIdPanel }, DeleteSession),

            new GuiCommandDescription("JoinSession", new List<RectTransform> {  sessionIdPanel }, JoinSession),
            new GuiCommandDescription("LeaveSession", null, LeaveSession),

            new GuiCommandDescription("GetScenarios", null, GetScenarios),
            //new GuiCommandDescription("GetScenarioInfo", new List<RectTransform> { scenarioIdPanel}, GetScenarioInfo),

            new GuiCommandDescription("GetUsers", null, GetUsers),
            //new GuiCommandDescription("GetUserInfo", null, GetUserInfo),
            new GuiCommandDescription("AddUser", new List<RectTransform> { userNamePanel, userPasswordPanel, sessionDescriptionPanel }, AddUser),
            new GuiCommandDescription("AddSession", new List<RectTransform> { scenarioIdPanel, sessionNamePanel, userAdminPanel }, AddSession),
            new GuiCommandDescription("DeleteUser", new List<RectTransform> { userIdPanel }, DeleteUser),

            //new GuiCommandDescription("GetRooms", null, GetRooms),
            //new GuiCommandDescription("GetRoomInfo", null, GetRoomInfo),

            new GuiCommandDescription("JoinRoom", new List<RectTransform> { roomIdPanel }, JoinRoom),
            new GuiCommandDescription("LeaveRoom", null, LeaveRoom),

                //// NOTE: not to be done here: those messages are initiated by the orchestrator
                //// new OrchestratorCommand("UpdateSession", "UpdateSession", null),
                //// new OrchestratorCommand("SessionClosed", "SessionClosed", null),

            //messages
            new GuiCommandDescription("SendMessage", new List<RectTransform> { messagePanel, userIdPanel }, SendMessage),
            new GuiCommandDescription("SendMessageToAll", new List<RectTransform> { messagePanel }, SendMessageToAll),
                //new OrchestratorCommand("MessageSent", "MessageSent", null)
        };
    }

    // Disconnect from the orchestrator
    private void socketDisconnect()
    {
        orchestratorWrapper.Disconnect();
    }

    // Connect to the orchestrator
    private void socketConnect()
    {
        orchestratorWrapper = new OrchestratorWrapper(orchestratorUrlIF.text, this, this);
        orchestratorWrapper.Connect();
    }

    // Login from the main buttons Login & Logout
    private void HeadLogin()
    {
        orchestratorWrapper.Login(userNameIF.text, userPasswordIF.text);
    }

    #region functions that prepare the command to send
    public void Login()
    {
        orchestratorWrapper.Login(userNamePanel.GetComponentInChildren<InputField>().text,
            userPasswordPanel.GetComponentInChildren<InputField>().text);
    }

    public void Logout()
    {
        orchestratorWrapper.Logout();
    }

    public void GetSessions()
    {
        orchestratorWrapper.GetSessions();
    }

    public void AddSession()
    {
        Dropdown dd = scenarioIdPanel.GetComponentInChildren<Dropdown>();
        orchestratorWrapper.AddSession(availableScenarios[dd.value].scenarioId, 
            sessionNamePanel.GetComponentInChildren<InputField>().text,
            sessionDescriptionPanel.GetComponentInChildren<InputField>().text);
    }

    public void DeleteSession()
    {
        Dropdown dd = sessionIdPanel.GetComponentInChildren<Dropdown>();
        orchestratorWrapper.DeleteSession(availableSessions[dd.value].sessionId);
    }

    public void JoinSession()
    {
        Dropdown dd = sessionIdPanel.GetComponentInChildren<Dropdown>();
        string sessionIdToJoin = availableSessions[dd.value].sessionId;
        userSession.text = sessionIdToJoin;
        orchestratorWrapper.JoinSession(sessionIdToJoin);
    }

    public void LeaveSession()
    {
        orchestratorWrapper.LeaveSession();
    }

    public void GetScenarios()
    {
        orchestratorWrapper.GetScenarios();
    }

    public void GetUsers()
    {
        orchestratorWrapper.GetUsers();
    }

    public void AddUser()
    {
        orchestratorWrapper.AddUser(userNamePanel.GetComponentInChildren<InputField>().text,
            userPasswordPanel.GetComponentInChildren<InputField>().text,
            userAdminPanel.GetComponentInChildren<Toggle>().isOn);
    }

    public void DeleteUser()
    {
        Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        orchestratorWrapper.DeleteUser(availableUsers[dd.value].userId);
    }

    public void GetRooms()
    {
        orchestratorWrapper.GetRooms();
    }

    public void JoinRoom()
    {
        Dropdown dd = roomIdPanel.GetComponentInChildren<Dropdown>();
        RoomInstance room = availableRoomInstances[dd.value];
        userRoom.text = room.GetGuiRepresentation();
        orchestratorWrapper.JoinRoom(room.roomId);
    }

    public void LeaveRoom()
    {
        orchestratorWrapper.LeaveRoom();
    }

    public void SendMessage()
    {
        Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        orchestratorWrapper.SendMessage(messagePanel.GetComponentInChildren<InputField>().text,
            availableUsers[dd.value].userId);
    }

    public void SendMessageToAll()
    {
        orchestratorWrapper.SendMessageToAll(messagePanel.GetComponentInChildren<InputField>().text);
    }

    #endregion

    // update connect and login buttons according to the states
    private void UpdateEnabledItems()
    {
        connectButton.interactable = !connectedToOrchestrator;
        disconnectButton.interactable = connectedToOrchestrator;
        loginButton.interactable = connectedToOrchestrator && (!userIsLogged);
        logoutButton.interactable = connectedToOrchestrator && userIsLogged;
        commandDropdown.interactable = connectedToOrchestrator;
        sendCommandButton.interactable = connectedToOrchestrator;
    }

    // Select a command
    private void SelectCommand(int value)
    {
        RemoveAllParamsPanels();
        selectedCommand = GuiCommands[value];
        UpdateParamsPanels(selectedCommand);
    }

    // remove all parameters panel from the view (before to fill it with the good ones according to the selected command)
    private void RemoveAllParamsPanels()
    {
        for (var i = paramsContainer.transform.childCount - 1; i >= 0; i--)
        {
            RectTransform objectA = (RectTransform)paramsContainer.transform.GetChild(i);
            objectA.gameObject.SetActive(false);
        }
    }

    // update the params panel acoording to the selected GuiCommand
    private void UpdateParamsPanels(GuiCommandDescription selectedCommand)
    {
        if ((selectedCommand != null) &&(selectedCommand.VisibleEditionPanels != null) && (selectedCommand.VisibleEditionPanels.Count > 0))
        {
            selectedCommand.VisibleEditionPanels.ForEach(delegate (RectTransform panel)
            {
                panel.gameObject.SetActive(true);
            });
        }
    }

    // Update is called once per frame
    void Update () {
		
	}

    #region listener for the messages sent and received (implementation of the IOrchestratorMessageListener interface)

    // Display the received message in the logs
    public void OnOrchestratorResponse(int status, string response)
    {
        AddTextComponentOnContent(logsContainer.transform, ">>> " + response);
    }


    // Display the sent message in the logs
    public void OnOrchestratorRequest(string request)
    {
        AddTextComponentOnContent(logsContainer.transform, "<<< " + request);
    }

    #endregion

    // Fill a scroll view with a text item
    public void AddTextComponentOnContent(Transform container, string value)
    {
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
    public void removeComponentsFromList(Transform container)
    {
        for (var i = container.childCount - 1; i >= 0; i--)
        {
            var obj = container.GetChild(i);
            obj.transform.SetParent(null);
            Destroy(obj.gameObject);
        }
    }

    #region callbacks for the commands (implementation of the IOrchestratorResponsesListener interface)

    // implementation des callbacks de retour de l'interface
    public void OnConnect()
    {
        connectedToOrchestrator = true;
        orchestratorConnected.text = connectedToOrchestrator.ToString();
        UpdateEnabledItems();
        //Test
        test.StatusTextUpdate();
    }

    public void OnDisconnect()
    {
        connectedToOrchestrator = false;
        userIsLogged = false;
        this.userId.text = "";
        userName.text = "";
        userAdmin.text = "";
        orchestratorConnected.text = connectedToOrchestrator.ToString();
        UpdateEnabledItems();
        //Test
        test.StatusTextUpdate();
    }

    public void OnLoginResponse(ResponseStatus status, string userId)
    {
        Debug.Log("OnLoginResponse:");
        bool userLoggedSucessfully = (status.Error == 0);

        if (! userIsLogged)
        {
            //user was not logged before request
            if (userLoggedSucessfully)
            {
                userIsLogged = true;
                if (autoRetrieveOrchestratorDataOnConnect.isOn)
                {
                    // tag to warn other callbacks that we are auto retrieving data, each call back will call the next one
                    // to get a full state of the orchestrator
                    isAutoRetrievingData = true;
                }
                else
                {
                    isAutoRetrievingData = false;
                }
                Debug.Log("isAutoRetrievingData:" + isAutoRetrievingData);
                bool result = orchestratorWrapper.GetUserInfo();
                Debug.Log(" CALL:" + result);
            }
            else
            {
                userIsLogged = false;
                this.userId.text = "";
                userName.text = "";
                userAdmin.text = "";
            }
        }
        else
        {
            //user was logged before previously
            if (! userLoggedSucessfully)
            {
                // normal, user previopusly logged, nothing to do
            }
            else
            {
                // should not occur
            }
        }
        userLogged.text = userIsLogged.ToString();
        UpdateEnabledItems();
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
                //normal
                userIsLogged = false;
                userLogged.text = false.ToString();
                this.userId.text = "";
                userName.text = "";
                userAdmin.text = "";
            }
            else
            {
                // problem while logout
                userIsLogged = true;
            }
        }
        UpdateEnabledItems();
    }

    public void OnGetSessionsResponse(ResponseStatus status, List<Session> sessions)
    {
        Debug.Log(" OnGetSessionsResponse:" + sessions.Count);

        // update the list of available sessions
        availableSessions = sessions;
        removeComponentsFromList(orchestratorSessions.transform);
        sessions.ForEach(delegate (Session element)
        {
            AddTextComponentOnContent(orchestratorSessions.transform, element.GetGuiRepresentation());
        });

        // update the dropdown
        Dropdown dd = sessionIdPanel.GetComponentInChildren<Dropdown>();
        dd.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        sessions.ForEach(delegate (Session session)
        {
            Debug.Log("Session:" + session.GetGuiRepresentation());
            Debug.Log("users:" + session.sessionUsers.Length);
            Array.ForEach<string>(session.sessionUsers, delegate (string user)
            {
                Debug.Log("userId:" + user);
            });
            options.Add(new Dropdown.OptionData(session.GetGuiRepresentation()));
        });
        dd.AddOptions(options);
        //TEST
        test.SessionsUpdate();

        if (isAutoRetrievingData)
        {
            // auto retriving phase: this was the last call
            isAutoRetrievingData = false;
        }
    }

    public void OnAddSessionResponse(ResponseStatus status, Session session)
    {
        // Is equal to AddSession + Join Session, except that session is returned (not on JoinSession)
        if (status.Error == 0)
        {
            // success

            availableSessions.Add(session);
            // update the list of available sessions
            removeComponentsFromList(orchestratorSessions.transform);
            availableSessions.ForEach(delegate (Session element)
            {
                AddTextComponentOnContent(orchestratorSessions.transform, element.GetGuiRepresentation());
            });
            //TEST
            test.SessionsUpdate();

            // update the dropdown
            Dropdown dd = sessionIdPanel.GetComponentInChildren<Dropdown>();
            dd.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            availableSessions.ForEach(delegate (Session sess)
            {
                options.Add(new Dropdown.OptionData(sess.GetGuiRepresentation()));
            });
            dd.AddOptions(options);

            userSession.text = session.GetGuiRepresentation();
            // Now retrieve the infos about the scenario instance
            userScenario.text = session.scenarioId;
            orchestratorWrapper.GetScenarioInstanceInfo(session.scenarioId);
            //OnJoinSessionResponse(status);
            activeSession = session;
        }
        else
        {
            userSession.text = "";
            userScenario.text = "";
        }

        //////XXX NOT 2 REQUESTS ON THE SAME THREADS
        //// update the list of available sessions
        //orchestratorWrapper.GetSessions();
    }

    public void OnGetSessionInfoResponse(ResponseStatus status, Session session)
    {
        if (status.Error == 0)
        {
            // success
            userSession.text = session.GetGuiRepresentation();
            userScenario.text = session.scenarioId;
            // now retrieve the secnario instance infos
            orchestratorWrapper.GetScenarioInstanceInfo(session.scenarioId);

            activeSession = session;
        }
        else
        {
            userSession.text = "";
            userScenario.text = "";
        }
    }

    public void OnDeleteSessionResponse(ResponseStatus status)
    {
        // update the lists of session, anyway the result
        orchestratorWrapper.GetSessions();
    }

    public void OnJoinSessionResponse(ResponseStatus status)
    {
        if (status.Error == 0)
        {
            // now we wwill need the session info with the sceanrio instance used for this session
            orchestratorWrapper.GetSessionInfo();
        }
        else
        {
            userSession.text = "";
        }
    }

    public void OnLeaveSessionResponse(ResponseStatus status)
    {
        if (status.Error == 0)
        {
            // success
            userSession.text = "";
            userScenario.text = "";


            activeSession = null;
            activeScenario = null;
            test.LobbyTextUpdate();
        }
    }

    public void OnGetScenariosResponse(ResponseStatus status, List<Scenario> scenarios)
    {
        Debug.Log(" OnGetScenariosResponse:" + scenarios.Count);

        // update the list of available scenarios
        availableScenarios = scenarios;
        removeComponentsFromList(orchestratorScenarios.transform);
        scenarios.ForEach(delegate (Scenario element)
        {
            AddTextComponentOnContent(orchestratorScenarios.transform, element.GetGuiRepresentation());
        });

        //update the data in the dropdown
        Dropdown dd = scenarioIdPanel.GetComponentInChildren<Dropdown>();
        dd.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        scenarios.ForEach(delegate (Scenario scenario)
        {
            Debug.Log("Scenario:" + scenario.GetGuiRepresentation());
            //Debug.Log("ScenarioRooms:" + scenario.scenarioRooms.Count);
            //scenario.scenarioRooms.ForEach(delegate (Room room)
            //{
            //    Debug.Log("ScenarioRoom:" + room.GetGuiRepresentation());
            //});
            options.Add(new Dropdown.OptionData(scenario.GetGuiRepresentation()));
        });
        dd.AddOptions(options);
        //TEST
        test.ScenariosUpdate();

        if (isAutoRetrievingData)
        {
            // auto retriving phase: call next
            orchestratorWrapper.GetSessions();
        }
    }

    public void OnGetScenarioInstanceInfoResponse(ResponseStatus status, ScenarioInstance scenario)
    {
        if (status.Error == 0)
        {
            userScenario.text = scenario.GetGuiRepresentation();
            // now retrieve the list of the available rooms
            orchestratorWrapper.GetRooms();
            activeScenario = scenario;
            //test.LobbyTextUpdate();
        }
    }

    public void OnGetUsersResponse(ResponseStatus status, List<User> users)
    {
        Debug.Log(" OnGetUsersResponse:" + users.Count);

        // update the list of available users
        availableUsers = users;
        removeComponentsFromList(orchestratorUsers.transform);
        users.ForEach(delegate (User element)
        {
            AddTextComponentOnContent(orchestratorUsers.transform, element.GetGuiRepresentation());
        });

        //update the data in the dropdown
        Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        dd.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        users.ForEach(delegate (User user)
        {
            Debug.Log("User:" + user.GetGuiRepresentation());
            options.Add(new Dropdown.OptionData(user.GetGuiRepresentation()));
        });
        dd.AddOptions(options);

        if (isAutoRetrievingData)
        {
            // auto retriving phase: call next
            orchestratorWrapper.GetScenarios();
        }
    }

    public void OnAddUserResponse(ResponseStatus status, User user)
    {
        // update the lists of user, anyway the result
        orchestratorWrapper.GetUsers();
    }

    public void OnGetUserInfoResponse(ResponseStatus status, User user)
    {
        Debug.Log(" OnGetUserInfoResponse()");
        if (status.Error == 0)
        {
            userId.text = user.userId;
            userName.text = user.userName;
            userAdmin.text = user.userAdmin.ToString();

            Debug.Log("isAutoRetrievingData:" + isAutoRetrievingData);
            if (isAutoRetrievingData)
            {
                Debug.Log("CALL USERS:" + isAutoRetrievingData);
                // auto retriving phase: call next
                orchestratorWrapper.GetUsers();
            }
        }
    }

    public void OnDeleteUserResponse(ResponseStatus status)
    {
        // update the lists of user, anyway the result
        orchestratorWrapper.GetUsers();
    }

    public void OnGetRoomsResponse(ResponseStatus status, List<RoomInstance> rooms)
    {
        Debug.Log(" OnGetRoomsResponse:" + rooms.Count);

        // update the list of available rooms
        availableRoomInstances = rooms;
        //updateListComponent(orchestratorUserSessions.transform, Orchestrator.orchestrator.orchestratorUserSessions);

        //update the data in the dropdown
        Dropdown dd = roomIdPanel.GetComponentInChildren<Dropdown>();
        dd.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        rooms.ForEach(delegate (RoomInstance room)
        {
            Debug.Log("Room:" + room.GetGuiRepresentation());
            options.Add(new Dropdown.OptionData(room.GetGuiRepresentation()));
        });
        dd.AddOptions(options);
    }

    public void OnJoinRoomResponse(ResponseStatus status)
    {
        if (status.Error != 0)
        {
            userRoom.text = "";
        }
    }

    public void OnLeaveRoomResponse(ResponseStatus status)
    {
        if (status.Error == 0)
        {
            userRoom.text = "";
        }
    }

    public void OnSendMessageResponse(ResponseStatus status)
    {
        // nothing to do
    }

    public void OnSendMessageToAllResponse(ResponseStatus status)
    {
        // nothing to do
    }

    #endregion

    //public void LogError(string message)
    //{
    //    Debug.LogError(message);
    //}

    #region test methods
    // Connect to the orchestrator
    public void ConnectSocket() {
        orchestratorWrapper = new OrchestratorWrapper("https://vrt-orch-ms-vo.viaccess-orca.com/socket.io/", this, this);
        orchestratorWrapper.Connect();
    }

    // Login from the main buttons Login & Logout
    public void TestLogin(string user, string password) {
        orchestratorWrapper.Login(user, password);
    }

    public void TestAddSession(InputField name, InputField description, int scenario) {
        orchestratorWrapper.AddSession(availableScenarios[scenario].scenarioId,
            name.text, description.text);
    }

    public void TestJoinSession(int session) {
        string sessionIdToJoin = availableSessions[session].sessionId;
        //userSession.text = sessionIdToJoin;
        orchestratorWrapper.JoinSession(sessionIdToJoin);
    }

    public void TestDeleteSession(string sessionId) {
        orchestratorWrapper.DeleteSession(sessionId);
    }

    #endregion
}
