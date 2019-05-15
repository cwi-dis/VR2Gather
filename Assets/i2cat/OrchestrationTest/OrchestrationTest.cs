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
    private InputField exchangeNameIF;
    [SerializeField]
    private InputField connectionUriIF;
    [SerializeField]
    private InputField sessionNameIF;
    [SerializeField]
    private InputField sessionDescriptionIF;
    [SerializeField]
    private Dropdown sessionIdDrop;
    [SerializeField]
    private Dropdown scenarioIdDrop;

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

    [Header("Buttons")]
    [SerializeField]
    private Button createButton;
    [SerializeField]
    private Button joinButton;
    [SerializeField]
    private Button doneCreateButton;
    [SerializeField]
    private Button doneJoinButton;

    [Space(10)]
    [SerializeField]
    private RectTransform orchestratorSessions;
    #endregion

    #region Utils
    private Color onlineCol = new Color(0.15f,0.78f,0.15f); // Green
    private Color offlineCol = new Color(0.78f,0.15f,0.15f); // Red
    #endregion

    public OrchestratorGui orchestrator;

    private State state = State.Default;

    public 
    
    // Start is called before the first frame update
    void Start() {
        orchestrator.ConnectSocket();
        orchestrator.TestLogin("admin", "password");

        StatusTextUpdate();
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

    public void CreateButton() {
        state = State.Create;
        createPanel.SetActive(true);
        joinPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        sessionPanel.SetActive(true);
        createButton.interactable = false;
        joinButton.interactable = true;
    }

    public void JoinButton() {
        state = State.Join;
        createPanel.SetActive(false);
        joinPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        sessionPanel.SetActive(true);
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
        createButton.interactable = true;
        joinButton.interactable = true;
    }
    
    public void ReadyButton() {
        //if (orchestrator.activeSession.sessionUsers.Length == 2) SceneManager.LoadScene(orchestrator.activeScenario.scenarioName);
        SceneManager.LoadScene(orchestrator.activeScenario.scenarioName);
    }

    #endregion
}
