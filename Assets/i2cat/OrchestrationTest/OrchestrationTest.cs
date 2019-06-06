using OrchestratorWrapping;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum State {
    Default, Login, Create, Join, Lobby, InGame
}

public class OrchestrationTest : MonoBehaviour {
    public bool isDebug = false;
    public bool useEcho = false;

    [Header("General")]
    public OrchestratorGui orchestrator;

    #region UI
    [SerializeField]
    private Text statusText;
    [SerializeField]
    private Text idText;
    [SerializeField]
    private Text nameText;

    [Header("Login")]
    [SerializeField]
    private InputField userNameLoginIF;
    [SerializeField]
    private InputField userPasswordLoginIF;
    public InputField connectionURILoginIF;
    public InputField exchangeNameLoginIF;

    [Header("Content")]
    [SerializeField]
    private RectTransform orchestratorSessions;
    [SerializeField]
    private RectTransform usersSession;

    [Header("Info")]
    //[SerializeField]
    public InputField exchangeNameIF;
    //[SerializeField]
    public InputField connectionURIIF;
    [SerializeField]
    private InputField pcDashServerIF;
    [SerializeField]
    private InputField audioDashServerIF;

    [Header("Create")]
    [SerializeField]
    private InputField sessionNameIF;
    [SerializeField]
    private InputField sessionDescriptionIF;
    [SerializeField]
    private Dropdown scenarioIdDrop;

    [Header("Join")]
    [SerializeField]
    private Dropdown sessionIdDrop;

    [Header("Lobby")]
    [SerializeField]
    private Text sessionNameText;
    [SerializeField]
    private Text sessionDescriptionText;
    //[SerializeField]
    public Text scenarioIdText;
    [SerializeField]
    private Text sessionNumUsersText;

    [Header("Panels")]
    [SerializeField]
    private GameObject loginPanel;
    [SerializeField]
    private GameObject infoPanel;
    [SerializeField]
    private GameObject createPanel;
    [SerializeField]
    private GameObject joinPanel;
    [SerializeField]
    private GameObject lobbyPanel;
    [SerializeField]
    private GameObject sessionPanel;
    [SerializeField]
    private GameObject usersPanel;

    [Header("Buttons")]
    [SerializeField]
    private Button loginButton;
    [SerializeField]
    private Button createButton;
    [SerializeField]
    private Button joinButton;
    [SerializeField]
    private Button doneCreateButton;
    [SerializeField]
    private Button doneJoinButton;
    [SerializeField]
    private Button readyLobbyButton;
    #endregion

    #region Utils
    private Color onlineCol = new Color(0.15f,0.78f,0.15f); // Green
    private Color offlineCol = new Color(0.78f,0.15f,0.15f); // Red
    private float timerUsers = 2.7f;
    private float refreshUsers = 3.0f;
    private float timerSessions = 0.0f;
    private float refreshSessions = 5.0f;
    #endregion

    [HideInInspector]
    public bool isMaster = false;


    private State state = State.Default;

    // Start is called before the first frame update
    void Start() {
        orchestrator.ConnectSocket();
        //orchestrator.TestLogin("admin", "password");

        loginPanel.SetActive(true);
        infoPanel.SetActive(false);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        sessionPanel.SetActive(false);
        usersPanel.SetActive(false);
        createButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);

        DontDestroyOnLoad(this);
        DontDestroyOnLoad(orchestrator);

