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
public class OrchestratorGui : MonoBehaviour
{
    #region GUI components

    //Connection and login components
    [Header("Connection and login components")]
    [SerializeField]
    private InputField orchestratorUrlIF = null;
    [SerializeField]
    private Button connectButton = null;
    [SerializeField]
    private Button disconnectButton = null;
    [SerializeField]
    private Toggle autoRetrieveOrchestratorDataOnConnect = null;
    [SerializeField]
    private InputField userNameIF = null;
    [SerializeField]
    private InputField userPasswordIF = null;
    /*
    [SerializeField]
    private InputField userMQurlIF = null;
    [SerializeField]
    private InputField userMQnameIF = null;
    */
    [SerializeField]
    private Button loginButton = null;
    [SerializeField]
    private Button logoutButton = null;

    // Logs container
    [Header("Logs container")]
    [SerializeField]
    private RectTransform logsContainer = null;
    [SerializeField]
    private ScrollRect logsScrollRect = null;
    [SerializeField]
    private Button logsClearBtn = null;
    private Font ArialFont;

    // User GUI components
    [Header("User GUI components")]
    [SerializeField]
    private Text userLogged = null;
    [SerializeField]
    private Text userId = null;
    [SerializeField]
    private Text userName = null;
    [SerializeField]
    private Text userAdmin = null;
    [SerializeField]
    private Text userMaster = null;
    [SerializeField]
    private Text userMQurl = null;
    [SerializeField]
    private Text userMQname = null;
    [SerializeField]
    private Text userRepresentation = null;
    [SerializeField]
    private Text userSession = null;
    [SerializeField]
    private Text userSessionUsers = null;
    [SerializeField]
    private Text userScenario = null;
    [SerializeField]
    private Text userLiveURL = null;
    [SerializeField]
    private Text userVODLiveURL = null;
    [SerializeField]
    private Text userRoom = null;

    // Orchestrator GUI components
    [Header("Orchestrator GUI components")]
    [SerializeField]
    private Text orchestratorConnected = null;
    [SerializeField]
    private Text orchestratorVersion = null;
    [SerializeField]
    private RectTransform orchestratorUsers = null;
    [SerializeField]
    private RectTransform orchestratorScenarios = null;
    [SerializeField]
    private RectTransform orchestratorSessions = null;

    #endregion

    #region GUI components panel to select the commands to send and their parameters

    // The list of available commands
    private List<Dropdown.OptionData> commandsListData = new List<Dropdown.OptionData>();

    [Header("Orchestrator GUI commands")]
    // dropdown to display the list of availbale commands
    [SerializeField]
    private Dropdown commandDropdown = null;

    // button to send the command
    [SerializeField]
    private Button sendCommandButton = null;

    // container that displays the list of parameters for the selected command
    [SerializeField]
    private RectTransform paramsContainer = null;

    // parameters panels
    [SerializeField]
    private RectTransform userIdPanel = null;
    [SerializeField]
    private RectTransform userAdminPanel = null;
    [SerializeField]
    private RectTransform userNamePanel = null;
    [SerializeField]
    private RectTransform userPasswordPanel = null;
    [SerializeField]
    private RectTransform userDataMQnamePanel = null;
    [SerializeField]
    private RectTransform userDataMQurlPanel = null;
    [SerializeField]
    private RectTransform userDataRepresentationTypePanel = null;
    [SerializeField]
    private Dropdown userDataRepresentationTypeDD = null;
    [SerializeField]
    private RectTransform sessionIdPanel = null;
    [SerializeField]
    private RectTransform sessionNamePanel = null;
    [SerializeField]
    private RectTransform sessionDescriptionPanel = null;
    [SerializeField]
    private RectTransform scenarioIdPanel = null;
    [SerializeField]
    private RectTransform roomIdPanel = null;
    [SerializeField]
    private RectTransform messagePanel = null;
    [SerializeField]
    private RectTransform eventPanel = null;

    #endregion

    #region GUI logics

