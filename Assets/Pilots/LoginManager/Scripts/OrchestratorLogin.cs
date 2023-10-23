using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.Voice;
using VRT.Core;
using VRT.Pilots.Common;
using static System.Collections.Specialized.BitVector32;
using UnityEngine.Serialization;

namespace VRT.Pilots.LoginManager
{

    public enum State
    {
        Offline, Online, LoggedIn, Config, Play, Create, Join, Lobby, InGame
    }

    public enum AutoState
    {
        DidNone, DidLogIn, DidPlay, DidCreate, DidPartialCreation, DidCompleteCreation, DidJoin, DidStart, Done
    };

    public class OrchestratorLogin : MonoBehaviour
    {
        [Tooltip("Enable experience developer options")]
        [SerializeField] private bool developerMode = true;

        private static OrchestratorLogin instance;
        
        #region GUI Components

        
        private State state = State.Offline;
        private AutoState autoState = AutoState.DidNone;

        
        [Header("Status and DeveloperStatus")]
        [SerializeField] private Toggle developerModeButton = null;
        [SerializeField] private GameObject developerPanel = null;
        [SerializeField] private Text statusText = null;
        [SerializeField][FormerlySerializedAs("userId")] private Text StatusPanelUserId = null;
        [SerializeField][FormerlySerializedAs("userName")] private Text StatusPanelUserName = null;
        [SerializeField][FormerlySerializedAs("orchURLText")] private Text StatusPanelOrchestratorURL = null;
        [SerializeField][FormerlySerializedAs("nativeVerText")] private Text StatusPanelNativeVersion = null;
        [SerializeField][FormerlySerializedAs("playerVerText")] private Text StatusPanelPlayerVersion = null;
        [SerializeField][FormerlySerializedAs("orchVerText")] private Text StatusPanelOrchestratorVersion = null;
        [SerializeField][FormerlySerializedAs("developerSessionButton")] private Button StatusPanelStartDeveloperSceneButton = null;

        [Header("ConnectPanel")]
        [SerializeField] private GameObject connectPanel = null;
      
        [Header("LoginPanel")]
        [SerializeField] private GameObject loginPanel = null;
        [SerializeField] private InputField userNameLoginIF = null;
        [SerializeField] private Button loginButton = null;
        [SerializeField] private Toggle rememberMeButton = null;

        [Header("HomePanel")]
        
        [SerializeField][FormerlySerializedAs("vrtPanel")] private GameObject homePanel = null;
        [SerializeField] private Text userNameVRTText = null;
        [SerializeField] private Button logoutButton = null;
        [SerializeField] private Button playButton = null;
        [SerializeField] private Button configButton = null;

        [Header("SettingsPanel")]
        [SerializeField][FormerlySerializedAs("configPanel")] private GameObject settingsPanel = null;
        [SerializeField] private GameObject webcamInfoGO = null;
        [SerializeField] private InputField tcpPointcloudURLConfigIF = null;
        [SerializeField] private InputField tcpAudioURLConfigIF = null;
        [SerializeField] private Dropdown representationTypeConfigDropdown = null;
        [SerializeField] private Dropdown webcamDropdown = null;
        [SerializeField] private Dropdown microphoneDropdown = null;
        [SerializeField] private RectTransform VUMeter = null;
        [SerializeField][FormerlySerializedAs("saveConfigButton")] private Button SettingsPanelSaveButton = null;
        [SerializeField][FormerlySerializedAs("exitConfigButton")] private Button SettingsPanelBackButton = null;
        [SerializeField] private SelfRepresentationPreview selfRepresentationPreview = null;
        [SerializeField] private Text selfRepresentationDescription = null;

        [Header("PlayPanel")]
        [SerializeField] private GameObject playPanel = null;
        [SerializeField][FormerlySerializedAs("backPlayButton")] private Button PlayPanelBackButton = null;
        [SerializeField][FormerlySerializedAs("createButton")] private Button PlayPanelCreateButton = null;
        [SerializeField][FormerlySerializedAs("joinButton")] private Button PlayPanelJoinButton = null;

        [Header("CreatePanel")]
        [SerializeField] private GameObject createPanel = null;
        [SerializeField][FormerlySerializedAs("backCreateButton")] private Button CreatePanelBackButton = null;
        [SerializeField] private InputField sessionNameIF = null;
        [SerializeField] private InputField sessionDescriptionIF = null;
        [SerializeField] private Dropdown scenarioIdDrop = null;
        [SerializeField] private Text scenarioDescription = null;
        [SerializeField] private Dropdown sessionProtocolDrop = null;
        [SerializeField] private Toggle uncompressedPointcloudsToggle = null;
        [SerializeField] private Toggle uncompressedAudioToggle = null;
        [SerializeField][FormerlySerializedAs("doneCreateButton")] private Button CreatePanelCreateButton = null;

        [Header("JoinPanel")]
        [SerializeField] private GameObject joinPanel = null;
        [SerializeField][FormerlySerializedAs("backJoinButton")] private Button JoinPanelBackButton = null;
        [SerializeField] private Dropdown sessionIdDrop = null;
        [SerializeField] private Text sessionJoinMessage = null;
        [SerializeField][FormerlySerializedAs("doneJoinButton")] private Button JoinPanelJoinButton = null;
        [SerializeField] private RectTransform orchestratorSessions = null;
        [SerializeField] private int refreshTimer = 5;

