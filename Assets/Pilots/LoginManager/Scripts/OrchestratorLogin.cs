using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.Voice;
using VRT.Core;

public enum State {
    Offline, Online, Logged, Config, Play, Create, Join, Lobby, InGame
}

public class OrchestratorLogin : MonoBehaviour {

    private static OrchestratorLogin instance;

    public static OrchestratorLogin Instance { get { return instance; } }

    #region GUI Components

    public bool developerOptions = true;
    public bool usePresenter = false;
    private int kindAudio = 2; // Set Dash as default
    private int kindPresenter = 0;
    private int ntpSyncThreshold = 4; // Magic number to be defined (in seconds)

    [HideInInspector] public bool isMaster = false;
    [HideInInspector] public string userID = "";

    private State state = State.Offline;

    [SerializeField] private bool autoRetrieveOrchestratorDataOnConnect = true;

    [Header("Info")]
    [SerializeField] private GameObject infoPanel = null;
    [SerializeField] private Text statusText = null;
    [SerializeField] private Text userId = null;
    [SerializeField] private Text userName = null;
    [SerializeField] private Text orchURLText = null;
    [SerializeField] private Text nativeVerText = null;
    [SerializeField] private Text playerVerText = null;
    [SerializeField] private Text orchVerText = null;
    [SerializeField] private Text ntpText = null;

    [Header("Connect")]
    [SerializeField] private GameObject ntpPanel = null;
    [SerializeField] private Button connectButton = null;
    [SerializeField] private Button okButton = null;

    [Header("Login")]
    [SerializeField] private GameObject usersButtonsPanel = null;
    [SerializeField] private GameObject loginPanel = null;
    [SerializeField] private InputField userNameLoginIF = null;
    [SerializeField] private InputField userPasswordLoginIF = null;
    [SerializeField] private Button loginButton = null;
    [SerializeField] private Button signinButton = null;
    [SerializeField] private Toggle rememberMeButton = null;

    [Header("Signin")]
    [SerializeField] private GameObject signinPanel = null;
    [SerializeField] private InputField userNameRegisterIF = null;
    [SerializeField] private InputField userPasswordRegisterIF = null;
    [SerializeField] private InputField confirmPasswordRegisterIF = null;
    [SerializeField] private Button registerButton = null;

    [Header("VRT")]
    [SerializeField] private GameObject vrtPanel = null;
    [SerializeField] private Text userNameVRTText = null;
    [SerializeField] private Button logoutButton = null;
    [SerializeField] private Button playButton = null;
    [SerializeField] private Button configButton = null;

    [Header("Config")]
    [SerializeField] private GameObject configPanel = null;
    [SerializeField] private GameObject tvmInfoGO = null;
    [SerializeField] private GameObject webcamInfoGO = null;
    [SerializeField] private GameObject pccerthInfoGO = null;
    [SerializeField] private InputField tvmConnectionURIConfigIF = null;
    [SerializeField] private InputField tvmExchangeNameConfigIF = null;
    [SerializeField] private InputField pccerthConnectionURIConfigIF = null;
    [SerializeField] private InputField pccerthPCLExchangeNameConfigIF = null;
    [SerializeField] private InputField pccerthMetaExchangeNameConfigIF = null;
    [SerializeField] private Dropdown representationTypeConfigDropdown = null;
    [SerializeField] private Dropdown webcamDropdown = null;
    [SerializeField] private Dropdown microphoneDropdown = null;
    [SerializeField] private RectTransform VUMeter = null;
    [SerializeField] private Button calibButton = null;
    [SerializeField] private Button saveConfigButton = null;
    [SerializeField] private Button exitConfigButton = null;
    [SerializeField] private SelfRepresentationPreview selfRepresentationPreview = null;
    [SerializeField] private Text selfRepresentationDescription = null;

    [Header("Play")]
    [SerializeField] private GameObject playPanel = null;
    [SerializeField] private Button backPlayButton = null;
    [SerializeField] private Button createButton = null;
    [SerializeField] private Button joinButton = null;

    [Header("Create")]
    [SerializeField] private GameObject createPanel = null;
    [SerializeField] private Button backCreateButton = null;
    [SerializeField] private InputField sessionNameIF = null;
    [SerializeField] private InputField sessionDescriptionIF = null;
    [SerializeField] private Dropdown scenarioIdDrop = null;
    [SerializeField] private GameObject presenterPanel = null;
    [SerializeField] private Toggle presenterToggle = null;
    [SerializeField] private Toggle liveToggle = null;
    [SerializeField] private Toggle socketAudioToggle = null;
    [SerializeField] private Toggle dashAudioToggle = null;
    [SerializeField] private Toggle tcpAudioToggle = null;

    [Header("Join")]
    [SerializeField] private GameObject joinPanel = null;
    [SerializeField] private Button backJoinButton = null;
    [SerializeField] private Dropdown sessionIdDrop = null;
    [SerializeField] private int refreshTimer = 5;

    [Header("Lobby")]
    [SerializeField] private GameObject lobbyPanel = null;
    [SerializeField] private Text sessionNameText = null;
    [SerializeField] private Text sessionDescriptionText = null;
    [SerializeField] private Text scenarioIdText = null;
    [SerializeField] private Text sessionNumUsersText = null;
    [SerializeField] private Text userRepresentationLobbyText = null;
    [SerializeField] private Image userRepresentationLobbyImage = null;
    private string sessionMasterID = null;

    [Header("Buttons")]
    [SerializeField] private Button doneCreateButton = null;
    [SerializeField] private Button doneJoinButton = null;
    [SerializeField] private Button readyButton = null;
    [SerializeField] private Button leaveButton = null;
    [SerializeField] private Button refreshSessionsButton = null;
    
    [Header("Content")]
    [SerializeField] private RectTransform orchestratorSessions = null;
    [SerializeField] private RectTransform usersSession = null;

    [Header("Logs container")]
    [SerializeField] private GameObject logsPanel = null;
    [SerializeField] private RectTransform logsContainer = null;
    [SerializeField] private ScrollRect logsScrollRect = null;

    #endregion

    #region Utils
    private Color connectedCol = new Color(0.15f, 0.78f, 0.15f); // Green
    private Color connectingCol = new Color(0.85f, 0.5f, 0.2f); // Orange
    private Color disconnectedCol = new Color(0.78f, 0.15f, 0.15f); // Red
    public Font MenuFont = null;
    private EventSystem system = null;
    private float timer = 0.0f;
    #endregion

    #region GUI