    // available commands
    private List<GuiCommandDescription> GuiCommands;

    // selected commands
    private GuiCommandDescription selectedCommand;

    #endregion

    #region Unity

    void Start()
    {
        // font to build gui components for logs!
        ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

        // buttons listeners
        connectButton.onClick.AddListener(delegate { SocketConnect(); });
        disconnectButton.onClick.AddListener(delegate { socketDisconnect(); });
        loginButton.onClick.AddListener(delegate { HeadLogin(); });
        logoutButton.onClick.AddListener(delegate { Logout(); });
        logsClearBtn.onClick.AddListener(delegate { ClearLogsGUI(); });

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

        // Fill UserData representation dropdown according to eUserRepresentationType enum declaration
        userDataRepresentationTypeDD.ClearOptions();
        userDataRepresentationTypeDD.AddOptions(new List<string>(Enum.GetNames(typeof(UserData.eUserRepresentationType))));

        // Add listener on the send button (call the function related to the selected command)
        sendCommandButton.onClick.AddListener(delegate { selectedCommand.FunctionToCall(); });

        // update the states of the enabled or disabled items according to the connection and log states
        UpdateEnabledItems();

        InitialiseControllerEvents();
    }

    #endregion

    #region GUI

    // Build the commands available
    private void BuildCommandsPanels()
    {
        GuiCommands = new List<GuiCommandDescription>
        {
            //Log
            new GuiCommandDescription("SignIn", new List<RectTransform> { userNamePanel, userPasswordPanel }, SignIn),
            new GuiCommandDescription("Login", new List<RectTransform> { userNamePanel, userPasswordPanel }, Login),
            new GuiCommandDescription("Logout", null, Logout),

            //NTP
            new GuiCommandDescription("GetNTPTime", null, GetNTPTime),

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
            new GuiCommandDescription("UpdateUserData", new List<RectTransform> { userDataMQnamePanel, userDataMQurlPanel, userDataRepresentationTypePanel }, UpdateUserData),
            new GuiCommandDescription("ClearUserData", null, ClearUserData),
            new GuiCommandDescription("AddUser", new List<RectTransform> { userNamePanel, userPasswordPanel, userAdminPanel }, AddUser),
            new GuiCommandDescription("DeleteUser", new List<RectTransform> { userIdPanel }, DeleteUser),

            //Room
            new GuiCommandDescription("JoinRoom", new List<RectTransform> { roomIdPanel }, JoinRoom),
            new GuiCommandDescription("LeaveRoom", null, LeaveRoom),

            //Messages
            new GuiCommandDescription("SendMessage", new List<RectTransform> { messagePanel, userIdPanel }, SendMessage),
            new GuiCommandDescription("SendMessageToAll", new List<RectTransform> { messagePanel }, SendMessageToAll),

            //User Events
            new GuiCommandDescription("SendEventToMaster", new List<RectTransform> { eventPanel }, SendEventToMaster),
            new GuiCommandDescription("SendEventToUser", new List<RectTransform> { eventPanel, userIdPanel }, SendEventToUser),
            new GuiCommandDescription("SendEventToAll", new List<RectTransform> { eventPanel }, SendEventToAll),

            //Data Stream
            new GuiCommandDescription("GetAvailableDataStreams", new List<RectTransform> { userIdPanel }, GetAvailableDataStreams),
            new GuiCommandDescription("GetRegisteredDataStreams", null, GetRegisteredDataStreams)
        };
    }