        StatusTextUpdate();
        state = State.Login;
    }

    // Update is called once per frame

    void Update() {
        // Auto-Refreshing Sessions
        timerSessions += Time.deltaTime;
        if (timerSessions >= refreshSessions) {
            orchestrator.GetSessions();
            timerSessions = 0.0f;
        }
        switch (state) {
            case State.Default:
                break;
            case State.Login:
                if (string.IsNullOrEmpty(userNameLoginIF.text) || string.IsNullOrEmpty(userPasswordLoginIF.text) ||
                    string.IsNullOrEmpty(connectionURILoginIF.text) || string.IsNullOrEmpty(exchangeNameLoginIF.text)) loginButton.interactable = false;
                else loginButton.interactable = true;
                break;
            case State.Create:
                if (string.IsNullOrEmpty(sessionNameIF.text)) doneCreateButton.interactable = false;
                else doneCreateButton.interactable = true;
                break;
            case State.Join:
                if (sessionIdDrop.options.Count == 0) doneJoinButton.interactable = false;
                else doneJoinButton.interactable = true;
                break;
            case State.Lobby:
                // Button interactuability
                //if (orchestrator.activeSession.sessionUsers.Length != 4) readyLobbyButton.interactable = false; // Change the number per maxUsers per pilot
                //else readyLobbyButton.interactable = true;
                // Auto-Refreshing Users
                timerUsers += Time.deltaTime;
                if (timerUsers >= refreshUsers) {
                    orchestrator.orchestratorWrapper.GetSessionInfo();
                    LobbyTextUpdate();
                    timerUsers = 0.0f;
                }
                break;
            case State.InGame:
                break;
            default:
                break;
        }
    }

    #region Auxiliar Methods
    public void StatusTextUpdate() {
        if (orchestrator.connectedToOrchestrator) {
            statusText.text = "Online";
            statusText.color = onlineCol;
        }
        else {
            statusText.text = "Offline";
            statusText.color = offlineCol;
        }
        idText.text = orchestrator.TestGetUserID();
        nameText.text = orchestrator.TestGetUserName();
    }

    public void SessionsUpdate() {
        // update the list of available sessions
        orchestrator.removeComponentsFromList(orchestratorSessions.transform);
        orchestrator.availableSessions.ForEach(delegate (Session element) {
            orchestrator.AddTextComponentOnContent(orchestratorSessions.transform, element.GetGuiRepresentation());
        });
        // update the dropdown
        sessionIdDrop.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        orchestrator.availableSessions.ForEach(delegate (Session session)
        {
            options.Add(new Dropdown.OptionData(session.GetGuiRepresentation()));
        });
        sessionIdDrop.AddOptions(options);

    }

    public void ScenariosUpdate() {
        // update the dropdown
        scenarioIdDrop.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        orchestrator.availableScenarios.ForEach(delegate (Scenario scenario)
        {
            options.Add(new Dropdown.OptionData(scenario.GetGuiRepresentation()));
        });
        scenarioIdDrop.AddOptions(options);
    }

    public void LobbyTextUpdate() {
        // Active Session
        if (orchestrator.activeSession != null) {
            sessionNameText.text = orchestrator.activeSession.sessionName;
            sessionDescriptionText.text = orchestrator.activeSession.sessionDescription;
            sessionNumUsersText.text = orchestrator.activeSession.sessionUsers.Length.ToString() + "/" + "4"; // To change the max users depending the pilot

            // update the list of users in session
            orchestrator.removeComponentsFromList(usersSession.transform);
            for (int i = 0; i < orchestrator.activeSession.sessionUsers.Length; i++) {
                // Make this to show the real name of the user, not the id
                foreach(User u in orchestrator.availableUsers) {
                    if (u.userId == orchestrator.activeSession.sessionUsers[i])
                        orchestrator.AddTextComponentOnContent(usersSession.transform, u.userName);
                }
            }
            Debug.Log("orchestrator.activeSession: Good");
        }
        else {
            sessionNameText.text = "";
            sessionDescriptionText.text = "";
            sessionNumUsersText.text = "X/X";
            orchestrator.removeComponentsFromList(usersSession.transform);
            Debug.Log("orchestrator.activeSession: Bad");
        }
        // Active Scenario
        if (orchestrator.activeScenario != null) {
            scenarioIdText.text = orchestrator.activeScenario.scenarioName;
            Debug.Log("orchestrator.activeScenario: Good");
        }
        else {
            scenarioIdText.text = "";
            Debug.Log("orchestrator.activeScenario: Bad");
        }
    }

    public void LoginButton() {
        loginPanel.SetActive(false);
        infoPanel.SetActive(true);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        sessionPanel.SetActive(true);
        usersPanel.SetActive(false);
        createButton.gameObject.SetActive(true);
        joinButton.gameObject.SetActive(true);

        orchestrator.TestLogin(userNameLoginIF.text, userPasswordLoginIF.text, connectionURILoginIF.text, exchangeNameLoginIF.text);
    }

    public void CreateButton() {
        state = State.Create;
        createPanel.SetActive(true);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        sessionPanel.SetActive(true);
        usersPanel.SetActive(false);
        createButton.interactable = false;
        joinButton.interactable = true;
    }

    public void JoinButton() {
        state = State.Join;
        createPanel.SetActive(false);
        joinPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        sessionPanel.SetActive(true);
        usersPanel.SetActive(false);
        createButton.interactable = true;
        joinButton.interactable = false;
    }

    public void DoneCreateButton() {
        isMaster = true;
        state = State.Lobby;
        orchestrator.TestAddSession(sessionNameIF, sessionDescriptionIF, scenarioIdDrop.value);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        sessionPanel.SetActive(false);
        usersPanel.SetActive(true);
        createButton.interactable = false;
        joinButton.interactable = false;
    }

    public void DoneJoinButton() {
        state = State.Lobby;
        orchestrator.TestJoinSession(sessionIdDrop.value);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        sessionPanel.SetActive(false);
        usersPanel.SetActive(true);
        createButton.interactable = false;
        joinButton.interactable = false;
    }

    public void LeaveButton() {
        isMaster = false;
        state = State.Default;
        orchestrator.LeaveSession();
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        sessionPanel.SetActive(true);
        usersPanel.SetActive(false);
        createButton.interactable = true;
        joinButton.interactable = true;
    }
    
    public void ReadyButton() {
        state = State.InGame;
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        sessionPanel.SetActive(true);
        usersPanel.SetActive(false);
        createButton.interactable = true;
        joinButton.interactable = true;

        if (isMaster && !isDebug) SceneManager.LoadScene("Sample Scenario 2");
        else SceneManager.LoadScene(scenarioIdText.text);
    }

    public void StartGame() {
        if (isMaster) SceneManager.LoadScene("Sample Scenario 2");
        else SceneManager.LoadScene(orchestrator.activeScenario.scenarioName);
    }

    public void UpdateTVMButton() {
        orchestrator.TestUpdateUserData(exchangeNameIF.text,connectionURIIF.text);
    }

    public void DebuggButton(int user) {
        if (user == 0) {
            userNameLoginIF.text = "Marc@i2CAT";
            userPasswordLoginIF.text = "i2CAT2020";
            connectionURILoginIF.text = "amqp://tofis:tofis@192.168.10.109:5672";
            exchangeNameLoginIF.text = "marc_tvm";
        }
        else if (user == 1) {
            userNameLoginIF.text = "Luca@i2CAT";
            userPasswordLoginIF.text = "i2CAT2020";
            connectionURILoginIF.text = "amqp://tofis:tofis@192.168.10.94:5672";
            exchangeNameLoginIF.text = "gianluca";
        }
        else if (user == 2) {
            userNameLoginIF.text = "Luca@i2CAT";
            userPasswordLoginIF.text = "i2CAT2020";
            connectionURILoginIF.text = "amqp://tofis:tofis@192.168.11.122:5672";
            exchangeNameLoginIF.text = "gianluca";
        }
        //orchestrator.TestLogin(userNameLoginIF.text, userPasswordLoginIF.text, connectionURILoginIF.text, exchangeNameLoginIF.text);
    }

    #endregion
}