        [Header("LobbyPanel")]
        [SerializeField] private GameObject lobbyPanel = null;
        [SerializeField] private Text sessionNameText = null;
        [SerializeField] private Text sessionDescriptionText = null;
        [SerializeField] private Text scenarioIdText = null;
        [SerializeField] private Text sessionNumUsersText = null;
        [SerializeField][FormerlySerializedAs("readyButton")] private Button LobbyPanelStartButton = null;
        [SerializeField][FormerlySerializedAs("leaveButton")] private Button LobbyPanelLeaveButton = null;
        [SerializeField] private RectTransform usersSession = null;
        [SerializeField] private Text userRepresentationLobbyText = null;
        [SerializeField] private Image userRepresentationLobbyImage = null;
     
       
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
        private void AddTextComponentOnContent(Transform container, string value)
        {
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

        private void AddUserComponentOnContent(Transform container, User user)
        {
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
           
            switch (user.userData.userRepresentationType)
            {
                case UserRepresentationType.NoRepresentation:
                    imageItem.sprite = Resources.Load<Sprite>("Icons/URNoneIcon");
                    textItem.text += " - (No Rep)";
                    break;
                case UserRepresentationType.VideoAvatar:
                    imageItem.sprite = Resources.Load<Sprite>("Icons/URCamIcon");
                    textItem.text += " - (2D Video)";
                    break;
                case UserRepresentationType.SimpleAvatar:
                    imageItem.sprite = Resources.Load<Sprite>("Icons/URAvatarIcon");
                    textItem.text += " - (3D Avatar)";
                    break;
                case UserRepresentationType.PointCloud:
                   imageItem.sprite = Resources.Load<Sprite>("Icons/URSingleIcon");
                    textItem.text += " - (Simple PC)";
                    break;
               case UserRepresentationType.AudioOnly:
                    imageItem.sprite = Resources.Load<Sprite>("Icons/URNoneIcon");
                    textItem.text += " - (Spectator)";
                    break;
                case UserRepresentationType.NoRepresentationCamera:
                    imageItem.sprite = Resources.Load<Sprite>("Icons/URCameramanIcon");
                    textItem.text += " - (Cameraman)";
                    break;
                default:
                    Debug.LogError($"OrchestratorLogin: Unknown UserRepresentationType {user.userData.userRepresentationType}");
                    break;
            }
        }

        private void RemoveComponentsFromList(Transform container)
        {
            for (var i = container.childCount - 1; i >= 0; i--)
            {
                var obj = container.GetChild(i);
                obj.transform.SetParent(null);
                Destroy(obj.gameObject);
            }
        }

        private void UpdateUsersSession(Transform container)
        {
            RemoveComponentsFromList(usersSession.transform);
            Session session = OrchestratorController.Instance.CurrentSession;
            if (session == null)
            {
                Debug.Log("xxxjack OrchestratorLogin: UpdateUsersSession: no current session");
                return;
            }
            User[] sessionUsers = session.GetUsers();
            foreach (User u in sessionUsers)
            {
                //AddTextComponentOnContent(container.transform, u.userName);
                AddUserComponentOnContent(container.transform, u);
            }
            sessionNumUsersText.text = sessionUsers.Length.ToString() /*+ "/" + "4"*/;
            Debug.Log($"xxxjack OrchestratorLogin: UpdateUsersSession: {sessionUsers.Length} users in session");
            // We may be able to continue auto-starting
            if (VRTConfig.Instance.AutoStart != null)
                Invoke(nameof(AutoStateUpdate), VRTConfig.Instance.AutoStart.autoDelay);
        }

        private void UpdateSessions(Transform container)
        {
            RemoveComponentsFromList(container.transform);
            foreach (var session in OrchestratorController.Instance.AvailableSessions)
            {
                AddTextComponentOnContent(container.transform, session.GetGuiRepresentation());
            }

            string selectedOption = "";
            // store selected option in dropdown
            if (sessionIdDrop.options.Count > 0)
                selectedOption = sessionIdDrop.options[sessionIdDrop.value].text;
            // update the dropdown
            sessionIdDrop.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (var sess in OrchestratorController.Instance.AvailableSessions)
            {
                options.Add(new Dropdown.OptionData(sess.GetGuiRepresentation()));
            }
            sessionIdDrop.AddOptions(options);
            // re-assign selected option in dropdown
            if (sessionIdDrop.options.Count > 0)
            {
                for (int i = 0; i < sessionIdDrop.options.Count; ++i)
                {
                    if (sessionIdDrop.options[i].text == selectedOption)
                        sessionIdDrop.value = i;
                }
            }
            SessionSelectionChanged();
        }

        private void UpdateScenarios()
        {
            // update the dropdown
            scenarioIdDrop.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (var sc in ScenarioRegistry.Instance.Scenarios)
            {
                options.Add(new Dropdown.OptionData(sc.scenarioName));
            }

            scenarioIdDrop.AddOptions(options);
            ScenarioSelectionChanged();
        }

        private void ScenarioSelectionChanged()
        {
            var idx = scenarioIdDrop.value;
            bool ok = false;
            string message = "(no scenario selected)";
            var scenarios = ScenarioRegistry.Instance.Scenarios;
            if (idx >= 0 && idx < scenarios.Count)
            {
                var sc = scenarios[idx];
                // Empty entries can be used as separators
                if (!string.IsNullOrEmpty(sc.scenarioId))
                {
                    ok = true;
                    message = sc.scenarioDescription;

                }
            }
            if (scenarioDescription != null)
            {
                scenarioDescription.text = message;
            }
            CreatePanelCreateButton.interactable = ok;
        }

        private void UpdateRepresentations(Dropdown dd)
        {
            // Fill UserData representation dropdown according to UserRepresentationType enum declaration
            // xxxjack this has the huge disadvantage that they are numerically sorted.
            // xxxjack and the order is difficult to change currently, because the values
            // xxxjack are stored by the orchestrator in the user record, in numerical form...
            dd.ClearOptions();
            dd.AddOptions(new List<string>(Enum.GetNames(typeof(UserRepresentationType))));
            
        }

        private void UpdateProtocols()
        {
            sessionProtocolDrop.ClearOptions();
            List<string> names = new List<string>();
            foreach(string protocolName in Enum.GetNames(typeof(SessionConfig.ProtocolType))) {
                if(protocolName == "None") continue;
                names.Add(protocolName);
            }
            sessionProtocolDrop.AddOptions(names);
            sessionProtocolDrop.value = 0;
        }

        private void UpdateWebcams(Dropdown dd)
        {
            // Fill UserData representation dropdown according to UserRepresentationType enum declaration
            dd.ClearOptions();
            WebCamDevice[] devices = WebCamTexture.devices;
            List<string> webcams = new List<string>();
            webcams.Add("None");
            foreach (WebCamDevice device in devices)
                webcams.Add(device.name);
            dd.AddOptions(webcams);
        }

        private void Updatemicrophones(Dropdown dd)
        {
            // Fill UserData representation dropdown according to UserRepresentationType enum declaration
            dd.ClearOptions();
            string[] devices = Microphone.devices;
            List<string> microphones = new List<string>();
            microphones.Add("None");
            foreach (string device in devices)
                microphones.Add(device);
            dd.AddOptions(microphones);
        }

        private void SetUserRepresentationGUI(UserRepresentationType _representationType)
        {
            userRepresentationLobbyText.text = _representationType.ToString();
            // left change the icon 'userRepresentationLobbyImage'
         
            switch (_representationType)
            {
                case UserRepresentationType.NoRepresentation:
                case UserRepresentationType.AudioOnly:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URNoneIcon");
                    break;
                case UserRepresentationType.VideoAvatar:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URCamIcon");
                    break;
                case UserRepresentationType.SimpleAvatar:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URAvatarIcon");
                    break;
                case UserRepresentationType.PointCloud:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URSingleIcon");
                    break;
                 case UserRepresentationType.NoRepresentationCamera:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URCameramanIcon");
                    break;
              
            }
        }

        private void SetUserRepresentationDescription(UserRepresentationType _representationType)
        {

            // left change the icon 'userRepresentationLobbyImage'
            switch (_representationType)
            {
                case UserRepresentationType.NoRepresentation:
                    selfRepresentationDescription.text = "No representation, no audio. The user can only watch.";
                    break;
                case UserRepresentationType.VideoAvatar:
                    selfRepresentationDescription.text = "Avatar with video window from your camera.";
                    break;
                case UserRepresentationType.SimpleAvatar:
                    selfRepresentationDescription.text = "3D Synthetic Avatar.";
                    break;
                case UserRepresentationType.PointCloud:
                    selfRepresentationDescription.text = "Realistic point cloud user representation, captured live.";
                    break;
                case UserRepresentationType.AudioOnly:
                    selfRepresentationDescription.text = "No visual representation, only audio communication.";
                    break;
                case UserRepresentationType.NoRepresentationCamera:
                    selfRepresentationDescription.text = "Local video recorder.";
                    break;
                default:
                    Debug.LogError($"OrchestratorLogin: Unknown UserRepresentationType {_representationType}");
                    break;
            }
        }

#endregion

#region Unity

        // Start is called before the first frame update
        void Start()
        {
            if (instance == null)
            {
                instance = this;
            }

            AsyncVoiceReader.PrepareDSP(VRTConfig.Instance.audioSampleRate, 0);

            system = EventSystem.current;
            // Developer mode settings
            developerMode = PlayerPrefs.GetInt("developerMode", 0) != 0;
            developerModeButton.isOn = developerMode;
            // Update Application version
            StatusPanelOrchestratorURL.text = VRTConfig.Instance.orchestratorURL;
            if (VersionLog.Instance != null) StatusPanelNativeVersion.text = VersionLog.Instance.NativeClient;
            StatusPanelPlayerVersion.text = "v" + Application.version;
            StatusPanelOrchestratorVersion.text = "";

            // Font to build gui components for logs!
            //MenuFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

            // Fill scenarios
            UpdateScenarios();

            // Fill UserData representation dropdown according to UserRepresentationType enum declaration
            UpdateRepresentations(representationTypeConfigDropdown);
            UpdateWebcams(webcamDropdown);
            Updatemicrophones(microphoneDropdown);

            // Buttons listeners
            developerModeButton.onValueChanged.AddListener(delegate { DeveloperModeButtonClicked(); });
            StatusPanelStartDeveloperSceneButton.onClick.AddListener(delegate { StartDeveloperSession(); });
            loginButton.onClick.AddListener(delegate { Login(); });
            logoutButton.onClick.AddListener(delegate { Logout(); });
            playButton.onClick.AddListener(delegate { StateButton(State.Play); });
            configButton.onClick.AddListener(delegate {
                FillSelfUserData();
                StateButton(State.Config);
            });
            SettingsPanelSaveButton.onClick.AddListener(delegate { SaveConfigButton(); });
            SettingsPanelBackButton.onClick.AddListener(delegate { ExitConfigButton(); });
            PlayPanelBackButton.onClick.AddListener(delegate { StateButton(State.LoggedIn); });
            PlayPanelCreateButton.onClick.AddListener(delegate { StateButton(State.Create); });
            PlayPanelJoinButton.onClick.AddListener(delegate { StateButton(State.Join); });
            CreatePanelBackButton.onClick.AddListener(delegate { StateButton(State.Play); });
            CreatePanelCreateButton.onClick.AddListener(delegate { AddSession(); });
            JoinPanelBackButton.onClick.AddListener(delegate { StateButton(State.Play); });
            JoinPanelJoinButton.onClick.AddListener(delegate { JoinSession(); });
            LobbyPanelStartButton.onClick.AddListener(delegate { ReadyButton(); });
            LobbyPanelLeaveButton.onClick.AddListener(delegate { LeaveSession(); });

            // Dropdown listeners
            representationTypeConfigDropdown.onValueChanged.AddListener(delegate { PanelChanger(); });
            webcamDropdown.onValueChanged.AddListener(delegate { PanelChanger(); });
            microphoneDropdown.onValueChanged.AddListener(delegate {
                selfRepresentationPreview.ChangeMicrophone(microphoneDropdown.options[microphoneDropdown.value].text);
            });
            scenarioIdDrop.onValueChanged.AddListener(delegate { ScenarioSelectionChanged(); });

            sessionIdDrop.onValueChanged.AddListener(delegate { SessionSelectionChanged(); });

            InitialiseControllerEvents();

            UpdateProtocols();
            uncompressedPointcloudsToggle.isOn = SessionConfig.Instance.pointCloudCodec == "cwi0";
            uncompressedAudioToggle.isOn = SessionConfig.Instance.voiceCodec == "VR2a";

            if (OrchestratorController.Instance.UserIsLogged)
            { // Comes from another scene
              // Set status to online
                statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
                statusText.color = connectedCol;
                FillSelfUserData();
                UpdateSessions(orchestratorSessions);
                UpdateScenarios();
                Debug.Log("OrchestratorLogin: Coming from another Scene");

                OrchestratorController.Instance.OnLoginResponse(new ResponseStatus(), StatusPanelUserId.text);
            }
            else
            { // Enter for first time
              // Set status to offline
                statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
                statusText.color = disconnectedCol;
                state = State.Offline;

                // Try to connect
                SocketConnect();
            }
        }

      
        // Update is called once per frame
        void Update()
        {
            if (VUMeter && selfRepresentationPreview)
                VUMeter.sizeDelta = new Vector2(355 * Mathf.Min(1, selfRepresentationPreview.MicrophoneLevel), 20);

            TabShortcut();
          
            // Refresh Sessions
            if (state == State.Join)
            {
                timer += Time.deltaTime;
                if (timer >= refreshTimer)
                {
                    GetSessions();
                    timer = 0.0f;
                }
            }
        }

        void AutoStateUpdate()
        {
            VRTConfig._AutoStart config = VRTConfig.Instance.AutoStart;
            if (config == null) return;
            if (
                    Keyboard.current.shiftKey.isPressed

                ) return;
            if (state == State.Play && autoState == AutoState.DidPlay)
            {
                if (config.autoCreate)
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: starting");
                    autoState = AutoState.DidCreate;
                    StateButton(State.Create);

                }
                if (config.autoJoin)
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoJoin: starting");
                    autoState = AutoState.DidJoin;
                    StateButton(State.Join);
                }
            }
            if (state == State.Create && autoState == AutoState.DidCreate)
            {
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: sessionName={config.sessionName}");
                sessionNameIF.text = config.sessionName;
                uncompressedPointcloudsToggle.isOn = config.sessionUncompressed;
                uncompressedAudioToggle.isOn = config.sessionUncompressedAudio;
                if (config.sessionTransportProtocol != null && config.sessionTransportProtocol != "")
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: sessionTransportProtocol={config.sessionTransportProtocol}");
                    // xxxjack I don't understand the intended logic behind the toggles. But turning everything
                    // on and then simulating a button callback works.
                    
                    SetProtocol(config.sessionTransportProtocol);
                }
                else
                {
                    SetProtocol("socketio");
                }
                autoState = AutoState.DidPartialCreation;
            }
            if (state == State.Create && autoState == AutoState.DidPartialCreation && scenarioIdDrop.options.Count > 0)
            {
                if (config.sessionScenario != null && config.sessionScenario != "")
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: sessionScenario={config.sessionScenario}");
                    bool found = false;
                    int idx = 0;
                    foreach (var entry in scenarioIdDrop.options)
                    {
                        if (entry.text == config.sessionScenario)
                        {
                            scenarioIdDrop.value = idx;
                            found = true;
                        }
                        idx++;
                    }
                    if (!found)
                    {
                        Debug.LogError($"OrchestratorLogin: AutoStart: No scenarios match {config.sessionScenario}");

                    }
                }
                if (config.autoCreate)
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: creating");
                    Invoke(nameof(AddSession), config.autoDelay);
                }
                autoState = AutoState.DidCompleteCreation;

            }
            if (state == State.Lobby && autoState == AutoState.DidCompleteCreation && config.autoStartWith >= 1)
            {
                if (sessionNumUsersText.text == config.autoStartWith.ToString())
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: starting with {config.autoStartWith} users");
                    Invoke(nameof(ReadyButton), config.autoDelay);
                    autoState = AutoState.Done;
                }
            }
            if (state == State.Join && autoState == AutoState.DidJoin)
            {
                var options = sessionIdDrop.options;
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autojoin: look for {config.sessionName}");
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].text.StartsWith(config.sessionName + " "))
                    {
                        if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autojoin: entry {i} is {config.sessionName}, joining");
                        sessionIdDrop.value = i;
                        autoState = AutoState.Done;
                        Invoke(nameof(JoinSession), config.autoDelay);
                    }
                }
            }
        }

        public void FillSelfUserData()
        {
            if (OrchestratorController.Instance == null || OrchestratorController.Instance.SelfUser == null)
            {
                Debug.LogWarning($"OrchestratorLogin: FillSelfUserData: no SelfUser data yet");
            }
            User user = OrchestratorController.Instance.SelfUser;

            // UserID & Name
            StatusPanelUserId.text = user.userId;
            StatusPanelUserName.text = user.userName;
            userNameVRTText.text = user.userName;
            // Config Info
            UserData userData = user.userData;
            tcpPointcloudURLConfigIF.text = userData.userPCurl;
            tcpAudioURLConfigIF.text = userData.userAudioUrl;
            representationTypeConfigDropdown.value = (int)userData.userRepresentationType;
            webcamDropdown.value = 0;

            for (int i = 0; i < webcamDropdown.options.Count; ++i)
            {
                if (webcamDropdown.options[i].text == userData.webcamName)
                {
                    webcamDropdown.value = i;
                    break;
                }
            }
            microphoneDropdown.value = 0;
            for (int i = 0; i < microphoneDropdown.options.Count; ++i)
            {
                if (microphoneDropdown.options[i].text == userData.microphoneName)
                {
                    microphoneDropdown.value = i;
                    break;
                }
            }
        }

        public void PanelChanger()
        {
            // Get the user name (if we have one) it is used to initialize various fields.
            string uname = OrchestratorController.Instance?.SelfUser?.userName;

            developerPanel.SetActive(developerMode);
            connectPanel.gameObject.SetActive(state == State.Offline);
            loginPanel.SetActive(state == State.Online);
            homePanel.SetActive(state == State.LoggedIn);
            settingsPanel.SetActive(state == State.Config);
            playPanel.SetActive(state == State.Play);
            createPanel.SetActive(state == State.Create);
            joinPanel.SetActive(state == State.Join);
            lobbyPanel.SetActive(state == State.Lobby);
            // Buttons
            switch (state)
            {
                case State.Offline:
                    break;
                case State.Online:
                    CheckRememberMe();
                    break;
                case State.LoggedIn:
                    userNameVRTText.text = uname;
                    StatusPanelUserName.text = uname;
                    break;
                case State.Config:
                   
                    // Behaviour
                    SelfRepresentationChanger();
                    break;
                case State.Play:
                    
                    break;
                case State.Create:
                    if (string.IsNullOrEmpty(sessionNameIF.text))
                    {
                         string time = DateTime.Now.ToString("hhmmss");
                        sessionNameIF.text = $"{uname}_{time}";
                    }
                    break;
                case State.Join:
                    // Behaviour
                    GetSessions();
                    break;
                case State.Lobby:
                    LobbyPanelStartButton.gameObject.SetActive(OrchestratorController.Instance.UserIsMaster);
                    break;
                case State.InGame:
                    break;
                default:
                    break;
            }
            SelectFirstIF();
        }

        public void SelfRepresentationChanger()
        {
            // Dropdown Logic
            webcamInfoGO.SetActive(false);
          
         
            if ((UserRepresentationType)representationTypeConfigDropdown.value == UserRepresentationType.VideoAvatar)
            {
                webcamInfoGO.SetActive(true);
            }
            // Preview
            SetUserRepresentationDescription((UserRepresentationType)representationTypeConfigDropdown.value);
            selfRepresentationPreview.ChangeRepresentation((UserRepresentationType)representationTypeConfigDropdown.value,
                webcamDropdown.options[webcamDropdown.value].text);
            selfRepresentationPreview.ChangeMicrophone(microphoneDropdown.options[microphoneDropdown.value].text);
        }

        private void OnDestroy()
        {
            TerminateControllerEvents();
        }

