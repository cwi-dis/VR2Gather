using OrchestratorWrapping;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum State {
    Default, Create, Join, Lobby
}

public class OrchestrationTest : MonoBehaviour {
    #region UI
    [SerializeField]
    private Text status;
    [SerializeField]
    private RectTransform orchestratorSessions;
    [SerializeField]
    private RectTransform usersSession;

    [Header("Info")]
    [SerializeField]
    private InputField exchangeNameIF;
    [SerializeField]
    private InputField connectionUriIF;
    
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
    [SerializeField]
    private Text scenarioIdText;
    [SerializeField]
    private Text sessionNumUsersText;

    [Header("Panels")]
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
    #endregion

    public OrchestratorGui orchestrator;

    private State state = State.Default;

    // Start is called before the first frame update
    void Start() {
        orchestrator.ConnectSocket();
        orchestrator.TestLogin("admin", "password");

        StatusTextUpdate();

        infoPanel.SetActive(true);
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        sessionPanel.SetActive(true);
        usersPanel.SetActive(false);
}

    // Update is called once per frame
    void Update() {
        switch (state) {
            case State.Default:
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
                if (orchestrator.activeSession.sessionUsers.Length != 4) readyLobbyButton.interactable = false; // Change the number per maxUsers per pilot
                else readyLobbyButton.interactable = true;
                LobbyTextUpdate();
                break;
            default:
                break;
        }
    }

    #region Auxiliar Methods
    public void StatusTextUpdate() {
        if (orchestrator.connectedToOrchestrator) {
            status.text = "Online";
            status.color = onlineCol;
        }
        else {
            status.text = "Offline";
            status.color = offlineCol;
        }
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
            //Debug.Log("Session:" + session.GetGuiRepresentation());
            //Debug.Log("users:" + session.sessionUsers.Length);
            //Array.ForEach<string>(session.sessionUsers, delegate (string user)
            //{
            //    Debug.Log("userId:" + user);
            //});
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
            //Debug.Log("Scenario:" + scenario.GetGuiRepresentation());
            //Debug.Log("ScenarioRooms:" + scenario.scenarioRooms.Count);
            //scenario.scenarioRooms.ForEach(delegate (Room room)
            //{
            //    Debug.Log("ScenarioRoom:" + room.GetGuiRepresentation());
            //});
            options.Add(new Dropdown.OptionData(scenario.GetGuiRepresentation()));
        });
        scenarioIdDrop.AddOptions(options);
    }

    public void LobbyTextUpdate() {
        if (orchestrator.activeSession != null) {
            sessionNameText.text = orchestrator.activeSession.sessionName;
            sessionDescriptionText.text = orchestrator.activeSession.sessionDescription;
            sessionNumUsersText.text = orchestrator.activeSession.sessionUsers.Length.ToString() + "/" + "4"; // To change the max users depending the pilot
            // update the list of users in session
            orchestrator.removeComponentsFromList(usersSession.transform);
            for (int i = 0; i < orchestrator.activeSession.sessionUsers.Length; i++) {
                orchestrator.AddTextComponentOnContent(usersSession.transform, orchestrator.activeSession.sessionUsers[i]);

            }
        }
        else {
            sessionNameText.text = "";
            sessionDescriptionText.text = "";
            sessionNumUsersText.text = "X/X";
            orchestrator.removeComponentsFromList(usersSession.transform);
        }
        if (orchestrator.activeScenario != null) {
            scenarioIdText.text = orchestrator.activeScenario.scenarioName;
        }
        else {
            scenarioIdText.text = "";
        }
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
        orchestrator.LeaveSession();

        state = State.Default;
        createPanel.SetActive(false);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        sessionPanel.SetActive(true);
        usersPanel.SetActive(false);
        createButton.interactable = true;
        joinButton.interactable = true;
    }
    
    public void ReadyButton() {
        //if (orchestrator.activeSession.sessionUsers.Length == 2) SceneManager.LoadScene(orchestrator.activeScenario.scenarioName);
        SceneManager.LoadScene(scenarioIdText.text);
    }

    #endregion
}