    // Fill a scroll view with a text item
    private void AddTextComponentOnContent(Transform container, string value) {
        GameObject textGO = new GameObject();
        textGO.name = "Text-" + value;
        textGO.transform.SetParent(container);
        Text item = textGO.AddComponent<Text>();
        item.font = MenuFont;
        item.fontSize = 20;
        item.color = Color.white;

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

    private void AddUserComponentOnContent(Transform container, User user) {        
        GameObject userGO = new GameObject();
        userGO.name = "User-" + user.userName;
        userGO.transform.SetParent(container);

        ContentSizeFitter lCsF = userGO.AddComponent<ContentSizeFitter>();
        lCsF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Placeholder
        Text placeholderText = userGO.AddComponent<Text>();
        placeholderText.font = MenuFont;
        placeholderText.fontSize = 20;
        placeholderText.color = Color.white;

        RectTransform rectGO;
        rectGO = placeholderText.GetComponent<RectTransform>();
        rectGO.localPosition = new Vector3(0, 0, 0);
        rectGO.sizeDelta = new Vector2(0, 30);
        rectGO.localScale = Vector3.one;
        placeholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
        placeholderText.verticalOverflow = VerticalWrapMode.Overflow;

        placeholderText.text = " ";

        // TEXT
        Text textItem = new GameObject("Text-" + user.userName).AddComponent<Text>();
        textItem.transform.SetParent(userGO.transform);
        textItem.font = MenuFont;
        textItem.fontSize = 20;
        textItem.color = Color.white;        

        RectTransform rectText;
        rectText = textItem.GetComponent<RectTransform>();
        rectText.anchorMin = new Vector2(0, 0.5f);
        rectText.anchorMax = new Vector2(1, 0.5f);
        rectText.localPosition = new Vector3(40, 0, 0);
        rectText.sizeDelta = new Vector2(0, 30);
        rectText.localScale = Vector3.one;
        textItem.horizontalOverflow = HorizontalWrapMode.Wrap;
        textItem.verticalOverflow = VerticalWrapMode.Overflow;

        textItem.text = user.userName;

        Image imageItem = new GameObject("Image-" + user.userName).AddComponent<Image>();
        imageItem.transform.SetParent(userGO.transform);
        imageItem.type = Image.Type.Simple;
        imageItem.preserveAspect = true;

        RectTransform rectImage;
        rectImage = imageItem.GetComponent<RectTransform>();
        rectImage.anchorMin = new Vector2(0, 0.5f); 
        rectImage.anchorMax = new Vector2(0, 0.5f);
        rectImage.localPosition = new Vector3(15, 0, 0);
        rectImage.sizeDelta = new Vector2(30, 30);
        rectImage.localScale = Vector3.one;
        // IMAGE
        switch (user.userData.userRepresentationType) {
            case UserRepresentationType.__NONE__:
                imageItem.sprite = Resources.Load<Sprite>("Icons/URNoneIcon");
                textItem.text += " - (No Rep)";
                break;
            case UserRepresentationType.__2D__:
                imageItem.sprite = Resources.Load<Sprite>("Icons/URCamIcon");
                textItem.text += " - (2D Video)";
                break;
            case UserRepresentationType.__AVATAR__:
                imageItem.sprite = Resources.Load<Sprite>("Icons/URAvatarIcon");
                textItem.text += " - (3D Avatar)";
                break;
            case UserRepresentationType.__TVM__:
                imageItem.sprite = Resources.Load<Sprite>("Icons/URPCIcon");
                textItem.text += " - (Volumetric 3D Mesh)";
                break;
            case UserRepresentationType.__PCC_CWI_:
            case UserRepresentationType.__PCC_CWIK4A_:
            case UserRepresentationType.__PCC_PROXY__:
                imageItem.sprite = Resources.Load<Sprite>("Icons/URSingleIcon");
                textItem.text += " - (Simple PC)";
                break;
            case UserRepresentationType.__PCC_SYNTH__:
                imageItem.sprite = Resources.Load<Sprite>("Icons/URSingleIcon");
                textItem.text += " - (Synthetic PC)";
                break;
            case UserRepresentationType.__PCC_PRERECORDED__:
                imageItem.sprite = Resources.Load<Sprite>("Icons/URSingleIcon");
                textItem.text += " - (Prerecorded PC)";
                break;
            case UserRepresentationType.__PCC_CERTH__:
                imageItem.sprite = Resources.Load<Sprite>("Icons/URPCIcon");
                textItem.text += " - (Volumetric PC)";
                break;
            case UserRepresentationType.__SPECTATOR__:
                imageItem.sprite = Resources.Load<Sprite>("Icons/URNoneIcon");
                textItem.text += " - (Spectator)";
                break;
            case UserRepresentationType.__CAMERAMAN__:
                imageItem.sprite = Resources.Load<Sprite>("Icons/URCameramanIcon");
                textItem.text += " - (Cameraman)";
                break;
            default:
                break;
        }
    }

    private void RemoveComponentsFromList(Transform container) {
        for (var i = container.childCount - 1; i >= 0; i--) {
            var obj = container.GetChild(i);
            obj.transform.SetParent(null);
            Destroy(obj.gameObject);
        }
    }

    private void UpdateUsersSession(Transform container) {
        RemoveComponentsFromList(usersSession.transform);
        if (OrchestratorController.Instance.ConnectedUsers != null) {
            foreach (User u in OrchestratorController.Instance.ConnectedUsers) {
                //AddTextComponentOnContent(container.transform, u.userName);
                AddUserComponentOnContent(container.transform, u);
            }
            sessionNumUsersText.text = OrchestratorController.Instance.ConnectedUsers.Length.ToString() /*+ "/" + "4"*/;
        }
        else {
            Debug.Log("[OrchestratorLogin][UpdateUsersSession] ConnectedUsers was null");
        }
    }

    private void UpdateSessions(Transform container, Dropdown dd) {
        RemoveComponentsFromList(container.transform);
        Array.ForEach(OrchestratorController.Instance.AvailableSessions, delegate (Session element) {
            AddTextComponentOnContent(container.transform, element.GetGuiRepresentation());
        });

        string selectedOption = "";
        // store selected option in dropdown
        if (dd.options.Count > 0)
            selectedOption = dd.options[dd.value].text;
        // update the dropdown
        dd.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        Array.ForEach(OrchestratorController.Instance.AvailableSessions, delegate (Session sess) {
            options.Add(new Dropdown.OptionData(sess.GetGuiRepresentation()));
        });
        dd.AddOptions(options);
        // re-assign selected option in dropdown
        if (dd.options.Count > 0) { 
            for (int i = 0; i < dd.options.Count; ++i) {
                if (dd.options[i].text == selectedOption)
                    dd.value = i;
            }
        }
    }

    private void UpdateScenarios(Dropdown dd) {
        // update the dropdown
        dd.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        Array.ForEach(OrchestratorController.Instance.AvailableScenarios, delegate (Scenario scenario) {
            options.Add(new Dropdown.OptionData(scenario.GetGuiRepresentation()));
        });
        dd.AddOptions(options);
    }

    private void UpdateRepresentations(Dropdown dd) {
        // Fill UserData representation dropdown according to UserRepresentationType enum declaration
        dd.ClearOptions();
        //dd.AddOptions(new List<string>(Enum.GetNames(typeof(UserRepresentationType))));
        List<string> finalNames = new List<string>();
        foreach (string type in Enum.GetNames(typeof(UserRepresentationType))) {
            string enumName;
            switch (type) {
                case "__NONE__":
                    enumName = "No Representation";
                    break;
                case "__2D__":
                    enumName = "2D Video";
                    break;
                case "__AVATAR__":
                    enumName = "3D Avatar";
                    break;
                case "__TVM__":
                    enumName = "Volumetric 3D Mesh";
                    break;
                case "__PCC_CWI_":
                    enumName = "Simple PointCloud (RealSense)";
                    break;
                case "__PCC_CWIK4A_":
                    enumName = "Simple PointCloud (Kinect)";
                    break;
                case "__PCC_PROXY__":
                    enumName = "Simple PointCloud (5G phone proxy)";
                    break;
                case "__PCC_SYNTH__":
                    enumName = "Synthetic PointCloud (development option)";
                    break;
                case "__PCC_PRERECORDED__":
                    enumName = "Prerecorded PointCloud (development option)";
                    break;
                case "__PCC_CERTH__":
                    enumName = "Volumetric PointCloud (development option)";
                    break;
                case "__SPECTATOR__":
                    enumName = "Spectator";
                    break;
                case "__CAMERAMAN__":
                    enumName = "Cameraman";
                    break;
                default:
                    enumName = type + " Not Defined";
                    break;
            }
            finalNames.Add(enumName);
        }
        dd.AddOptions(finalNames);
    }

    private void UpdateWebcams(Dropdown dd) {
        // Fill UserData representation dropdown according to UserRepresentationType enum declaration
        dd.ClearOptions();
        WebCamDevice[] devices = WebCamTexture.devices;
        List<string> webcams = new List<string>();
        webcams.Add("None");
        foreach (WebCamDevice device in devices)
            webcams.Add(device.name);
        dd.AddOptions(webcams);
    }

    private void Updatemicrophones(Dropdown dd) {
        // Fill UserData representation dropdown according to UserRepresentationType enum declaration
        dd.ClearOptions();
        string[] devices = Microphone.devices;
        List<string> microphones = new List<string>();
        microphones.Add("None");
        foreach (string device in devices)
            microphones.Add(device);
        dd.AddOptions(microphones );
    }


    private IEnumerator ScrollLogsToBottom() {
        yield return new WaitForSeconds(0.2f);
        logsScrollRect.verticalScrollbar.value = 0;
    }

    private void SetUserRepresentationGUI(UserRepresentationType _representationType) {
        userRepresentationLobbyText.text = _representationType.ToString();
        // left change the icon 'userRepresentationLobbyImage'
        switch (_representationType) {
            case UserRepresentationType.__NONE__:
                userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URNoneIcon");
                userRepresentationLobbyText.text = "NO REPRESENTATION";
                break;
            case UserRepresentationType.__2D__:
                userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URCamIcon");
                userRepresentationLobbyText.text = "2D VIDEO";
                break;
            case UserRepresentationType.__AVATAR__:
                userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URAvatarIcon");
                userRepresentationLobbyText.text = "3D AVATAR";
                break;
            case UserRepresentationType.__TVM__:
                userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URPCIcon");
                userRepresentationLobbyText.text = "VOLUMETRIC 3D MESH";
                break;
            case UserRepresentationType.__PCC_CWI_:
            case UserRepresentationType.__PCC_CWIK4A_:
            case UserRepresentationType.__PCC_PROXY__:
                userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URSingleIcon");
                userRepresentationLobbyText.text = "SIMPLE PC";
                break;
            case UserRepresentationType.__PCC_SYNTH__:
                userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URAvatarIcon");
                userRepresentationLobbyText.text = "SYNTHETIC PC";
                break;
            case UserRepresentationType.__PCC_PRERECORDED__:
                userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URSingleIcon");
                userRepresentationLobbyText.text = "PRERECORDED PC";
                break;
            case UserRepresentationType.__PCC_CERTH__:
                userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URPCIcon");
                userRepresentationLobbyText.text = "VOLUMETRIC PC";
                break;
            case UserRepresentationType.__SPECTATOR__:
                userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URNoneIcon");
                userRepresentationLobbyText.text = "SPECTATOR";
                break;
            case UserRepresentationType.__CAMERAMAN__:
                userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URCameramanIcon");
                userRepresentationLobbyText.text = "CAMERAMAN";
                break;
            default:
                break;
        }
    }

    private void SetUserRepresentationDescription(UserRepresentationType _representationType) {
        // left change the icon 'userRepresentationLobbyImage'
        switch (_representationType) {
            case UserRepresentationType.__NONE__:
                selfRepresentationDescription.text = "No visual representation, and no audio communication. The user can only listen.";
                break;
            case UserRepresentationType.__2D__:
                selfRepresentationDescription.text = "2D video window from your camera, as in typical conferencing services.";
                break;
            case UserRepresentationType.__AVATAR__:
                selfRepresentationDescription.text = "3D Synthetic Avatar.";
                break;
            case UserRepresentationType.__TVM__:
                selfRepresentationDescription.text = "Realistic user representation, using the full capturing system with 4 RGB-D cameras, as a Time Varying Meshes (TVM).";
                break;
            case UserRepresentationType.__PCC_CWI_:
                selfRepresentationDescription.text = "Realistic user representation, using a single RealSense RGB-D camera, as a PointCloud.";
                break;
            case UserRepresentationType.__PCC_CWIK4A_:
                selfRepresentationDescription.text = "Realistic user representation, using a single Azure Kinect RGB-D camera, as a PointCloud.";
                break;
            case UserRepresentationType.__PCC_PROXY__:
                selfRepresentationDescription.text = "Realistic user representation, streamed from 5G telephone, as a PointCloud.";
                break;
            case UserRepresentationType.__PCC_SYNTH__:
                selfRepresentationDescription.text = "3D Synthetic PointCloud.";
                break;
            case UserRepresentationType.__PCC_PRERECORDED__:
                selfRepresentationDescription.text = "3D Pre-recorded PointCloud.";
                break;
            case UserRepresentationType.__PCC_CERTH__:
                selfRepresentationDescription.text = "Realistic user representation, using the full capturing system with 4 RGB-D cameras, as a PointCloud.";
                break;
            case UserRepresentationType.__SPECTATOR__:
                selfRepresentationDescription.text = "No visual representation, but audio communication.";
                break;
            case UserRepresentationType.__CAMERAMAN__:
                selfRepresentationDescription.text = "Local video recorder.";
                break;
            default:
                break;
        }
    }

    #endregion

    #region Unity

    // Start is called before the first frame update
    void Start() {
        if (instance == null) {
            instance = this;
        }

        VoiceReader.PrepareDSP();

        system = EventSystem.current;

        // Update Application version
        orchURLText.text = Config.Instance.orchestratorURL;
        nativeVerText.text = VersionLog.Instance.NativeClient;
        playerVerText.text = "v" + Application.version;
        orchVerText.text = "";

        // Font to build gui components for logs!
        //MenuFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

        // Fill UserData representation dropdown according to UserRepresentationType enum declaration
        UpdateRepresentations(representationTypeConfigDropdown);
        UpdateWebcams(webcamDropdown);
        Updatemicrophones(microphoneDropdown);

        // Buttons listeners
        connectButton.onClick.AddListener(delegate { SocketConnect(); });
        okButton.onClick.AddListener(delegate { OKButton(); });
        loginButton.onClick.AddListener(delegate { Login(); });
        signinButton.onClick.AddListener(delegate { SigninButton(); });
        registerButton.onClick.AddListener(delegate { RegisterButton(true); });
        logoutButton.onClick.AddListener(delegate { Logout(); });
        playButton.onClick.AddListener(delegate { StateButton(State.Play); });
        configButton.onClick.AddListener(delegate {
            FillSelfUserData();
            StateButton(State.Config);
        });
        saveConfigButton.onClick.AddListener(delegate { SaveConfigButton(); });
        exitConfigButton.onClick.AddListener(delegate { ExitConfigButton(); });
        calibButton.onClick.AddListener(delegate { GoToCalibration(); });
        refreshSessionsButton.onClick.AddListener(delegate { GetSessions(); });
        backPlayButton.onClick.AddListener(delegate { StateButton(State.Logged); });
        createButton.onClick.AddListener(delegate { StateButton(State.Create); });
        joinButton.onClick.AddListener(delegate { StateButton(State.Join); });
        backCreateButton.onClick.AddListener(delegate { StateButton(State.Play); });
        doneCreateButton.onClick.AddListener(delegate { AddSession(); });
        backJoinButton.onClick.AddListener(delegate { StateButton(State.Play); });
        doneJoinButton.onClick.AddListener(delegate { JoinSession(); });
        readyButton.onClick.AddListener(delegate { ReadyButton(); });
        leaveButton.onClick.AddListener(delegate { LeaveSession(); });

        // Dropdown listeners
        representationTypeConfigDropdown.onValueChanged.AddListener(delegate { PanelChanger(); });
        webcamDropdown.onValueChanged.AddListener(delegate { PanelChanger(); });
        microphoneDropdown.onValueChanged.AddListener(delegate {
            selfRepresentationPreview.ChangeMicrophone(microphoneDropdown.options[microphoneDropdown.value].text);
        });

        InitialiseControllerEvents();

        socketAudioToggle.isOn = true;
        dashAudioToggle.isOn = false;
        tcpAudioToggle.isOn = false;
        presenterToggle.isOn = false;
        liveToggle.isOn = false;

        if (OrchestratorController.Instance.UserIsLogged) { // Comes from another scene
            // Set status to online
            statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
            statusText.color = connectedCol;
            FillSelfUserData();
            UpdateSessions(orchestratorSessions, sessionIdDrop);
            UpdateScenarios(scenarioIdDrop);
            Debug.Log("Come from another Scene");

            OrchestratorController.Instance.OnLoginResponse(new ResponseStatus(), userId.text);
        }
        else { // Enter for first time
            // Set status to offline
            statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
            statusText.color = disconnectedCol;
            state = State.Offline;

            // Try to connect
            SocketConnect();
        }
    }

    // Update is called once per frame
    void Update() {
        if(VUMeter && selfRepresentationPreview)
            VUMeter.sizeDelta = new Vector2(355 * Mathf.Min( 1, selfRepresentationPreview.MicrophoneLevel), 20);

        TabShortcut();
        if (state == State.Create) {
            AudioToggle();
            PresenterToggles();
        }
        // Refresh Sessions
        if (state == State.Join) {
            timer += Time.deltaTime;
            if (timer >= refreshTimer) {
                GetSessions();
                timer = 0.0f;
            }
        }
    }

    public void FillSelfUserData() {
        if (OrchestratorController.Instance == null || OrchestratorController.Instance.SelfUser==null) return;
        User user = OrchestratorController.Instance.SelfUser;

        // UserID & Name
        userId.text = user.userId;
        userName.text = user.userName;
        userNameVRTText.text = user.userName;
        // Config Info
        UserData userData = user.userData;
        tvmExchangeNameConfigIF.text = userData.userMQexchangeName;
        tvmConnectionURIConfigIF.text = userData.userMQurl;
        pccerthConnectionURIConfigIF.text = Config.Instance.LocalUser.PCSelfConfig.CerthReaderConfig.ConnectionURI;
        pccerthPCLExchangeNameConfigIF.text = Config.Instance.LocalUser.PCSelfConfig.CerthReaderConfig.PCLExchangeName;
        pccerthMetaExchangeNameConfigIF.text = Config.Instance.LocalUser.PCSelfConfig.CerthReaderConfig.MetaExchangeName;
        representationTypeConfigDropdown.value = (int)userData.userRepresentationType;
        webcamDropdown.value = 0;

        for (int i = 0; i < webcamDropdown.options.Count; ++i) {
            if (webcamDropdown.options[i].text == userData.webcamName) {
                webcamDropdown.value = i;
                break;
            }
        }
        microphoneDropdown.value = 0;
        for (int i = 0; i < microphoneDropdown.options.Count; ++i) {
            if (microphoneDropdown.options[i].text == userData.microphoneName) {
                microphoneDropdown.value = i;
                break;
            }
        }
    }

    public void PanelChanger() {
        switch (state) {
            case State.Offline:
                // Panels
                ntpPanel.SetActive(false);
                loginPanel.SetActive(false);
                if (developerOptions) {
                    infoPanel.SetActive(true);
                    usersButtonsPanel.SetActive(false);
                    logsPanel.SetActive(true);
                }
                else {
                    infoPanel.SetActive(false);
                    logsPanel.SetActive(false);
                }
                vrtPanel.SetActive(false);
                configPanel.SetActive(false);
                playPanel.SetActive(false);
                createPanel.SetActive(false);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                // Buttons
                connectButton.gameObject.SetActive(true);
                break;
            case State.Online:
                // Panels
                ntpPanel.SetActive(false);
                loginPanel.SetActive(true);
                CheckRememberMe();
                if (developerOptions) {
                    infoPanel.SetActive(true);
                    usersButtonsPanel.SetActive(true);
                    logsPanel.SetActive(true);
                }
                else {
                    infoPanel.SetActive(false);
                    logsPanel.SetActive(false);
                }
                vrtPanel.SetActive(false);
                configPanel.SetActive(false);
                playPanel.SetActive(false);
                createPanel.SetActive(false);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                // Buttons
                connectButton.gameObject.SetActive(false);
                break;
            case State.Logged:
                // Panels
                ntpPanel.SetActive(false);
                loginPanel.SetActive(false);
                if (developerOptions) {
                    infoPanel.SetActive(true);
                    usersButtonsPanel.SetActive(false);
                    logsPanel.SetActive(true);
                }
                else {
                    infoPanel.SetActive(false);
                    logsPanel.SetActive(false);
                }
                vrtPanel.SetActive(true);
                configPanel.SetActive(false);
                playPanel.SetActive(false);
                createPanel.SetActive(false);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                // Buttons
                connectButton.gameObject.SetActive(false);
                break;
            case State.Config:
                // Panels
                ntpPanel.SetActive(false);
                loginPanel.SetActive(false);
                if (developerOptions) {
                    infoPanel.SetActive(true);
                    usersButtonsPanel.SetActive(false);
                    logsPanel.SetActive(true);
                }
                else {
                    infoPanel.SetActive(false);
                    logsPanel.SetActive(false);
                }
                vrtPanel.SetActive(false);
                configPanel.SetActive(true);
                playPanel.SetActive(false);
                createPanel.SetActive(false);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                // Buttons
                connectButton.gameObject.SetActive(false);
                // Behaviour
                SelfRepresentationChanger();
                break;
            case State.Play:
                // Panels
                ntpPanel.SetActive(false);
                loginPanel.SetActive(false);
                if (developerOptions) {
                    infoPanel.SetActive(true);
                    usersButtonsPanel.SetActive(false);
                    logsPanel.SetActive(true);
                }
                else {
                    infoPanel.SetActive(false);
                    logsPanel.SetActive(false);
                }
                vrtPanel.SetActive(false);
                configPanel.SetActive(false);
                playPanel.SetActive(true);
                createPanel.SetActive(false);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                // Buttons
                connectButton.gameObject.SetActive(false);
                break;
            case State.Create:
                // Panels
                ntpPanel.SetActive(false);
                loginPanel.SetActive(false);
                if (developerOptions) {
                    infoPanel.SetActive(true);
                    usersButtonsPanel.SetActive(false);
                    logsPanel.SetActive(true);
                }
                else {
                    infoPanel.SetActive(false);
                    logsPanel.SetActive(false);
                }
                vrtPanel.SetActive(false);
                configPanel.SetActive(false);
                playPanel.SetActive(false);
                createPanel.SetActive(true);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(false);
                // Buttons
                connectButton.gameObject.SetActive(false);
                break;
            case State.Join:
                // Panels
                ntpPanel.SetActive(false);
                loginPanel.SetActive(false);
                if (developerOptions) {
                    infoPanel.SetActive(true);
                    usersButtonsPanel.SetActive(false);
                    logsPanel.SetActive(true);
                }
                else {
                    infoPanel.SetActive(false);
                    logsPanel.SetActive(false);
                }
                vrtPanel.SetActive(false);
                configPanel.SetActive(false);
                playPanel.SetActive(false);
                createPanel.SetActive(false);
                joinPanel.SetActive(true);
                lobbyPanel.SetActive(false);
                // Buttons
                connectButton.gameObject.SetActive(false);
                // Behaviour
                GetSessions();
                break;
            case State.Lobby:
                // Panels
                ntpPanel.SetActive(false);
                loginPanel.SetActive(false);
                if (developerOptions) {
                    infoPanel.SetActive(true);
                    usersButtonsPanel.SetActive(false);
                    logsPanel.SetActive(true);
                }
                else {
                    infoPanel.SetActive(false);
                    logsPanel.SetActive(false);
                }
                vrtPanel.SetActive(false);
                configPanel.SetActive(false);
                playPanel.SetActive(false);
                createPanel.SetActive(false);
                joinPanel.SetActive(false);
                lobbyPanel.SetActive(true);
                // Buttons
                connectButton.gameObject.SetActive(false);
                if (OrchestratorController.Instance.UserIsMaster)
                    readyButton.gameObject.SetActive(true);
                else
                    readyButton.gameObject.SetActive(false);
                break;
            case State.InGame:
                break;
            default:
                break;
        }
        SelectFirstIF();
    }

    public void SelfRepresentationChanger() {
        // Dropdown Logic
        tvmInfoGO.SetActive(false);
        webcamInfoGO.SetActive(false);
        pccerthInfoGO.SetActive(false);
        calibButton.gameObject.SetActive(false);
        if ((UserRepresentationType)representationTypeConfigDropdown.value == UserRepresentationType.__TVM__) {
            tvmInfoGO.SetActive(true);
            calibButton.gameObject.SetActive(true);
        }
        else if ((UserRepresentationType)representationTypeConfigDropdown.value == UserRepresentationType.__PCC_CWI_)
        {
            calibButton.gameObject.SetActive(true);
        }
        else if ((UserRepresentationType)representationTypeConfigDropdown.value == UserRepresentationType.__PCC_CWIK4A_)
        {
            calibButton.gameObject.SetActive(true);
        }
        else if ((UserRepresentationType)representationTypeConfigDropdown.value == UserRepresentationType.__PCC_PROXY__)
        {
            calibButton.gameObject.SetActive(true);
        }
        else if ((UserRepresentationType)representationTypeConfigDropdown.value == UserRepresentationType.__PCC_SYNTH__)
        {
            calibButton.gameObject.SetActive(true);
        }
        else if ((UserRepresentationType)representationTypeConfigDropdown.value == UserRepresentationType.__PCC_CERTH__) {
            pccerthInfoGO.SetActive(true);
            calibButton.gameObject.SetActive(true);
        }
        else if ((UserRepresentationType)representationTypeConfigDropdown.value == UserRepresentationType.__2D__) {
            webcamInfoGO.SetActive(true);
        }
        // Preview
        SetUserRepresentationDescription((UserRepresentationType)representationTypeConfigDropdown.value);
        selfRepresentationPreview.ChangeRepresentation((UserRepresentationType)representationTypeConfigDropdown.value,
            webcamDropdown.options[webcamDropdown.value].text);
        selfRepresentationPreview.ChangeMicrophone(microphoneDropdown.options[microphoneDropdown.value].text);
    }

    private void OnDestroy() {
        TerminateControllerEvents();
    }

#endregion

#region Input

    void SelectFirstIF() {
        try {
            InputField[] inputFields = FindObjectsOfType<InputField>();
            if (inputFields != null) {
                inputFields[inputFields.Length - 1].OnPointerClick(new PointerEventData(system));  //if it's an input field, also set the text caret
                inputFields[inputFields.Length - 1].caretWidth = 2;
                //system.SetSelectedGameObject(first.gameObject, new BaseEventData(system));
            }
        }
        catch { }
    }

    void TabShortcut() {
        if (Input.GetKeyDown(KeyCode.Tab)) {
            try {
                Selectable current = system.currentSelectedGameObject.GetComponent<Selectable>();
                if (current != null) {
                    Selectable next = current.FindSelectableOnDown();
                    if (next != null) {
                        InputField inputfield = next.GetComponent<InputField>();
                        if (inputfield != null) {
                            inputfield.OnPointerClick(new PointerEventData(system));  //if it's an input field, also set the text caret
                            inputfield.caretWidth = 2;
                        }

                        system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
                    }
                    else {
                        // Select the first IF because no more elements exists in the list
                        SelectFirstIF();
                    }
                }
                //else Debug.Log("no selectable object selected in event system");
            }
            catch { }
        }
    }

#endregion

#region Buttons

    private void SigninButton() {
        loginPanel.SetActive(false);
        signinPanel.SetActive(true);
    }

    public void RegisterButton(bool register) {
        if (register) {
            if (userPasswordRegisterIF.text == confirmPasswordRegisterIF.text) {
                SignIn();
                confirmPasswordRegisterIF.textComponent.color = Color.white;
            }
            else {
                confirmPasswordRegisterIF.textComponent.color = Color.red;
            }
        }
        else {
            loginPanel.SetActive(true);
            signinPanel.SetActive(false);
            confirmPasswordRegisterIF.textComponent.color = Color.white;
        }
    }

    public void OKButton() {
        PanelChanger();
    }

    public void SaveConfigButton() {
        selfRepresentationPreview.Stop();
        selfRepresentationPreview.StopMicrophone();
        UpdateUserData();
        state = State.Logged;
        PanelChanger();
    }

    public void ExitConfigButton() {
        selfRepresentationPreview.Stop();
        selfRepresentationPreview.StopMicrophone();
        GetUserInfo();
        state = State.Logged;
        PanelChanger();
    }

    public void StateButton(State _state) {
        state = _state;
        PanelChanger();
        if (state == State.Config)
            UpdateUserData();
    }

    public void ReadyButton() {
        if (OrchestratorController.Instance.MyScenario.scenarioName == "Pilot 2")
            SendMessageToAll("START_" + OrchestratorController.Instance.MyScenario.scenarioName + "_" + kindAudio + "_" + kindPresenter);
        else 
            SendMessageToAll("START_" + OrchestratorController.Instance.MyScenario.scenarioName + "_" + kindAudio);
    }

    public void GoToCalibration() {
        StartCoroutine(CalibRoutine());
    }

    IEnumerator CalibRoutine() {
        UpdateUserData();
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("SelfCalibration");
    }

    public void AutoFillButtons(int user) {
        switch (user) {
            case 0:
                userNameLoginIF.text = "Marc@i2CAT";
                userPasswordLoginIF.text = "i2CAT2020";
                break;
            case 1:
                userNameLoginIF.text = "Luca@i2CAT";
                userPasswordLoginIF.text = "i2CAT2020";
                break;
            case 2:
                userNameLoginIF.text = "Einar@i2CAT";
                userPasswordLoginIF.text = "i2CAT2020";
                break;
            case 3:
                userNameLoginIF.text = "Isaac@i2CAT";
                userPasswordLoginIF.text = "i2CAT2020";
                break;
            case 4:
                userNameLoginIF.text = "cwibig";
                userPasswordLoginIF.text = "CWI2020";
                break;
            case 5:
                userNameLoginIF.text = "cwismall";
                userPasswordLoginIF.text = "CWI2020";
                break;
            case 6:
                userNameLoginIF.text = "cwitiny";
                userPasswordLoginIF.text = "CWI2020";
                break;
            case 7:
                userNameLoginIF.text = "Jack@CWI";
                userPasswordLoginIF.text = "CWI2020";
                break;
            case 8:
                userNameLoginIF.text = "Shishir@CWI";
                userPasswordLoginIF.text = "CWI2020";
                break;
            case 9:
                userNameLoginIF.text = "Fernando@THEMO";
                userPasswordLoginIF.text = "THEMO2020";
                break;
            case 10:
                userNameLoginIF.text = "Romain@MS";
                userPasswordLoginIF.text = "MS2020";
                break;
            case 11:
                userNameLoginIF.text = "Argyris@CERTH";
                userPasswordLoginIF.text = "CERTH2020";
                break;
            case 12:
                userNameLoginIF.text = "Spiros@CERTH";
                userPasswordLoginIF.text = "CERTH2020";
                break;
            case 13:
                userNameLoginIF.text = "Vincent@VO";
                userPasswordLoginIF.text = "VO2020";
                break;
            case 14:
                userNameLoginIF.text = "Patrice@VO";
                userPasswordLoginIF.text = "VO2020";
                break;
			case 15:
				userNameLoginIF.text = "Bart@ARTANIM";
				userPasswordLoginIF.text = "ARTANIM2020";
				break;
			case 16:
                userNameLoginIF.text = "Name";
                userPasswordLoginIF.text = "Lastname";
                break;
            default:
                break;
        }
    }

#endregion

#region Toggles 

    private void AudioToggle() {
        socketAudioToggle.interactable = !socketAudioToggle.isOn;
        dashAudioToggle.interactable = !dashAudioToggle.isOn;
        tcpAudioToggle.interactable = !tcpAudioToggle.isOn;
    }

    public void SetAudio(int kind) {
        switch (kind) {
            case 1: // Socket
                if (socketAudioToggle.isOn) {
                    // Set AudioType
                    Config.Instance.protocolType = Config.ProtocolType.SocketIO;
                    // Set Toggles
                    dashAudioToggle.isOn = false;
                    tcpAudioToggle.isOn = false;
                }
                break;
            case 2: // Dash
                if (dashAudioToggle.isOn)
                {
                    // Set AudioType
                    Config.Instance.protocolType = Config.ProtocolType.Dash;
                    // Set Toggles
                    socketAudioToggle.isOn = false;
                    tcpAudioToggle.isOn = false;
                }
                break;
            case 3: // Dash
                if (tcpAudioToggle.isOn)
                {
                    // Set AudioType
                    Config.Instance.protocolType = Config.ProtocolType.TCP;
                    // Set Toggles
                    socketAudioToggle.isOn = false;
                    dashAudioToggle.isOn = false;
                }
                break;
            default:
                break;
        }
        kindAudio = kind;
    }

    private void PresenterToggles() {
        if (OrchestratorController.Instance.AvailableScenarios[scenarioIdDrop.value].scenarioName == "Pilot 2") {
            presenterPanel.SetActive(true);
            // Check if presenter is active to show live option
            if (presenterToggle.isOn) {
                liveToggle.gameObject.SetActive(true);
                if (liveToggle.isOn) {
                    Config.Instance.presenter = Config.Presenter.Live;
                    kindPresenter = 2;
                }
                else {
                    Config.Instance.presenter = Config.Presenter.Local;
                    kindPresenter = 1;
                }
            }
            else {
                liveToggle.gameObject.SetActive(false);
                Config.Instance.presenter = Config.Presenter.None;
                kindPresenter = 0;
            }  
        }
        else {
            presenterPanel.SetActive(false);
        }
    }      

#endregion

#region Events listeners

    // Subscribe to Orchestrator Wrapper Events
    private void InitialiseControllerEvents() {
        OrchestratorController.Instance.OnConnectionEvent += OnConnect;
        OrchestratorController.Instance.OnConnectingEvent += OnConnecting;
        OrchestratorController.Instance.OnConnectionEvent += OnDisconnect;
        OrchestratorController.Instance.OnOrchestratorRequestEvent += OnOrchestratorRequest;
        OrchestratorController.Instance.OnOrchestratorResponseEvent += OnOrchestratorResponse;
        OrchestratorController.Instance.OnGetOrchestratorVersionEvent += OnGetOrchestratorVersionHandler;
        OrchestratorController.Instance.OnLoginEvent += OnLogin;
        OrchestratorController.Instance.OnLogoutEvent += OnLogout;
        OrchestratorController.Instance.OnSignInEvent += OnSignIn;
        OrchestratorController.Instance.OnGetNTPTimeEvent += OnGetNTPTimeResponse;
        OrchestratorController.Instance.OnGetSessionsEvent += OnGetSessionsHandler;
        OrchestratorController.Instance.OnAddSessionEvent += OnAddSessionHandler;
        OrchestratorController.Instance.OnGetSessionInfoEvent += OnGetSessionInfoHandler;
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
        OrchestratorController.Instance.OnUserEventReceivedEvent += OnUserEventReceivedHandler;
        OrchestratorController.Instance.OnErrorEvent += OnErrorHandler;
    }

    // Un-Subscribe to Orchestrator Wrapper Events
    private void TerminateControllerEvents() {
        OrchestratorController.Instance.OnConnectionEvent -= OnConnect;
        OrchestratorController.Instance.OnConnectingEvent -= OnConnecting;
        OrchestratorController.Instance.OnConnectionEvent -= OnDisconnect;
        OrchestratorController.Instance.OnOrchestratorRequestEvent -= OnOrchestratorRequest;
        OrchestratorController.Instance.OnOrchestratorResponseEvent -= OnOrchestratorResponse;
        OrchestratorController.Instance.OnGetOrchestratorVersionEvent -= OnGetOrchestratorVersionHandler;
        OrchestratorController.Instance.OnLoginEvent -= OnLogin;
        OrchestratorController.Instance.OnLogoutEvent -= OnLogout;
        OrchestratorController.Instance.OnSignInEvent -= OnSignIn;
        OrchestratorController.Instance.OnGetNTPTimeEvent -= OnGetNTPTimeResponse;
        OrchestratorController.Instance.OnGetSessionsEvent -= OnGetSessionsHandler;
        OrchestratorController.Instance.OnAddSessionEvent -= OnAddSessionHandler;
        OrchestratorController.Instance.OnGetSessionInfoEvent -= OnGetSessionInfoHandler;
        OrchestratorController.Instance.OnJoinSessionEvent -= OnJoinSessionHandler;
        OrchestratorController.Instance.OnLeaveSessionEvent -= OnLeaveSessionHandler;
        OrchestratorController.Instance.OnDeleteSessionEvent -= OnDeleteSessionHandler;
        OrchestratorController.Instance.OnUserJoinSessionEvent -= OnUserJoinedSessionHandler;
        OrchestratorController.Instance.OnUserLeaveSessionEvent -= OnUserLeftSessionHandler;
        OrchestratorController.Instance.OnGetScenarioEvent -= OnGetScenarioInstanceInfoHandler;
        OrchestratorController.Instance.OnGetScenariosEvent -= OnGetScenariosHandler;
        OrchestratorController.Instance.OnGetLiveDataEvent -= OnGetLivePresenterDataHandler;
        OrchestratorController.Instance.OnGetUsersEvent -= OnGetUsersHandler;
        OrchestratorController.Instance.OnAddUserEvent -= OnAddUserHandler;
        OrchestratorController.Instance.OnGetUserInfoEvent -= OnGetUserInfoHandler;
        OrchestratorController.Instance.OnGetRoomsEvent -= OnGetRoomsHandler;
        OrchestratorController.Instance.OnJoinRoomEvent -= OnJoinRoomHandler;
        OrchestratorController.Instance.OnLeaveRoomEvent -= OnLeaveRoomHandler;
        OrchestratorController.Instance.OnUserMessageReceivedEvent -= OnUserMessageReceivedHandler;
        OrchestratorController.Instance.OnMasterEventReceivedEvent -= OnMasterEventReceivedHandler;
        OrchestratorController.Instance.OnUserEventReceivedEvent -= OnUserEventReceivedHandler;
        OrchestratorController.Instance.OnErrorEvent -= OnErrorHandler;
    }

#endregion

#region Commands

#region Socket.io connect

    public void SocketConnect() {
        switch (OrchestratorController.Instance.ConnectionStatus) {
            case OrchestratorController.orchestratorConnectionStatus.__DISCONNECTED__:
                OrchestratorController.Instance.SocketConnect(Config.Instance.orchestratorURL);
                break;
            case OrchestratorController.orchestratorConnectionStatus.__CONNECTING__:
                OrchestratorController.Instance.Abort();
                break;
        }
    }

    private void OnConnect(bool pConnected) {
        if (pConnected) {
            statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
            statusText.color = connectedCol;
            state = State.Online;
        }
        PanelChanger();
    }

    private void OnConnecting() {
        statusText.text = OrchestratorController.orchestratorConnectionStatus.__CONNECTING__.ToString();
        statusText.color = connectingCol;
    }

    private void socketDisconnect() {
        OrchestratorController.Instance.socketDisconnect();
    }

    private void OnDisconnect(bool pConnected) {
        if (!pConnected) {
            OnLogout(true);
            statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
            statusText.color = disconnectedCol;
            state = State.Offline;
        }
        PanelChanger();
    }

    private void OnGetOrchestratorVersionHandler(string pVersion) {
        Debug.Log("Orchestration Service: " + pVersion);
        orchVerText.text = pVersion;
        OrchestratorController.Instance.GetNTPTime();
    }

#endregion

#region Orchestrator Logs

    // Display the sent message in the logs
    public void OnOrchestratorRequest(string pRequest) {
        AddTextComponentOnContent(logsContainer.transform, ">>> " + pRequest);
    }

    // Display the received message in the logs
    public void OnOrchestratorResponse(string pResponse) {
        string lResponse = pResponse.Length <= 8192 ? pResponse : pResponse.Substring(0, 8192) + "...";
        AddTextComponentOnContent(logsContainer.transform, "<<< " + lResponse);
        StartCoroutine(ScrollLogsToBottom());
    }

#endregion

#region Login/Logout

    private void SignIn() {
        Debug.Log("[OrchestratorLogin][SignIn] Send SignIn registration for user " + userNameRegisterIF.text);
        OrchestratorController.Instance.SignIn(userNameRegisterIF.text, userPasswordRegisterIF.text);
    }

    private void OnSignIn() {
        Debug.Log("[OrchestratorLogin][OnSignIn] User " + userNameLoginIF.text +" successfully registered.");
        userNameLoginIF.text = userNameRegisterIF.text;
        userPasswordLoginIF.text = userPasswordRegisterIF.text;
        loginPanel.SetActive(true);
        signinPanel.SetActive(false);
    }

    // Login from the main buttons Login & Logout
    private void Login() {
        if (rememberMeButton.isOn) {
            PlayerPrefs.SetString("userNameLoginIF", userNameLoginIF.text);
            PlayerPrefs.SetString("userPasswordLoginIF", userPasswordLoginIF.text);
        } else {
            PlayerPrefs.DeleteKey("userNameLoginIF");
            PlayerPrefs.DeleteKey("userPasswordLoginIF");
        }
        OrchestratorController.Instance.Login(userNameLoginIF.text, userPasswordLoginIF.text);
    }

    // Check saved used credentials.
    private void CheckRememberMe() {
        if (PlayerPrefs.HasKey("userNameLoginIF") && PlayerPrefs.HasKey("userPasswordLoginIF")) {
            rememberMeButton.isOn = true;
            userNameLoginIF.text = PlayerPrefs.GetString("userNameLoginIF");
            userPasswordLoginIF.text = PlayerPrefs.GetString("userPasswordLoginIF");
        } else
            rememberMeButton.isOn = false;
    }

    private void OnLogin(bool userLoggedSucessfully) {
        if (userLoggedSucessfully) {
            OrchestratorController.Instance.IsAutoRetrievingData = autoRetrieveOrchestratorDataOnConnect;

            // UserData info in Login
            //UserData lUserData = new UserData {
            //    userMQexchangeName = exchangeNameLoginIF.text,
            //    userMQurl = connectionURILoginIF.text,
            //    userRepresentationType = (UserRepresentationType)representationTypeLoginDropdown.value
            //};
            //OrchestratorController.Instance.UpdateUserData(lUserData);
            state = State.Logged;
        }
        else {
            this.userId.text = "";
            userName.text = "";
            userNameVRTText.text = "";

            state = State.Online;
        }

        PanelChanger();
    }
    
    private void Logout() {
        OrchestratorController.Instance.Logout();
    }

    private void OnLogout(bool userLogoutSucessfully) {
        if (userLogoutSucessfully) {
            userId.text = "";
            userName.text = "";
            userNameVRTText.text = "";
            state = State.Online;
        }
        PanelChanger();
    }

#endregion

#region NTP clock

    private void GetNTPTime() {
        OrchestratorController.Instance.GetNTPTime();
    }

    private void OnGetNTPTimeResponse(NtpClock ntpTime) {
        int difference = Helper.GetClockTimestamp(DateTime.UtcNow) - ntpTime.Timestamp;
        if (difference >= ntpSyncThreshold || difference <= -ntpSyncThreshold) {
            ntpText.text = "You have a desynchronization of " + difference + " sec with the Orchestrator.\nYou may suffer some problems as a result.";
            ntpPanel.SetActive(true);
            loginPanel.SetActive(false);
        }
        Debug.Log("[OrchestratorLogin][OnGetNTPTimeResponse] Difference: " + difference);
    }

#endregion

#region Sessions

    private void GetSessions() {
        OrchestratorController.Instance.GetSessions();
    }

    private void OnGetSessionsHandler(Session[] sessions) {
        if (sessions != null) {
            // update the list of available sessions
            UpdateSessions(orchestratorSessions, sessionIdDrop);
        }
    }

    private void AddSession() {
        OrchestratorController.Instance.AddSession(OrchestratorController.Instance.AvailableScenarios[scenarioIdDrop.value].scenarioId,
                                                    sessionNameIF.text,
                                                    sessionDescriptionIF.text);
    }

    private void OnAddSessionHandler(Session session) {
        // Is equal to AddSession + Join Session, except that session is returned (not on JoinSession)
        if (session != null) {
            // update the list of available sessions
            UpdateSessions(orchestratorSessions, sessionIdDrop);

            // Update the info in LobbyPanel
            isMaster = OrchestratorController.Instance.UserIsMaster;
            sessionNameText.text = session.sessionName;
            sessionDescriptionText.text = session.sessionDescription;
            sessionMasterID = OrchestratorController.Instance.GetUser(session.sessionMaster).userName;

            // Update the list of session users
            UpdateUsersSession(usersSession);

            state = State.Lobby;
            PanelChanger();
        }
        else {
            isMaster = false;
            sessionNameText.text = "";
            sessionDescriptionText.text = "";
            scenarioIdText.text = "";
            sessionNumUsersText.text = "";
            sessionMasterID = "";
            RemoveComponentsFromList(usersSession.transform);
        }
    }

    private void OnGetSessionInfoHandler(Session session) {
        if (session != null) {
            // Update the info in LobbyPanel
            isMaster = OrchestratorController.Instance.UserIsMaster;
            sessionNameText.text = session.sessionName;
            sessionDescriptionText.text = session.sessionDescription;
            if (session.sessionMaster != "")
                sessionMasterID = OrchestratorController.Instance.GetUser(session.sessionMaster).userName;
            // Update the list of session users
            UpdateUsersSession(usersSession);
        } else {
            isMaster = false;
            sessionNameText.text = "";
            sessionDescriptionText.text = "";
            scenarioIdText.text = "";
            sessionNumUsersText.text = "";
            sessionMasterID = "";
            RemoveComponentsFromList(usersSession.transform);
        }
    }

    private void OnGetScenarioInstanceInfoHandler(ScenarioInstance scenario) {
        if (scenario != null) {
            scenarioIdText.text = scenario.scenarioName;
            // Update the list of session users
            UpdateUsersSession(usersSession);
        }
    }

    private void DeleteSession() {
        OrchestratorController.Instance.DeleteSession(OrchestratorController.Instance.MySession.sessionId);
    }

    private void OnDeleteSessionHandler() {
        Debug.Log("[OrchestratorLogin][OnDeleteSessionHandler] Not implemented");
    }

    private void JoinSession() {
        if (sessionIdDrop.options.Count <= 0)
            Debug.LogError($"[JoinSession] There are no sessions to join.");
        else {
            string sessionIdToJoin = OrchestratorController.Instance.AvailableSessions[sessionIdDrop.value].sessionId;
            OrchestratorController.Instance.JoinSession(sessionIdToJoin);
        }
    }

    private void OnJoinSessionHandler(Session session) {
        if (session != null) {
            isMaster = OrchestratorController.Instance.UserIsMaster;
            // Update the info in LobbyPanel
            sessionNameText.text = session.sessionName;
            sessionDescriptionText.text = session.sessionDescription;
            sessionMasterID = OrchestratorController.Instance.GetUser(session.sessionMaster).userName;

            // Update the list of session users
            UpdateUsersSession(usersSession);

            state = State.Lobby;
            PanelChanger();
        }
        else {
            isMaster = false;
            sessionNameText.text = "";
            sessionDescriptionText.text = "";
            scenarioIdText.text = "";
            sessionNumUsersText.text = "";
            sessionMasterID = "";
            RemoveComponentsFromList(usersSession.transform);
        }
    }

    private void LeaveSession() {
        OrchestratorController.Instance.LeaveSession();
    }

    private void OnLeaveSessionHandler() {
        isMaster = false;
        sessionNameText.text = "";
        sessionDescriptionText.text = "";
        scenarioIdText.text = "";
        sessionNumUsersText.text = "";
        RemoveComponentsFromList(usersSession.transform);

        state = State.Play;
        PanelChanger();
    }

    private void OnUserJoinedSessionHandler(string userID) {
        if (!string.IsNullOrEmpty(userID)) {
            OrchestratorController.Instance.GetUsers();
        }
    }

    private void OnUserLeftSessionHandler(string userID) {
        if (!string.IsNullOrEmpty(userID)) {
            OrchestratorController.Instance.GetUsers();
        }
    }

#endregion

#region Scenarios

    private void GetScenarios() {
        OrchestratorController.Instance.GetScenarios();
    }

    private void OnGetScenariosHandler(Scenario[] scenarios) {
        if (scenarios != null && scenarios.Length > 0) {
            //update the data in the dropdown
            UpdateScenarios(scenarioIdDrop);
        }
    }

#endregion

#region Live

    private void OnGetLivePresenterDataHandler(LivePresenterData liveData) {
        //Debug.Log("[OrchestratorLogin][OnGetLivePresenterDataHandler] Not implemented");
    }

#endregion

#region Users

    private void GetUsers() {
        OrchestratorController.Instance.GetUsers();
    }

    private void OnGetUsersHandler(User[] users) {
        Debug.Log("[OrchestratorLogin][OnGetUsersHandler] Users Updated");

        // Update the sfuData if is in session.
        if (OrchestratorController.Instance.ConnectedUsers != null) {
            for (int i = 0; i < OrchestratorController.Instance.ConnectedUsers.Length; ++i) {
                foreach (User u in users) {
                    if (OrchestratorController.Instance.ConnectedUsers[i].userId == u.userId) {
                        OrchestratorController.Instance.ConnectedUsers[i].sfuData = u.sfuData;
                        OrchestratorController.Instance.ConnectedUsers[i].userData = u.userData;
                    }
                }
            }
        }

        UpdateUsersSession(usersSession);
    }

    private void AddUser() {
        Debug.Log("[OrchestratorLogin][AddUser] Send AddUser registration for user " + userNameRegisterIF.text);
        OrchestratorController.Instance.AddUser(userNameRegisterIF.text, userPasswordRegisterIF.text);
    }

    private void OnAddUserHandler(User user) {
        Debug.Log("[OrchestratorLogin][OnAddUserHandler] User " + user.userName + " registered with exit.");
        loginPanel.SetActive(true);
        signinPanel.SetActive(false);
        userNameLoginIF.text = userNameRegisterIF.text;
    }

    private void UpdateUserData() {
        // UserData info in Config
        UserData lUserData = new UserData {
            userMQexchangeName = Config.Instance.TVMs.exchangeName,
            userMQurl = Config.Instance.TVMs.connectionURI,
            userPCurl = Config.Instance.pointcloudServerURL,
            userAudioUrl = Config.Instance.audioServerURL,
            userRepresentationType = (UserRepresentationType)representationTypeConfigDropdown.value,
            webcamName = (webcamDropdown.options.Count <= 0) ? "None" : webcamDropdown.options[webcamDropdown.value].text,
            microphoneName = (microphoneDropdown.options.Count <= 0) ? "None" : microphoneDropdown.options[microphoneDropdown.value].text
        };
        Debug.Log($"xxxjack OrchestratorLogin.UpdateUserData: userPCurl={lUserData.userPCurl}, userAudioUrl={lUserData.userAudioUrl}");
        OrchestratorController.Instance.UpdateFullUserData(lUserData);
    }

    private void GetUserInfo() {
        OrchestratorController.Instance.GetUserInfo(OrchestratorController.Instance.SelfUser.userId);
    }

    private void OnGetUserInfoHandler(User user) {
        if (user != null) {
            if (string.IsNullOrEmpty(userId.text) || user.userId == OrchestratorController.Instance.SelfUser.userId) {
                OrchestratorController.Instance.SelfUser = user;

                userId.text = user.userId;
                userName.text = user.userName;
                userNameVRTText.text = user.userName;

                //UserData
                tvmExchangeNameConfigIF.text = user.userData.userMQexchangeName;
                tvmConnectionURIConfigIF.text = user.userData.userMQurl;
                representationTypeConfigDropdown.value = (int)user.userData.userRepresentationType;

                SetUserRepresentationGUI(user.userData.userRepresentationType);
                // Session name

#if UNITY_STANDALONE_WIN
                string time = DateTime.Now.ToString("hhmmss");
                sessionNameIF.text = $"{user.userName}_{time}";
#endif

            }

            GetUsers(); // To update the user representation

            // Update the sfuData and UserData if is in session.
            if (OrchestratorController.Instance.ConnectedUsers != null) {
                for (int i = 0; i < OrchestratorController.Instance.ConnectedUsers.Length; ++i) {
                    if (OrchestratorController.Instance.ConnectedUsers[i].userId == user.userId) {
                        // sfuData
                        OrchestratorController.Instance.ConnectedUsers[i].sfuData = user.sfuData;
                        // UserData
                        OrchestratorController.Instance.ConnectedUsers[i].userData = user.userData;
                    }
                }
            }
        }
    }

    private void DeleteUser() {
        Debug.Log("[OrchestratorLogin][DeleteUser] Not implemented");
    }

#endregion

#region Rooms

    private void GetRooms() {
        OrchestratorController.Instance.GetRooms();
    }

    private void OnGetRoomsHandler(RoomInstance[] rooms) {
        Debug.Log("[OrchestratorLogin][OnGetRoomsHandler] Send GetUsers command");

        OrchestratorController.Instance.GetUsers();
    }

    private void JoinRoom() {
        Debug.Log("[OrchestratorLogin][JoinRoom] Not implemented");
    }

    private void OnJoinRoomHandler(bool hasJoined) {
        Debug.Log("[OrchestratorLogin][OnJoinRoomHandler] Not implemented");
    }

    private void LeaveRoom() {
        OrchestratorController.Instance.LeaveRoom();
    }

    private void OnLeaveRoomHandler() {
        Debug.Log("[OrchestratorLogin][OnLeaveRoomHandler] Not implemented");
    }

#endregion

#region Messages

    private void SendMessage() {
        Debug.Log("[OrchestratorLogin][SendMessage] Not implemented");
    }

    private void SendMessageToAll(string message) {
        OrchestratorController.Instance.SendMessageToAll(message);
    }

    private void OnUserMessageReceivedHandler(UserMessage userMessage) {
        AddTextComponentOnContent(logsContainer.transform, "<<< USER MESSAGE RECEIVED: " + userMessage.fromName + "[" + userMessage.fromId + "]: " + userMessage.message);
        StartCoroutine(ScrollLogsToBottom());

        LoginController.Instance.MessageActivation(userMessage.message);
    }

#endregion

#region Events

    private void SendEventToMaster() {
        Debug.Log("[OrchestratorLogin][SendEventToMaster] Not implemented");
    }

    private void SendEventToUser() {
        Debug.Log("[OrchestratorLogin][SendEventToUser] Not implemented");
    }

    private void SendEventToAll() {
        Debug.Log("[OrchestratorLogin][SendEventToAll] Not implemented");
    }

    private void OnMasterEventReceivedHandler(UserEvent pMasterEventData) {
        Debug.Log("[OrchestratorLogin][OnMasterEventReceivedHandler] MASTER EVENT RECEIVED: [" + pMasterEventData.fromId + "]: " + pMasterEventData.message);
    }

    private void OnUserEventReceivedHandler(UserEvent pUserEventData) {
        Debug.Log("[OrchestratorLogin][OnUserEventReceivedHandler] USER EVENT RECEIVED: [" + pUserEventData.fromId + "]: " + pUserEventData.message);
    }

#endregion

#region Data Stream

    private void GetAvailableDataStreams() {
        Debug.Log("[OrchestratorLogin][GetAvailableDataStreams] Not implemented");
    }

    private void GetRegisteredDataStreams() {
        OrchestratorController.Instance.GetRegisteredDataStreams();
    }

#endregion

#region Errors

    private void OnErrorHandler(ResponseStatus status) {
        Debug.Log("[OrchestratorLogin][OnError]::Error code: " + status.Error + "::Error message: " + status.Message);
        ErrorManager.Instance.EnqueueOrchestratorError(status.Error, status.Message);
    }

#endregion

#endregion

#if UNITY_STANDALONE_WIN
    void OnGUI() {
        if (GUI.Button(new Rect(Screen.width / 2, 5, 70, 20), "Open Log")) {
            var log_path = System.IO.Path.Combine(System.IO.Directory.GetParent(Environment.GetEnvironmentVariable("AppData")).ToString(), "LocalLow", Application.companyName, Application.productName, "Player.log");
            Debug.Log(log_path);
            Application.OpenURL(log_path);
        }
    }
#endif
}