#endregion

#region Input

        void SelectFirstIF()
        {
            try
            {
                InputField[] inputFields = FindObjectsOfType<InputField>();
                if (inputFields != null)
                {
                    inputFields[inputFields.Length - 1].OnPointerClick(new PointerEventData(system));  //if it's an input field, also set the text caret
                    inputFields[inputFields.Length - 1].caretWidth = 2;
                    //system.SetSelectedGameObject(first.gameObject, new BaseEventData(system));
                }
            }
            catch { }
        }

        void TabShortcut()
        {
            if (
            Keyboard.current.tabKey.wasPressedThisFrame
                )
            {
                try
                {
                    Selectable current = system.currentSelectedGameObject.GetComponent<Selectable>();
                    if (current != null)
                    {
                        Selectable next = current.FindSelectableOnDown();
                        if (next != null)
                        {
                            InputField inputfield = next.GetComponent<InputField>();
                            if (inputfield != null)
                            {
                                inputfield.OnPointerClick(new PointerEventData(system));  //if it's an input field, also set the text caret
                                inputfield.caretWidth = 2;
                            }

                            system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
                        }
                        else
                        {
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

        private void DeveloperModeButtonClicked()
        {
            developerMode = developerModeButton.isOn;
            PlayerPrefs.SetInt("developerMode", developerMode?1:0);
            PanelChanger();
        }

        private void StartDeveloperSession()
        {
            PilotController.LoadScene("SoloPlayground");
        }

        public void SaveConfigButton()
        {
            selfRepresentationPreview.StopMicrophone();
            UpdateUserData();
            state = State.LoggedIn;
            PanelChanger();
        }

        public void ExitConfigButton()
        {
            selfRepresentationPreview.StopMicrophone();
            state = State.LoggedIn;
            PanelChanger();
        }

        public void StateButton(State _state)
        {
            state = _state;
            PanelChanger();
            if (state == State.Config)
            {
                UpdateUserData();
            }
        }

        public void ReadyButton()
        {
            SessionConfig cfg = SessionConfig.Instance;
            cfg.scenarioName = OrchestratorController.Instance.CurrentScenario.scenarioName;
            cfg.scenarioVariant = null;
            // protocolType already set
            // pointCloudCodec, voiceCodec and videoCodec already set
            string message = JsonUtility.ToJson(cfg);
            SendMessageToAll("START_" + message);
        }

     

#endregion

#region Toggles 

      

        public void SetCompression()
        {
            if (uncompressedPointcloudsToggle.isOn)
            {
                SessionConfig.Instance.pointCloudCodec = "cwi0";
            }
            else
            {
                SessionConfig.Instance.pointCloudCodec = "cwi1";
            }
            if (uncompressedAudioToggle.isOn)
            {
                SessionConfig.Instance.voiceCodec = "VR2a";
            }
            else
            {
                SessionConfig.Instance.voiceCodec = "VR2A";
            }
        }

        public void SetProtocol(string protoString)
        {
            if (string.IsNullOrEmpty(protoString))
            {
                // Empty string means we're called from the dropdown callback. Get the value from there.
                protoString = sessionProtocolDrop.options[sessionProtocolDrop.value].text;
            }
            SessionConfig.ProtocolType proto = SessionConfig.ProtocolFromString(protoString);
            bool done = false;
            for (int i = 0; i < sessionProtocolDrop.options.Count; i++)
            {
                if (protoString.ToLower() == sessionProtocolDrop.options[i].text.ToLower())
                {
                    done = true;
                    sessionProtocolDrop.value = i;
                }
            }
            if (!done)
            {
                Debug.LogError($"OrchestratorLogin: unknown protocol \"protoString\"");
            }
            
            SessionConfig.Instance.protocolType = proto;
        }



#endregion

#region Events listeners

        // Subscribe to Orchestrator Wrapper Events
        private void InitialiseControllerEvents()
        {
            OrchestratorController.Instance.OnConnectionEvent += OnConnect;
            OrchestratorController.Instance.OnConnectingEvent += OnConnecting;
            OrchestratorController.Instance.OnConnectionEvent += OnDisconnect;
            OrchestratorController.Instance.OnGetOrchestratorVersionEvent += OnGetOrchestratorVersionHandler;
            OrchestratorController.Instance.OnLoginEvent += OnLogin;
            OrchestratorController.Instance.OnLogoutEvent += OnLogout;
            OrchestratorController.Instance.OnGetNTPTimeEvent += OnGetNTPTimeResponse;
            OrchestratorController.Instance.OnSessionsEvent += OnSessionsHandler;
            OrchestratorController.Instance.OnAddSessionEvent += OnAddSessionHandler;
            OrchestratorController.Instance.OnSessionInfoEvent += OnSessionInfoHandler;
            OrchestratorController.Instance.OnJoinSessionEvent += OnJoinSessionHandler;
            OrchestratorController.Instance.OnLeaveSessionEvent += OnLeaveSessionHandler;
            OrchestratorController.Instance.OnDeleteSessionEvent += OnDeleteSessionHandler;
            OrchestratorController.Instance.OnUserJoinSessionEvent += OnUserJoinedSessionHandler;
            OrchestratorController.Instance.OnUserLeaveSessionEvent += OnUserLeftSessionHandler;

            OrchestratorController.Instance.OnUserMessageReceivedEvent += OnUserMessageReceivedHandler;
            OrchestratorController.Instance.OnMasterEventReceivedEvent += OnMasterEventReceivedHandler;
            OrchestratorController.Instance.OnUserEventReceivedEvent += OnUserEventReceivedHandler;
            OrchestratorController.Instance.OnErrorEvent += OnErrorHandler;
        }

        // Un-Subscribe to Orchestrator Wrapper Events
        private void TerminateControllerEvents()
        {
            OrchestratorController.Instance.OnConnectionEvent -= OnConnect;
            OrchestratorController.Instance.OnConnectingEvent -= OnConnecting;
            OrchestratorController.Instance.OnConnectionEvent -= OnDisconnect;
            OrchestratorController.Instance.OnGetOrchestratorVersionEvent -= OnGetOrchestratorVersionHandler;
            OrchestratorController.Instance.OnLoginEvent -= OnLogin;
            OrchestratorController.Instance.OnLogoutEvent -= OnLogout;
            OrchestratorController.Instance.OnGetNTPTimeEvent -= OnGetNTPTimeResponse;
            OrchestratorController.Instance.OnSessionsEvent -= OnSessionsHandler;
            OrchestratorController.Instance.OnAddSessionEvent -= OnAddSessionHandler;
            OrchestratorController.Instance.OnSessionInfoEvent -= OnSessionInfoHandler;
            OrchestratorController.Instance.OnJoinSessionEvent -= OnJoinSessionHandler;
            OrchestratorController.Instance.OnLeaveSessionEvent -= OnLeaveSessionHandler;
            OrchestratorController.Instance.OnDeleteSessionEvent -= OnDeleteSessionHandler;
            OrchestratorController.Instance.OnUserJoinSessionEvent -= OnUserJoinedSessionHandler;
            OrchestratorController.Instance.OnUserLeaveSessionEvent -= OnUserLeftSessionHandler;

            OrchestratorController.Instance.OnUserMessageReceivedEvent -= OnUserMessageReceivedHandler;
            OrchestratorController.Instance.OnMasterEventReceivedEvent -= OnMasterEventReceivedHandler;
            OrchestratorController.Instance.OnUserEventReceivedEvent -= OnUserEventReceivedHandler;
            OrchestratorController.Instance.OnErrorEvent -= OnErrorHandler;
        }

#endregion

#region Commands

#region Socket.io connect

        public void SocketConnect()
        {
            switch (OrchestratorController.Instance.ConnectionStatus)
            {
                case OrchestratorController.orchestratorConnectionStatus.__DISCONNECTED__:
                    OrchestratorController.Instance.SocketConnect(VRTConfig.Instance.orchestratorURL);
                    break;
                case OrchestratorController.orchestratorConnectionStatus.__CONNECTING__:
                    OrchestratorController.Instance.Abort();
                    break;
            }
        }

        private void OnConnect(bool pConnected)
        {
            if (pConnected)
            {
                statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
                statusText.color = connectedCol;
                state = State.Online;
            }
            PanelChanger();
            if (pConnected && autoState == AutoState.DidNone && VRTConfig.Instance.AutoStart != null && VRTConfig.Instance.AutoStart.autoLogin)
            {
                if (
                    Keyboard.current.shiftKey.isPressed

                    ) return;
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoLogin");
                autoState = AutoState.DidLogIn;
                Login();
            }
        }

        private void OnConnecting()
        {
            statusText.text = OrchestratorController.orchestratorConnectionStatus.__CONNECTING__.ToString();
            statusText.color = connectingCol;
        }

        private void OnDisconnect(bool pConnected)
        {
            if (!pConnected)
            {
                OnLogout(true);
                statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
                statusText.color = disconnectedCol;
                state = State.Offline;
            }
            PanelChanger();
        }

        private void OnGetOrchestratorVersionHandler(string pVersion)
        {
            Debug.Log("Orchestration Service: " + pVersion);
            StatusPanelOrchestratorVersion.text = pVersion;
            OrchestratorController.Instance.GetNTPTime();
        }

#endregion

      
#region Login/Logout

        // Login from the main buttons Login & Logout
        private void Login()
        {
            if (rememberMeButton.isOn)
            {
                PlayerPrefs.SetString("userNameLoginIF", userNameLoginIF.text);
            }
            else
            {
                PlayerPrefs.DeleteKey("userNameLoginIF");
            }
            // If we want to autoCreate or autoStart depending on username set the right config flags.
            if (VRTConfig.Instance.AutoStart != null && VRTConfig.Instance.AutoStart.autoCreateForUser != "")
            {
                bool isThisUser = VRTConfig.Instance.AutoStart.autoCreateForUser == userNameLoginIF.text;
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: user={userNameLoginIF.text} autoCreateForUser={VRTConfig.Instance.AutoStart.autoCreateForUser} isThisUser={isThisUser}");
                VRTConfig.Instance.AutoStart.autoCreate = isThisUser;
                VRTConfig.Instance.AutoStart.autoJoin = !isThisUser;
            }
            OrchestratorController.Instance.Login(userNameLoginIF.text, "");
        }
        // Check saved used credentials.
        private void CheckRememberMe()
        {
            if (PlayerPrefs.HasKey("userNameLoginIF") && PlayerPrefs.HasKey("userPasswordLoginIF"))
            {
                rememberMeButton.isOn = true;
                userNameLoginIF.text = PlayerPrefs.GetString("userNameLoginIF");
            }
            else
                rememberMeButton.isOn = false;
        }

        private void OnLogin(bool userLoggedSucessfully)
        {
            if (userLoggedSucessfully)
            {
                // Load locally save user data
                if (!String.IsNullOrEmpty(VRTConfig.Instance.LocalUser.orchestratorConfigFilename))
                {
                    var fullName = VRTConfig.ConfigFilename(VRTConfig.Instance.LocalUser.orchestratorConfigFilename);
                    if (System.IO.File.Exists(fullName))
                    {
                        Debug.Log($"OrchestratorLogin: load UserData from {fullName}");
                        var configData = System.IO.File.ReadAllText(fullName);
                        UserData lUserData = new UserData();
                        JsonUtility.FromJsonOverwrite(configData, lUserData);
                        OrchestratorController.Instance.SelfUser.userData = lUserData;
                        // Also send to orchestrator. This is mainly so that the orchestrator can tell
                        // other participants our self-representation.
                        OrchestratorController.Instance.UpdateFullUserData(lUserData);
                        Debug.Log($"OrchestratorLogin: uploaded UserData to orchestrator");
                    }
                }

                state = State.LoggedIn;
            }
            else
            {
                this.StatusPanelUserId.text = "";
                StatusPanelUserName.text = "";
                userNameVRTText.text = "";

                state = State.Online;
            }

            PanelChanger();
            if (userLoggedSucessfully
                && autoState == AutoState.DidLogIn
                && VRTConfig.Instance.AutoStart != null
                && (VRTConfig.Instance.AutoStart.autoCreate || VRTConfig.Instance.AutoStart.autoJoin)
                )
            {
                if (
                    Keyboard.current.shiftKey.isPressed

                    ) return;
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate {VRTConfig.Instance.AutoStart.autoCreate} autoJoin {VRTConfig.Instance.AutoStart.autoJoin}");
                autoState = AutoState.DidPlay;
                StateButton(State.Play);
                Invoke(nameof(AutoStateUpdate), VRTConfig.Instance.AutoStart.autoDelay);
            }
        }

        private void Logout()
        {
            OrchestratorController.Instance.Logout();
        }

        private void OnLogout(bool userLogoutSucessfully)
        {
            if (userLogoutSucessfully)
            {
                StatusPanelUserId.text = "";
                StatusPanelUserName.text = "";
                userNameVRTText.text = "";
                state = State.Online;
            }
            PanelChanger();
        }

#endregion

#region NTP clock

        private void OnGetNTPTimeResponse(NtpClock ntpTime)
        {
            double difference = Helper.GetClockTimestamp(DateTime.UtcNow) - ntpTime.Timestamp;
            if (developerMode) Debug.Log("OrchestratorLogin: OnGetNTPTimeResponse: Difference: " + difference);
            if (Math.Abs(difference) >= VRTConfig.Instance.ntpSyncThreshold)
            {
                Debug.LogError($"This machine has a desynchronization of {difference:F3} sec with the Orchestrator.\nThis is greater than {VRTConfig.Instance.ntpSyncThreshold:F3}.\nYou may suffer some problems as a result.");
            }
        }

#endregion

#region Sessions

        private void GetSessions()
        {
            OrchestratorController.Instance.GetSessions();
        }

        private void OnSessionsHandler(Session[] sessions)
        {
            if (sessions != null)
            {
                // update the list of available sessions
                UpdateSessions(orchestratorSessions);
                // We may be able to advance auto-connection
                if (VRTConfig.Instance.AutoStart != null)
                    Invoke(nameof(AutoStateUpdate), VRTConfig.Instance.AutoStart.autoDelay);
            }
        }

        private void AddSession()
        {
            string protocol = SessionConfig.ProtocolToString(SessionConfig.Instance.protocolType);

            ScenarioRegistry.ScenarioInfo scenarioInfo = ScenarioRegistry.Instance.Scenarios[scenarioIdDrop.value];
            Scenario scenario = scenarioInfo.AsScenario();
            OrchestratorController.Instance.AddSession(scenarioInfo.scenarioId,
                                                        scenario,
                                                        sessionNameIF.text,
                                                        sessionDescriptionIF.text,
                                                        protocol);
        }

        private void OnAddSessionHandler(Session session)
        {
            // Is equal to AddSession + Join Session, except that session is returned (not on JoinSession)
            if (session != null)
            {
                // update the list of available sessions
                UpdateSessions(orchestratorSessions);

                // Update the info in LobbyPanel
                sessionNameText.text = session.sessionName;
                sessionDescriptionText.text = session.sessionDescription;
          
                // Update the list of session users
                UpdateUsersSession(usersSession);

                state = State.Lobby;
                PanelChanger();
                // We may be able to advance auto-connection
                if (VRTConfig.Instance.AutoStart != null)
                    Invoke(nameof(AutoStateUpdate), VRTConfig.Instance.AutoStart.autoDelay);
            }
            else
            {
                sessionNameText.text = "";
                sessionDescriptionText.text = "";
                scenarioIdText.text = "";
                sessionNumUsersText.text = "";
                RemoveComponentsFromList(usersSession.transform);
            }
        }

        private void OnSessionInfoHandler(Session session)
        {
            if (session != null)
            {
                // Update the info in LobbyPanel
                sessionNameText.text = session.sessionName;
                sessionDescriptionText.text = session.sessionDescription;
                // Update the list of session users
                UpdateUsersSession(usersSession);
            }
            else
            {
                sessionNameText.text = "";
                sessionDescriptionText.text = "";
                scenarioIdText.text = "";
                sessionNumUsersText.text = "";
                RemoveComponentsFromList(usersSession.transform);
            }
        }
        private void OnDeleteSessionHandler()
        {
            if (developerMode) Debug.Log("OrchestratorLogin: OnDeleteSessionHandler: Session deleted");
        }

        private void SessionSelectionChanged()
        {
            var idx = sessionIdDrop.value;
            string description = "";
            bool ok = idx >= 0 && idx < OrchestratorController.Instance.AvailableSessions.Length;
            if (ok)
            {
                var sessionSelected = OrchestratorController.Instance.AvailableSessions[idx];
                var scenarioSelected = sessionSelected.scenarioId;
                var sessionMaster = sessionSelected.sessionMaster;
                var masterUser = sessionSelected.GetUser(sessionMaster);
                var masterName = masterUser == null ? sessionMaster : masterUser.userName;
                var scenarioInfo = ScenarioRegistry.Instance.GetScenarioById(scenarioSelected);
                description = $"{sessionSelected.sessionName} by {masterName}\n{sessionSelected.sessionDescription}\n";
                if (scenarioInfo == null)
                {
                    description += "Cannot join: not implemented in this VR2Gather player.";
                    ok = false;
                }
                else
                {
                    description += scenarioInfo.scenarioDescription;
                }
          }
            else
            {
                description = "(no session selected)";
            }
            sessionJoinMessage.text = description;
            JoinPanelJoinButton.interactable = ok;
        }

        private void JoinSession()
        {
            if (sessionIdDrop.options.Count <= 0)
                Debug.LogError($"JoinSession: There are no sessions to join.");
            else
            {
                string sessionIdToJoin = OrchestratorController.Instance.AvailableSessions[sessionIdDrop.value].sessionId;
                OrchestratorController.Instance.JoinSession(sessionIdToJoin);
            }
        }

        private void OnJoinSessionHandler(Session session)
        {
            if (session != null)
            {
                // Update the info in LobbyPanel
                sessionNameText.text = session.sessionName;
                sessionDescriptionText.text = session.sessionDescription;

                // Update the list of session users
                UpdateUsersSession(usersSession);

                state = State.Lobby;
                PanelChanger();
            }
            else
            {
                sessionNameText.text = "";
                sessionDescriptionText.text = "";
                scenarioIdText.text = "";
                sessionNumUsersText.text = "";
                RemoveComponentsFromList(usersSession.transform);
            }
        }

        private void LeaveSession()
        {
            OrchestratorController.Instance.LeaveSession();
        }

        private void OnLeaveSessionHandler()
        {
            sessionNameText.text = "";
            sessionDescriptionText.text = "";
            scenarioIdText.text = "";
            sessionNumUsersText.text = "";
            RemoveComponentsFromList(usersSession.transform);

            state = State.Play;
            PanelChanger();
        }

        private void OnUserJoinedSessionHandler(string userID)
        {
        }

        private void OnUserLeftSessionHandler(string userID)
        {
        }

#endregion


#region Users


        private void UpdateUserData()
        {
            // UserData info in Config
            UserData lUserData = new UserData
            {
                userPCurl = tcpPointcloudURLConfigIF.text,
                userAudioUrl = tcpAudioURLConfigIF.text,
                userRepresentationType = (UserRepresentationType)representationTypeConfigDropdown.value,
                webcamName = (webcamDropdown.options.Count <= 0) ? "None" : webcamDropdown.options[webcamDropdown.value].text,
                microphoneName = (microphoneDropdown.options.Count <= 0) ? "None" : microphoneDropdown.options[microphoneDropdown.value].text
            };
            // And also save a local copy, if wanted
            if (!String.IsNullOrEmpty(VRTConfig.Instance.LocalUser.orchestratorConfigFilename))
            {
                var configData = lUserData.AsJsonString();
                var fullName = VRTConfig.ConfigFilename(VRTConfig.Instance.LocalUser.orchestratorConfigFilename);
                System.IO.File.WriteAllText(fullName, configData);
                Debug.Log($"OrchestratorLogin: saved UserData to {fullName}");
            }
        }


#endregion

#region Messages


        private void SendMessageToAll(string message)
        {
            OrchestratorController.Instance.SendMessageToAll(message);
        }

        private void OnUserMessageReceivedHandler(UserMessage userMessage)
        {
            LoginController.Instance.OnUserMessageReceived(userMessage.message);
        }

#endregion

        private void OnMasterEventReceivedHandler(UserEvent pMasterEventData)
        {
            Debug.LogError("OrchestratorLogin: OnMasterEventReceivedHandler: Unexpected message from " + pMasterEventData.fromId + ": " + pMasterEventData.message);
        }

        private void OnUserEventReceivedHandler(UserEvent pUserEventData)
        {
            Debug.LogError("OrchestratorLogin: OnUserEventReceivedHandler: Unexpected message from " + pUserEventData.fromId + ": " + pUserEventData.message);
        }
#endregion


#region Errors

        private void OnErrorHandler(ResponseStatus status)
        {
            Debug.Log("OrchestratorLogin: OnError: Error code: " + status.Error + ", Error message: " + status.Message);
            ErrorManager.Instance.EnqueueOrchestratorError(status.Error, status.Message);
        }

#endregion

    }

}