    // update connect and login buttons according to the states
    private void UpdateEnabledItems()
    {
        connectButton.interactable = !OrchestratorController.Instance.ConnectedToOrchestrator;
        disconnectButton.interactable = OrchestratorController.Instance.ConnectedToOrchestrator;
        loginButton.interactable = OrchestratorController.Instance.ConnectedToOrchestrator && (!OrchestratorController.Instance.UserIsLogged);
        logoutButton.interactable = OrchestratorController.Instance.ConnectedToOrchestrator && OrchestratorController.Instance.UserIsLogged;
        commandDropdown.interactable = OrchestratorController.Instance.ConnectedToOrchestrator;
        sendCommandButton.interactable = OrchestratorController.Instance.ConnectedToOrchestrator;
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

    // update the list of connected users in a session to display
    private void UpdateConnectedUsersGUI()
    {
        if (OrchestratorController.Instance.ConnectedUsers == null)
        {
            userSessionUsers.text = "None";
            return;
        }

        userSessionUsers.text = "";

        switch(OrchestratorController.Instance.ConnectedUsers.Length)
        {
            case 0:
                userSessionUsers.text = "None";
                break;
            case 1:
                userSessionUsers.text = "Me; " + OrchestratorController.Instance.ConnectedUsers[0].userName;
                break;
            default:
                userSessionUsers.text = "Me; ";
                foreach (User u in OrchestratorController.Instance.ConnectedUsers)
                {
                    userSessionUsers.text += u.userName + "; ";
                }
                break;
        }
    }

    private IEnumerator ScrollLogsToBottom()
    {
        yield return new WaitForSeconds(0.2f);
        logsScrollRect.verticalScrollbar.value = 0;
    }

    private void ClearLogsGUI()
    {
        foreach(Transform child in logsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    // Fill a scroll view with a text item
    private void AddTextComponentOnContent(Transform container, string value)
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
    private void RemoveComponentsFromList(Transform container)
    {
        for (var i = container.childCount - 1; i >= 0; i--)
        {
            var obj = container.GetChild(i);
            obj.transform.SetParent(null);
            Destroy(obj.gameObject);
        }
    }

    #endregion

    #region Events listeners
    
    // Subscribe to Orchestrator Wrapper Events
    private void InitialiseControllerEvents()
    {
        OrchestratorController.Instance.OnConnectionEvent += OnConnect;
        OrchestratorController.Instance.OnConnectionEvent += OnDisconnect;
        OrchestratorController.Instance.OnOrchestratorRequestEvent += OnOrchestratorRequest;
        OrchestratorController.Instance.OnOrchestratorResponseEvent += OnOrchestratorResponse;
        OrchestratorController.Instance.OnGetOrchestratorVersionEvent += OnGetOrchestratorVersionHandler;
        OrchestratorController.Instance.OnSignInEvent += OnSignIn;
        OrchestratorController.Instance.OnLoginEvent += OnLogin;
        OrchestratorController.Instance.OnLogoutEvent += OnLogout;
        OrchestratorController.Instance.OnGetSessionsEvent += OnGetSessionsHandler;
        OrchestratorController.Instance.OnAddSessionEvent += OnAddSessionHandler;
        OrchestratorController.Instance.OnJoinSessionEvent += OnJoinSessionHandler;
        OrchestratorController.Instance.OnLeaveSessionEvent += OnLeaveSessionHandler;
        OrchestratorController.Instance.OnDeleteSessionEvent += OnDeleteSessionHandler;
        OrchestratorController.Instance.OnUserJoinSessionEvent += OnUserJoinedSessionHandler;
        OrchestratorController.Instance.OnUserLeaveSessionEvent += OnUserLeftSessionHandler;
        OrchestratorController.Instance.OnGetScenarioEvent += OnGetScenarioInstanceInfoHandler;
        OrchestratorController.Instance.OnGetScenariosEvent += OnGetScenariosHandler;
        OrchestratorController.Instance.OnGetLiveDataEvent += OnGetLivePresenterDataHandler;
        OrchestratorController.Instance.OnGetUsersEvent += OnGetUsersHandler;
        OrchestratorController.Instance.OnAddUserEvent += OnAddUserHandler;
        OrchestratorController.Instance.OnGetUserInfoEvent += OnGetUserInfoHandler;
        OrchestratorController.Instance.OnGetRoomsEvent += OnGetRoomsHandler;
        OrchestratorController.Instance.OnJoinRoomEvent += OnJoinRoomHandler;
        OrchestratorController.Instance.OnLeaveRoomEvent += OnLeaveRoomHandler;
        OrchestratorController.Instance.OnUserMessageReceivedEvent += OnUserMessageReceivedHandler;
        OrchestratorController.Instance.OnMasterEventReceivedEvent += OnMasterEventReceivedHandler;
        OrchestratorController.Instance.OnUserEventReceivedEvent+= OnUserEventReceivedHandler;
    }

    #endregion

    #region Commands

    #region Socket.io connect

    public void SocketConnect()
    {
        OrchestratorController.Instance.SocketConnect(orchestratorUrlIF.text);
    }

    private void OnConnect(bool pConnected)
    {
        orchestratorConnected.text = pConnected.ToString();
        UpdateEnabledItems();
    }

    private void socketDisconnect()
    {
        OrchestratorController.Instance.socketDisconnect();
    }

    private void OnDisconnect(bool pConnected)
    {
        this.userId.text = "";
        userName.text = "";
        userAdmin.text = "";
        orchestratorConnected.text = pConnected.ToString();
        orchestratorVersion.text = "";
        UpdateEnabledItems();
    }

    private void OnGetOrchestratorVersionHandler(string pVersion)
    {
        orchestratorVersion.text = pVersion;
    }

    #endregion

    #region Orchestrator Logs

    // Display the sent message in the logs
    public void OnOrchestratorRequest(string pRequest)
    {
        AddTextComponentOnContent(logsContainer.transform, ">>> " + pRequest);
    }

    // Display the received message in the logs
    public void OnOrchestratorResponse(string pResponse)
    {
        AddTextComponentOnContent(logsContainer.transform, "<<< " + pResponse);
        StartCoroutine(ScrollLogsToBottom());
    }

    #endregion

    #region Login/Logout

    private void SignIn()
    {
        OrchestratorController.Instance.SignIn(userNamePanel.GetComponentInChildren<InputField>().text, userPasswordPanel.GetComponentInChildren<InputField>().text);
    }

    private void OnSignIn()
    {
        userNameIF.text = userNamePanel.GetComponentInChildren<InputField>().text;
        userPasswordIF.text = userPasswordPanel.GetComponentInChildren<InputField>().text;
        HeadLogin();
    }

    // Login from the main buttons Login & Logout
    private void HeadLogin()
    {
        OrchestratorController.Instance.Login(userNameIF.text, userPasswordIF.text);
    }

    private void Login()
    {
        OrchestratorController.Instance.Login(userNamePanel.GetComponentInChildren<InputField>().text, userPasswordPanel.GetComponentInChildren<InputField>().text);
    }

    private void OnLogin(bool userLoggedSucessfully)
    {
        if (userLoggedSucessfully)
        {
            OrchestratorController.Instance.IsAutoRetrievingData = autoRetrieveOrchestratorDataOnConnect.isOn;
        }
        else
        {
            this.userId.text = "";
            userName.text = "";
            userAdmin.text = "";
        }

        userLogged.text = userLoggedSucessfully.ToString();
        UpdateEnabledItems();
    }


    private void Logout()
    {
        OrchestratorController.Instance.Logout();
    }

    private void OnLogout(bool userLogoutSucessfully)
    {
        if (userLogoutSucessfully)
        {
            userLogged.text = false.ToString();
            this.userId.text = "";
            userName.text = "";
            userAdmin.text = "";
            userMQname.text = "";
            userMQurl.text = "";
        }
        UpdateEnabledItems();
    }

    #endregion

    #region NTP clock

    private void GetNTPTime()
    {
        OrchestratorController.Instance.GetNTPTime();
    }

    #endregion

    #region Sessions

    private void GetSessions()
    {
        OrchestratorController.Instance.GetSessions();
    }

    private void OnGetSessionsHandler(Session[] sessions)
    {
        // clean current session items
        RemoveComponentsFromList(orchestratorSessions.transform);
        Dropdown dd = sessionIdPanel.GetComponentInChildren<Dropdown>();
        dd.ClearOptions();

        // update the list of available sessions
        if (sessions != null && sessions.Length > 0)
        {
            Array.ForEach(sessions, delegate (Session element)
            {
                AddTextComponentOnContent(orchestratorSessions.transform, element.GetGuiRepresentation());
            });

            // update the dropdown
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            Array.ForEach(sessions, delegate (Session session)
            {
                options.Add(new Dropdown.OptionData(session.GetGuiRepresentation()));
            });
            dd.AddOptions(options);
        }
    }

    private void AddSession()
    {
        Dropdown dd = scenarioIdPanel.GetComponentInChildren<Dropdown>();
        OrchestratorController.Instance.AddSession(OrchestratorController.Instance.AvailableScenarios[dd.value].scenarioId,
        sessionNamePanel.GetComponentInChildren<InputField>().text,
        sessionDescriptionPanel.GetComponentInChildren<InputField>().text);
    }

    private void OnAddSessionHandler(Session session)
    {
        // Is equal to AddSession + Join Session, except that session is returned (not on JoinSession)
        if (session != null)
        {
            // update the list of available sessions
            RemoveComponentsFromList(orchestratorSessions.transform);
            Array.ForEach(OrchestratorController.Instance.AvailableSessions, delegate (Session element)
            {
                AddTextComponentOnContent(orchestratorSessions.transform, element.GetGuiRepresentation());
            });

            // update the dropdown
            Dropdown dd = sessionIdPanel.GetComponentInChildren<Dropdown>();
            dd.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            Array.ForEach(OrchestratorController.Instance.AvailableSessions, delegate(Session sess)
            {
                options.Add(new Dropdown.OptionData(sess.GetGuiRepresentation()));
            });
            dd.AddOptions(options);

            userMaster.text = OrchestratorController.Instance.UserIsMaster.ToString(); ;
            userSession.text = session.GetGuiRepresentation();
            userScenario.text = session.scenarioId;
        }
        else
        {
            userSession.text = "";
            userScenario.text = "";
        }
    }

    private void OnGetScenarioInstanceInfoHandler(ScenarioInstance scenario)
    {
        if (scenario != null)
        {
            userScenario.text = scenario.GetGuiRepresentation();
        }
    }

    private void DeleteSession()
    {
        Dropdown dd = sessionIdPanel.GetComponentInChildren<Dropdown>();
        OrchestratorController.Instance.DeleteSession(OrchestratorController.Instance.AvailableSessions[dd.value].sessionId);
    }

    private void OnDeleteSessionHandler()
    {
        userLiveURL.text = "";
        userVODLiveURL.text = "";
    }

    private void JoinSession()
    {
        Dropdown dd = sessionIdPanel.GetComponentInChildren<Dropdown>();
        string sessionIdToJoin = OrchestratorController.Instance.AvailableSessions[dd.value].sessionId;
        OrchestratorController.Instance.JoinSession(sessionIdToJoin);
    }

    private void OnJoinSessionHandler(Session session)
    {
        if(session != null)
        {
            userSession.text = session.GetGuiRepresentation();
            userScenario.text = session.scenarioId;
            userMaster.text = OrchestratorController.Instance.UserIsMaster.ToString();
        }
        else
        {
            userSession.text = "";
            userScenario.text = "";
            userMaster.text = "";
        }
    }

    private void LeaveSession()
    {
        OrchestratorController.Instance.LeaveSession();
    }

    private void OnLeaveSessionHandler()
    {
        userSession.text = "";
        userScenario.text = "";
        userMaster.text = "";

        UpdateConnectedUsersGUI();
    }

    private void OnUserJoinedSessionHandler(string userID)
    {
        if (!string.IsNullOrEmpty(userID))
        {
            UpdateConnectedUsersGUI();
        }
    }

    private void OnUserLeftSessionHandler(string userID)
    {
        if (!string.IsNullOrEmpty(userID))
        {
            UpdateConnectedUsersGUI();
        }
    }

    #endregion

    #region Scenarios

    private void GetScenarios()
    {
        OrchestratorController.Instance.GetScenarios();
    }

    private void OnGetScenariosHandler(Scenario[] scenarios)
    {
        if(scenarios != null && scenarios.Length > 0)
        {
            // update the list of available scenarios
            RemoveComponentsFromList(orchestratorScenarios.transform);
            Array.ForEach(scenarios, delegate (Scenario element)
            {
                AddTextComponentOnContent(orchestratorScenarios.transform, element.GetGuiRepresentation());
            });

            //update the data in the dropdown
            Dropdown dd = scenarioIdPanel.GetComponentInChildren<Dropdown>();
            dd.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            Array.ForEach(scenarios, delegate (Scenario scenario)
            {
                options.Add(new Dropdown.OptionData(scenario.GetGuiRepresentation()));
            });
            dd.AddOptions(options);
        }
    }

    #endregion

    #region Live

    private void OnGetLivePresenterDataHandler(LivePresenterData liveData)
    {
        if(liveData != null)
        {
            userLiveURL.text = liveData.liveAddress;
            userVODLiveURL.text = liveData.vodAddress;
        }
        else
        {
            userLiveURL.text = "";
            userVODLiveURL.text = "";
        }
    }

    #endregion

    #region Users

    private void GetUsers()
    {
        OrchestratorController.Instance.GetUsers();
    }

    private void OnGetUsersHandler(User[] users)
    {
        if(users != null && users.Length > 0)
        {
            // update the list of available users
            RemoveComponentsFromList(orchestratorUsers.transform);
            Array.ForEach(users, delegate (User element)
            {
                AddTextComponentOnContent(orchestratorUsers.transform, element.GetGuiRepresentation());
            });

            //update the data in the dropdown
            Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
            dd.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            Array.ForEach(users, delegate (User user)
            {
                options.Add(new Dropdown.OptionData(user.GetGuiRepresentation()));
            });
            dd.AddOptions(options);
        }
    }

    private void AddUser()
    {
        OrchestratorController.Instance.AddUser(userNamePanel.GetComponentInChildren<InputField>().text,
                                        userPasswordPanel.GetComponentInChildren<InputField>().text,
                                        userAdminPanel.GetComponentInChildren<Toggle>().isOn);
    }

    private void OnAddUserHandler(User user)
    {
        //Nothing to do here, free to add new behaviour.
    }

    private void UpdateUserData()
    {
        UserData lUserData = new UserData();
        lUserData.userMQexchangeName = userDataMQnamePanel.GetComponentInChildren<InputField>().text;
        lUserData.userMQurl = userDataMQurlPanel.GetComponentInChildren<InputField>().text;
        lUserData.userRepresentationType = (UserData.eUserRepresentationType)userDataRepresentationTypeDD.value;

        OrchestratorController.Instance.UpdateUserData(lUserData);
    }

    private void ClearUserData()
    {
        OrchestratorController.Instance.ClearUserData();
    }

    private void GetUserInfo()
    {
        Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        OrchestratorController.Instance.GetUserInfo(OrchestratorController.Instance.AvailableUserAccounts[dd.value].userId);
    }

    private void OnGetUserInfoHandler(User user)
    {
        if (user != null)
        {
            if (string.IsNullOrEmpty(userId.text) || user.userId == OrchestratorController.Instance.SelfUser.userId)
            {
                OrchestratorController.Instance.SelfUser = user;

                userId.text = user.userId;
                userName.text = user.userName;
                userAdmin.text = user.userAdmin.ToString();
                userMQname.text = user.userData.userMQexchangeName;
                userMQurl.text = user.userData.userMQurl;
                userRepresentation.text = user.userData.userRepresentationType.ToString();
            }
        }
    }

    private void DeleteUser()
    {
        Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        OrchestratorController.Instance.DeleteUser(OrchestratorController.Instance.AvailableUserAccounts[dd.value].userId);
    }

    #endregion

    #region Rooms

    private void GetRooms()
    {
        OrchestratorController.Instance.GetRooms();
    }

    private void OnGetRoomsHandler(RoomInstance[] rooms)
    {
        if(rooms != null && rooms.Length > 0)
        {
            //update the data in the dropdown
            Dropdown dd = roomIdPanel.GetComponentInChildren<Dropdown>();
            dd.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            Array.ForEach(rooms, delegate (RoomInstance room)
            {
                options.Add(new Dropdown.OptionData(room.GetGuiRepresentation()));
            });
            dd.AddOptions(options);
        }
    }

    private void JoinRoom()
    {
        Dropdown dd = roomIdPanel.GetComponentInChildren<Dropdown>();
        RoomInstance room = OrchestratorController.Instance.AvailableRooms[dd.value];
        userRoom.text = room.GetGuiRepresentation();
        OrchestratorController.Instance.JoinRoom(room.roomId);
    }

    private void OnJoinRoomHandler(bool hasJoined)
    {
        if(!hasJoined)
        {
            userRoom.text = "";
        }
    }

    private void LeaveRoom()
    {
        OrchestratorController.Instance.LeaveRoom();
    }

    private void OnLeaveRoomHandler()
    {
        userRoom.text = "";
    }

    #endregion

    #region Messages

    private void SendMessage()
    {
        Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        OrchestratorController.Instance.SendMessage(messagePanel.GetComponentInChildren<InputField>().text, OrchestratorController.Instance.AvailableUserAccounts[dd.value].userId);
    }

    private void SendMessageToAll()
    {
        OrchestratorController.Instance.SendMessageToAll(messagePanel.GetComponentInChildren<InputField>().text);
    }

    private void OnUserMessageReceivedHandler(UserMessage userMessage)
    {
        AddTextComponentOnContent(logsContainer.transform, "<<< USER MESSAGE RECEIVED: " + userMessage.fromName + "[" + userMessage.fromId + "]: " + userMessage.message);
        StartCoroutine(ScrollLogsToBottom());
    }

    #endregion

    #region Events

    private void SendEventToMaster()
    {
        if(!OrchestratorController.Instance.UserIsMaster)
        {
            OrchestratorController.Instance.SendEventToMaster(eventPanel.GetComponentInChildren<InputField>().text);
        }
    }

    private void SendEventToUser()
    {
        Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        OrchestratorController.Instance.SendEventToUser(OrchestratorController.Instance.AvailableUserAccounts[dd.value].userId, eventPanel.GetComponentInChildren<InputField>().text);
    }

    private void SendEventToAll()
    {
        OrchestratorController.Instance.SendEventToAll(eventPanel.GetComponentInChildren<InputField>().text);
    }

    private void OnMasterEventReceivedHandler(UserEvent pMasterEventData)
    {
        //Add your event handling
        AddTextComponentOnContent(logsContainer.transform, "<<< MASTER EVENT RECEIVED: [" + pMasterEventData.fromId + "]: " + pMasterEventData.message);
        StartCoroutine(ScrollLogsToBottom());
    }

    private void OnUserEventReceivedHandler (UserEvent pUserEventData)
    {
        //Add your event handling
        AddTextComponentOnContent(logsContainer.transform, "<<< USER EVENT RECEIVED: [" + pUserEventData.fromId + "]: " + pUserEventData.message);
        StartCoroutine(ScrollLogsToBottom());
    }

    #endregion

    #region Data Stream

    private void GetAvailableDataStreams()
    {
        Dropdown dd = userIdPanel.GetComponentInChildren<Dropdown>();
        OrchestratorController.Instance.GetAvailableDataStreams(OrchestratorController.Instance.AvailableUserAccounts[dd.value].userId);
    }

    private void GetRegisteredDataStreams()
    {
        OrchestratorController.Instance.GetRegisteredDataStreams();
    }

    #endregion

    #endregion
}