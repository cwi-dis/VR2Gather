using System;
using System.Collections;
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
    [Header("Connection and login components")]
    [SerializeField]
    public InputField orchestratorUrlIF;
    [SerializeField]
    public Button connectButton;
    [SerializeField]
    public Button disconnectButton;
    [SerializeField]
    public Toggle autoRetrieveOrchestratorDataOnConnect;
    [SerializeField]
    public InputField userNameIF;
    [SerializeField]
    public InputField userPasswordIF;
    [SerializeField]
    public InputField userMQurlIF;
    [SerializeField]
    public InputField userMQnameIF;
    [SerializeField]
    public Button loginButton;
    [SerializeField]
    public Button logoutButton;

    // Logs container
    [Header("Logs container")]
    [SerializeField]
    public RectTransform logsContainer;
    [SerializeField]
    public ScrollRect logsScrollRect;
    private Font ArialFont;

    // User GUI components
    [Header("User GUI components")]
    [SerializeField]
    public Text userLogged;
    [SerializeField]
    public Text userId;
    [SerializeField]
    public Text userName;
    [SerializeField]
    public Text userAdmin;
    [SerializeField]
    public Text userMQurl;
    [SerializeField]
    public Text userMQname;
    [SerializeField]
    public Text userSession;
    [SerializeField]
    public Text userScenario;
    [SerializeField]
    public Text userRoom;

    // Orchestrator GUI components
    [Header("Orchestrator GUI components")]
    [SerializeField]
    public Text orchestratorConnected;
    [SerializeField]
    public RectTransform orchestratorUsers;
    [SerializeField]
    public RectTransform orchestratorScenarios;
    [SerializeField]
    public RectTransform orchestratorSessions;

    #endregion

    #region Gui components panel to select the commands to send and their parameters

    // The list of available commands
    private List<Dropdown.OptionData> commandsListData = new List<Dropdown.OptionData>();

    [Header("Orchestrator GUI commands")]
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
    private RectTransform userDataMQnamePanel;
    [SerializeField]
    private RectTransform userDataMQurlPanel;
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

    // the wrapper for the orchestrator
    public OrchestratorWrapper orchestratorWrapper;

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

    [SerializeField]
    private OrchestrationTest test;

    public ScenarioInstance activeScenario;
    public Session activeSession;

    // auto retrieving data on login: is used on login to chain the commands that allow to get the items available for the user (list of sessions, users, scenarios)
    private bool isAutoRetrievingData = false;

    string userID = "";
    #endregion

    // Use this for initialization
    void Start()
    {

        // font to build gui components for logs!
        ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

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
        sendCommandButton.onClick.AddListener(delegate { selectedCommand.FunctionToCall(); });

        // update the states of the enabled or disabled items according to the connection and log states
        UpdateEnabledItems();
    }

    // Build the commands available
    private void BuildCommandsPanels()
    {
        GuiCommands = new List<GuiCommandDescription>
        {
            //Log
            new GuiCommandDescription("Login", new List<RectTransform> { userNamePanel, userPasswordPanel }, Login),
            new GuiCommandDescription("Logout", null, Logout),

            //Sessions
            new GuiCommandDescription("AddSession", new List<RectTransform> { scenarioIdPanel, sessionNamePanel, sessionDescriptionPanel }, AddSession),
            new GuiCommandDescription("GetSessions", null, GetSessions),
            new GuiCommandDescription("DeleteSession", new List<RectTransform> { sessionIdPanel }, DeleteSession),
            new GuiCommandDescription("JoinSession", new List<RectTransform> {  sessionIdPanel }, JoinSession),
            new GuiCommandDescription("LeaveSession", null, LeaveSession),

            //Scenarios
            new GuiCommandDescription("GetScenarios", null, GetScenarios),

            //Users
            new GuiCommandDescription("GetUsers", null, GetUsers),
            new GuiCommandDescription("GetUserInfo", new List<RectTransform> { userIdPanel }, GetUserInfo),
            new GuiCommandDescription("UpdateUserData", new List<RectTransform> { userDataMQnamePanel, userDataMQurlPanel }, UpdateUserData),
            new GuiCommandDescription("AddUser", new List<RectTransform> { userNamePanel, userPasswordPanel, sessionDescriptionPanel }, AddUser),
            new GuiCommandDescription("DeleteUser", new List<RectTransform> { userIdPanel }, DeleteUser),

            //Room
            new GuiCommandDescription("JoinRoom", new List<RectTransform> { roomIdPanel }, JoinRoom),
            new GuiCommandDescription("LeaveRoom", null, LeaveRoom),

            //Messages
            new GuiCommandDescription("SendMessage", new List<RectTransform> { messagePanel, userIdPanel }, SendMessage),
            new GuiCommandDescription("SendMessageToAll", new List<RectTransform> { messagePanel }, SendMessageToAll),
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
        orchestratorWrapper.Login(userNamePanel.GetComponentInChildren<InputField>().text, userPasswordPanel.GetComponentInChildren<InputField>().text);
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

    public void UpdateUserData()
    {
        orchestratorWrapper.UpdateUserDataJson(userDataMQnamePanel.GetComponentInChildren<InputField>().text, userDataMQurlPanel.GetComponentInChildren<InputField>().text, 
                                                userDataMQurlPanel.GetComponentInChildren<InputField>().text, userDataMQurlPanel.GetComponentInChildren<InputField>().text);
    }

    public void GetUserInfo()
    {
        Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        orchestratorWrapper.GetUserInfo(availableUsers[dd.value].userId);
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
        if ((selectedCommand != null) && (selectedCommand.VisibleEditionPanels != null) && (selectedCommand.VisibleEditionPanels.Count > 0))
        {
            selectedCommand.VisibleEditionPanels.ForEach(delegate (RectTransform panel)
            {
                panel.gameObject.SetActive(true);
            });
        }
    }

    #region listener for the messages sent and received (implementation of the IOrchestratorMessageListener interface)

    // Display the received message in the logs
    public void OnOrchestratorResponse(int status, string response)
    {
        AddTextComponentOnContent(logsContainer.transform, "<<< " + response);
        StartCoroutine(ScrollLogsToBottom());
    }

    // Display the sent message in the logs
    public void OnOrchestratorRequest(string request)
    {
        AddTextComponentOnContent(logsContainer.transform, ">>> " + request);
    }

    private IEnumerator ScrollLogsToBottom()
    {
        yield return new WaitForSeconds(0.2f);
        logsScrollRect.verticalScrollbar.value = 0;
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

        //TEST
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

        //TEST
        test.StatusTextUpdate();
    }

    public void OnLoginResponse(ResponseStatus status, string userId)
    {
        Debug.Log("OnLoginResponse()");
        bool userLoggedSucessfully = (status.Error == 0);

        if (!userIsLogged)
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

                //TEST
                //orchestratorWrapper.UpdateUserDataJson(userMQnameIF.text, userMQurlIF.text);
                orchestratorWrapper.UpdateUserDataJson(test.exchangeNameLoginIF.text, test.connectionURILoginIF.text, test.pcDashServerLoginIF.text, test.audioDashServerLoginIF.text);
                userID = userId;
            }
            else
            {
                userIsLogged = false;
                this.userId.text = "";
                userName.text = "";
                userAdmin.text = "";
                userID = "";
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
                userMQname.text = "";
                userMQurl.text = "";
                userID = "";
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
        Debug.Log("OnGetSessionsResponse:" + sessions.Count);

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

        if (isAutoRetrievingData)
        {
            // auto retriving phase: this was the last call
            isAutoRetrievingData = false;
        }

        //TEST
        test.SessionsUpdate();
        Debug.Log("OnGetSessionsResponse: Good");
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

            //TEST
            test.SessionsUpdate();
            activeSession = session;

            Debug.Log("OnAddSessionResponse: Good");
        }
        else
        {
            userSession.text = "";
            userScenario.text = "";
            Debug.Log("OnAddSessionResponse: Bad");
        }
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

            //TEST
            activeSession = session;
            Debug.Log("OnGetSessionInfoResponse: Good");
        }
        else
        {
            userSession.text = "";
            userScenario.text = "";
            Debug.Log("OnGetSessionInfoResponse: Bad");
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
            Debug.Log("OnJoinSessionResponse: Good");
        }
        else
        {
            userSession.text = "";
            Debug.Log("OnJoinSessionResponse: Bad");
        }
    }

    public void OnLeaveSessionResponse(ResponseStatus status)
    {
        if (status.Error == 0)
        {
            // success
            userSession.text = "";
            userScenario.text = "";

            //TEST
            activeSession = null;
            activeScenario = null;
            test.LobbyTextUpdate();
        }
    }

    public void OnGetScenariosResponse(ResponseStatus status, List<Scenario> scenarios)
    {
        Debug.Log("OnGetScenariosResponse:" + scenarios.Count);

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
            Debug.Log("ScenarioRooms:" + scenario.scenarioRooms.Count);
            scenario.scenarioRooms.ForEach(delegate (Room room)
            {
                Debug.Log("ScenarioRoom:" + room.GetGuiRepresentation());
            });
            options.Add(new Dropdown.OptionData(scenario.GetGuiRepresentation()));
        });
        dd.AddOptions(options);

        if (isAutoRetrievingData)
        {
            // auto retriving phase: call next
            orchestratorWrapper.GetSessions();
        }

        //TEST
        test.ScenariosUpdate();
    }

    public void OnGetScenarioInstanceInfoResponse(ResponseStatus status, ScenarioInstance scenario)
    {
        if (status.Error == 0)
        {            
            userScenario.text = scenario.GetGuiRepresentation();
            test.scenarioIdText.text = scenario.scenarioName;
            // now retrieve the list of the available rooms
            orchestratorWrapper.GetRooms();

            //TEST
            activeScenario = scenario;
            Debug.Log("OnGetScenarioInstanceInfoResponse: Good");
        }
        else
        {
            Debug.Log("OnGetScenarioInstanceInfoResponse: Bad");
        }
    }

    public void OnGetUsersResponse(ResponseStatus status, List<User> users)
    {
        Debug.Log("OnGetUsersResponse:" + users.Count);

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
        Debug.Log("OnGetUserInfoResponse()");

        if (status.Error == 0)
        {
            if (string.IsNullOrEmpty(userId.text) || user.userId == userId.text)
            {
                userId.text = user.userId;
                userName.text = user.userName;
                userAdmin.text = user.userAdmin.ToString();
                userMQname.text = user.userData.userMQexchangeName;
                userMQurl.text = user.userData.userMQurl;
            }

            if (isAutoRetrievingData)
            {
                // auto retriving phase: call next
                orchestratorWrapper.GetUsers();
            }

            //TEST
            test.exchangeNameIF.text = user.userData.userMQexchangeName;
            test.connectionURIIF.text = user.userData.userMQurl;
            test.pcDashServerIF.text = user.userData.userPCDash;
            test.audioDashServerIF.text = user.userData.userAudioDash;
            test.StatusTextUpdate();
        }
    }

    public void OnUpdateUserDataJsonResponse(ResponseStatus status)
    {
        Debug.Log("OnUpdateUserDataJsonResponse()");

        if (status.Error == 0)
        {
            orchestratorWrapper.GetUserInfo();
        }
    }

    public void OnDeleteUserResponse(ResponseStatus status)
    {
        Debug.Log("OnDeleteUserResponse()");

        // update the lists of user, anyway the result
        orchestratorWrapper.GetUsers();
    }

    public void OnGetRoomsResponse(ResponseStatus status, List<RoomInstance> rooms)
    {
        Debug.Log("OnGetRoomsResponse:" + rooms.Count);

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

        orchestratorWrapper.GetUsers();
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

    #region test methods
    // Connect to the orchestrator
    public void ConnectSocket()
    {
        orchestratorWrapper = new OrchestratorWrapper("https://vrt-orch-ms-vo.viaccess-orca.com/socket.io/", this, this);
        orchestratorWrapper.Connect();
    }

    // Login from the main buttons Login & Logout
    public void TestLogin(string user, string password)
    {
        orchestratorWrapper.Login(user, password);
    }

    public void TestAddSession(InputField name, InputField description, int scenario)
    {
        orchestratorWrapper.AddSession(availableScenarios[scenario].scenarioId,
            name.text, description.text);
    }

    public void TestJoinSession(int session)
    {
        string sessionIdToJoin = availableSessions[session].sessionId;
        //userSession.text = sessionIdToJoin;
        orchestratorWrapper.JoinSession(sessionIdToJoin);
    }

    public void TestDeleteSession(string sessionId)
    {
        orchestratorWrapper.DeleteSession(sessionId);
    }

    public void TestUpdateUserData(string name, string url, string pc, string audio) {
        orchestratorWrapper.UpdateUserDataJson(name, url, pc, audio);
    }

    public string TestGetUserID()
    {
        return userId.text;
    }

    public string TestGetUserName()
    {
        return userName.text;
    }
    #endregion
}