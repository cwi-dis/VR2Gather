using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VRT.Orchestrator.Wrapping;
using VRT.Orchestrator.Responses;
using VRT.Orchestrator.Elements;
using VRT.Core;
using VRT.Pilots.Common;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
#endif
namespace VRT.Login
{

    public enum State
    {
        Offline, Online, LoggedIn, Settings, Play, Create, Join, Lobby, InGame
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
        private State state = State.Offline;
        private AutoState autoState = AutoState.DidNone;

        [SerializeField] private Toggle developerModeButton = null;
        [SerializeField] private Text statusText = null;
        [SerializeField] private SelfRepresentationPreview SelfRepresentationPreview = null;

        [Header("Status and DeveloperStatus")]
        [SerializeField] private GameObject developerPanel = null;
        [SerializeField] private Text StatusPanelUserId = null;
        [SerializeField] private Text StatusPanelUserName = null;
        [SerializeField] private Text StatusPanelOrchestratorURL = null;
        [SerializeField] private Text StatusPanelNativeVersion = null;
        [SerializeField] private Text StatusPanelPlayerVersion = null;
        [SerializeField] private Text StatusPanelOrchestratorVersion = null;
        [SerializeField] private Button StatusPanelStartDeveloperSceneButton = null;

        [Header("ConnectPanel")]
        [SerializeField] private GameObject connectPanel = null;
      
        [Header("LoginPanel")]
        [SerializeField] private GameObject loginPanel = null;
        [SerializeField] private InputField LoginPanelUserName = null;
        [SerializeField] private Button LoginPanelLoginButton = null;
        [SerializeField] private Toggle LoginPanelRememberMeToggle = null;

        [Header("HomePanel")]
        
        [SerializeField] private GameObject homePanel = null;
        [SerializeField] private Text HomePanelUserName = null;
        [SerializeField] private Button HomePanelLogoutButton = null;
        [SerializeField] private Button HomePanelPlayButton = null;
        [SerializeField] private Button HomePanelSettingsButton = null;

        [Header("SettingsPanel")]
        [SerializeField] private GameObject settingsPanel = null;
        [SerializeField] private GameObject SettingsPanelWebcamInfoGO = null;
        [SerializeField] private InputField SettingsPanelTCPURLField = null;
        [SerializeField] private Dropdown SettingsPanelRepresentationDropdown = null;
        [SerializeField] private Dropdown SettingsPanelWebcamDropdown = null;
        [SerializeField] private Dropdown SettingsPanelMicrophoneDropdown = null;
        [SerializeField] private RectTransform SettingsPanelVUMeter = null;
        [SerializeField] private Button SettingsPanelSaveButton = null;
        [SerializeField] private Button SettingsPanelBackButton = null;
        [SerializeField] private Text SettingsPanelSelfRepresentationDescription = null;

        [Header("PlayPanel")]
        [SerializeField] private GameObject playPanel = null;
        [SerializeField] private Button PlayPanelBackButton = null;
        [SerializeField] private Button PlayPanelCreateButton = null;
        [SerializeField] private Button PlayPanelJoinButton = null;

        [Header("CreatePanel")]
        [SerializeField] private GameObject createPanel = null;
        [SerializeField] private Button CreatePanelBackButton = null;
        [SerializeField] private InputField CreatePanelSessionNameField = null;
        [SerializeField] private InputField CreatePanelSessionDescriptionField = null;
        [SerializeField] private Dropdown CreatePanelScenarioDropdown = null;
        [SerializeField] private Text CreatePanelScenarioDescription = null;
        [SerializeField] private Dropdown CreatePanelSessionProtocolDropdown = null;
        [SerializeField] private Toggle CreatePanelUncompressedPointcloudsToggle = null;
        [SerializeField] private Toggle CreatePanelUncompressedAudioToggle = null;
        [SerializeField] private Button CreatePanelCreateButton = null;

        [Header("JoinPanel")]
        [SerializeField] private GameObject joinPanel = null;
        [SerializeField] private Button JoinPanelBackButton = null;
        [SerializeField] private Dropdown JoinPanelSessionDropdown = null;
        [SerializeField] private Text JoinPanelSessionDescription = null;
        [SerializeField] private Button JoinPanelJoinButton = null;
        [SerializeField] private RectTransform JoinPanelSessionList = null;
        [SerializeField] private int JoinPanelRefreshInterval = 5;
        private float JoinPanelRefreshTimer = 0.0f;

        [Header("LobbyPanel")]
        [SerializeField] private GameObject lobbyPanel = null;
        [SerializeField] private Text LobbyPanelSessionName = null;
        [SerializeField] private Text LobbyPanelSessionDescription = null;
        [SerializeField] private Text LobbyPanelScenarioName = null;
        [SerializeField] private Text LobbyPanelSessionNumUsers = null;
        [SerializeField] private Button LobbyPanelStartButton = null;
        [SerializeField] private Button LobbyPanelLeaveButton = null;
        [SerializeField] private RectTransform LobbyPanelSessionUsers = null;
        [SerializeField] private Text LobbyPanelUserRepresentationText = null; // xxxjack can be removed
        [SerializeField] private Image LobbyPanelUserRepresentationImage = null; // xxxjack can be removed

        [Header("Visual representation")]
        [SerializeField] private Color colorConnected = new Color(0.15f, 0.78f, 0.15f); // Green
        [SerializeField] private Color colorConnecting = new Color(0.85f, 0.5f, 0.2f); // Orange
        [SerializeField] private Color colorDisconnecting = new Color(0.78f, 0.15f, 0.15f); // Red
        [SerializeField] private Font MenuFont = null;

        #region Unity

        // Start is called before the first frame update
        void Start()
        {
            if (instance == null)
            {
                instance = this;
            }

            
            // Developer mode settings
            developerMode = PlayerPrefs.GetInt("developerMode", 0) != 0;
            developerModeButton.isOn = developerMode;
            // Update Application version
            StatusPanelOrchestratorURL.text = VRTConfig.Instance.orchestratorURL;
#if UNITY_EDITOR
            foreach(var pkg in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()) {
                if (pkg.name == "nl.cwi.dis.vr2gather") {
                    StatusPanelNativeVersion.text = pkg.version;
                }
            }
#endif
            StatusPanelPlayerVersion.text = "v" + Application.version;
            StatusPanelOrchestratorVersion.text = "";

            // Font to build gui components for logs!
            //MenuFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

            // Fill scenarios
            CreatePanel_UpdateScenarios();

            // Fill UserData representation dropdown according to UserRepresentationType enum declaration
            SettingsPanel_UpdateRepresentations();
            SettingsPanel_UpdateWebcams();
            SettingsPanel_UpdateMicrophones();

            // Buttons listeners
            developerModeButton.onValueChanged.AddListener(delegate { DeveloperModeToggleClicked(); });
            StatusPanelStartDeveloperSceneButton.onClick.AddListener(delegate { StartDeveloperSceneButtonPressed(); });
            LoginPanelLoginButton.onClick.AddListener(delegate { Login(); });
            HomePanelLogoutButton.onClick.AddListener(delegate { Logout(); });
            HomePanelPlayButton.onClick.AddListener(delegate { ChangeState(State.Play); });
            HomePanelSettingsButton.onClick.AddListener(delegate { ChangeState(State.Settings); });
            SettingsPanelSaveButton.onClick.AddListener(delegate { SettingsPanel_SaveButtonPressed(); });
            SettingsPanelBackButton.onClick.AddListener(delegate { SettingsPanel_BackButtonPressed(); });
            PlayPanelBackButton.onClick.AddListener(delegate { ChangeState(State.LoggedIn); });
            PlayPanelCreateButton.onClick.AddListener(delegate { ChangeState(State.Create); });
            PlayPanelJoinButton.onClick.AddListener(delegate { ChangeState(State.Join); });
            CreatePanelBackButton.onClick.AddListener(delegate { ChangeState(State.Play); });
            CreatePanelCreateButton.onClick.AddListener(delegate { AddSession(); });
            JoinPanelBackButton.onClick.AddListener(delegate { ChangeState(State.Play); });
            JoinPanelJoinButton.onClick.AddListener(delegate { JoinSession(); });
            LobbyPanelStartButton.onClick.AddListener(delegate { LobbyPanel_StartButtonPressed(); });
            LobbyPanelLeaveButton.onClick.AddListener(delegate { LeaveSession(); });

            // Dropdown listeners
            SettingsPanelRepresentationDropdown.onValueChanged.AddListener(delegate {
                SettingsPanel_UpdateAfterRepresentationChange();
                // xxxjack AllPanels_UpdateAfterStateChange();
            });
            SettingsPanelWebcamDropdown.onValueChanged.AddListener(delegate {
                SettingsPanel_UpdateAfterRepresentationChange();
                // xxxjack AllPanels_UpdateAfterStateChange();
            });
            SettingsPanelMicrophoneDropdown.onValueChanged.AddListener(delegate {
                SelfRepresentationPreview.ChangeMicrophone(SettingsPanelMicrophoneDropdown.options[SettingsPanelMicrophoneDropdown.value].text);
            });
            CreatePanelScenarioDropdown.onValueChanged.AddListener(delegate { CreatePanel_ScenarioSelectionChanged(); });

            JoinPanelSessionDropdown.onValueChanged.AddListener(delegate { JoinPanel_SessionSelectionChanged(); });

            InitialiseControllerEvents();

            CreatePanel_UpdateProtocols();
            CreatePanelUncompressedPointcloudsToggle.isOn = SessionConfig.Instance.pointCloudCodec == "cwi0";
            CreatePanelUncompressedAudioToggle.isOn = SessionConfig.Instance.voiceCodec == "VR2a";

            if (OrchestratorController.Instance.UserIsLogged)
            { // Comes from another scene
              // Set status to online
                statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
                statusText.color = colorConnected;
                AllPanels_UpdateUserData();
                JoinPanel_UpdateSessions();
           
                OrchestratorController.Instance.OnLoginResponse(new ResponseStatus(), StatusPanelUserId.text);
            }
            else
            { // Enter for first time
              // Set status to offline
                statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
                statusText.color = colorDisconnecting;
                state = State.Offline;

                // Try to connect
                SocketConnect();
            }
        }

      
        // Update is called once per frame
        void Update()
        {
            // Update the microphone VU meter, if it is visible.
            if (SettingsPanelVUMeter && SettingsPanelVUMeter.gameObject.activeInHierarchy && SelfRepresentationPreview)
                SettingsPanelVUMeter.sizeDelta = new Vector2(355 * Mathf.Min(1, SelfRepresentationPreview.MicrophoneLevel), 20);
            // Allow tabbing between input fields, if needed
            _ImplementTabShortcut();
          
            // Refresh Sessions, if needed
            if (state == State.Join)
            {
                JoinPanelRefreshTimer += Time.deltaTime;
                if (JoinPanelRefreshTimer >= JoinPanelRefreshInterval)
                {
                    GetSessions();
                    JoinPanelRefreshTimer = 0.0f;
                }
            }
        }

        private void OnDestroy()
        {
            TerminateControllerEvents();
        }
        #endregion

        #region Global logic
        private void StartSelfRepresentationPreview()
        {
            if (SelfRepresentationPreview == null)
            {
                Debug.LogError("OrchestratorLogin: No self previww");
                return;
            }
            SelfRepresentationPreview.gameObject.SetActive(true);
            SelfRepresentationPreview.enabled = true;
            SelfRepresentationPreview.InitializeSelfPlayer();
        }

        /// <summary>
        /// This method implements AutoStart. It is called whenever something in the state has changed, so that we can
        /// potentially get a bit further with autostart.
        /// </summary>
        private void AutoStart_StateUpdate()
        {
            // We do a quick exit if we don't have an autostart config, or if shift is pressed.
            VRTConfig._AutoStart config = VRTConfig.Instance.AutoStart;
            if (config == null) return;
            if (Keyboard.current.shiftKey.isPressed) return;

            if (autoState == AutoState.DidNone && VRTConfig.Instance.AutoStart.autoLogin)
            {
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoLogin");
                if (Login())
                {
                    autoState = AutoState.DidLogIn;
                }
                else
                {
                    VRTConfig.Instance.AutoStart.autoLogin = false;
                }
                return;
            }
            if (autoState == AutoState.DidLogIn && (VRTConfig.Instance.AutoStart.autoCreate || VRTConfig.Instance.AutoStart.autoJoin))
            {
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate {VRTConfig.Instance.AutoStart.autoCreate} autoJoin {VRTConfig.Instance.AutoStart.autoJoin}");
                autoState = AutoState.DidPlay;
                ChangeState(State.Play);
                Invoke(nameof(AutoStart_StateUpdate), VRTConfig.Instance.AutoStart.autoDelay);
                return;
            }
            if (state == State.Play && autoState == AutoState.DidPlay)
            {
                if (config.autoCreate)
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: starting");
                    autoState = AutoState.DidCreate;
                    ChangeState(State.Create);

                }
                if (config.autoJoin)
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoJoin: starting");
                    autoState = AutoState.DidJoin;
                    ChangeState(State.Join);
                }
            }
            if (state == State.Create && autoState == AutoState.DidCreate)
            {
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: sessionName={config.sessionName}");
                CreatePanelSessionNameField.text = config.sessionName;
                CreatePanelUncompressedPointcloudsToggle.isOn = config.sessionUncompressed;
                CreatePanelUncompressedAudioToggle.isOn = config.sessionUncompressedAudio;
                if (config.sessionTransportProtocol != null && config.sessionTransportProtocol != "")
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: sessionTransportProtocol={config.sessionTransportProtocol}");
                    // xxxjack I don't understand the intended logic behind the toggles. But turning everything
                    // on and then simulating a button callback works.
                    
                    CreatePanel_ProtocolChanged(config.sessionTransportProtocol);
                }
                else
                {
                    CreatePanel_ProtocolChanged("socketio");
                }
                autoState = AutoState.DidPartialCreation;
            }
            if (state == State.Create && autoState == AutoState.DidPartialCreation && CreatePanelScenarioDropdown.options.Count > 0)
            {
                if (config.sessionScenario != null && config.sessionScenario != "")
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: sessionScenario={config.sessionScenario}");
                    bool found = false;
                    int idx = 0;
                    foreach (var entry in CreatePanelScenarioDropdown.options)
                    {
                        if (entry.text == config.sessionScenario)
                        {
                            CreatePanelScenarioDropdown.value = idx;
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
                if (LobbyPanelSessionNumUsers.text == config.autoStartWith.ToString())
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: starting with {config.autoStartWith} users");
                    Invoke(nameof(LobbyPanel_StartButtonPressed), config.autoDelay);
                    autoState = AutoState.Done;
                }
            }
            if (state == State.Join && autoState == AutoState.DidJoin)
            {
                var options = JoinPanelSessionDropdown.options;
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autojoin: look for {config.sessionName}");
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].text.StartsWith(config.sessionName + " "))
                    {
                        if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autojoin: entry {i} is {config.sessionName}, joining");
                        JoinPanelSessionDropdown.value = i;
                        autoState = AutoState.Done;
                        Invoke(nameof(JoinSession), config.autoDelay);
                    }
                }
            }
        }

        private void SaveUserData()
        {
            // And also save a local copy, if wanted
            if (String.IsNullOrEmpty(VRTConfig.Instance.LocalUser.orchestratorConfigFilename))
            {
                Debug.LogError("OrchestratorLogin.SaveUserData: orchestratorConfigFilename is empty");
                return;
            }
            var configData = OrchestratorController.Instance.SelfUser.userData.AsJsonString();
            var fullName = VRTConfig.ConfigFilename(VRTConfig.Instance.LocalUser.orchestratorConfigFilename);
            Debug.Log("Full config filename: " + fullName);
            System.IO.File.WriteAllText(fullName, configData);
            Debug.Log($"OrchestratorLogin: saved UserData to {fullName}");
        }

        private void LoadUserData()
        {
            // Load locally save user data
            if (String.IsNullOrEmpty(VRTConfig.Instance.LocalUser.orchestratorConfigFilename))
            {
                Debug.LogError("OrchestratorLogin.LoadUserData: LocalUser.orchestratorConfigFilename is empty");
                OrchestratorController.Instance.SelfUser.userData = new UserData();
                return;
            }
            var fullName = VRTConfig.ConfigFilename(VRTConfig.Instance.LocalUser.orchestratorConfigFilename);
            if (!System.IO.File.Exists(fullName))
            {
                Debug.LogWarning($"OrchestratorLogin.LoadUserData: Cannot open {fullName}");
                OrchestratorController.Instance.SelfUser.userData = new UserData();
                return;
            }

            Debug.Log($"OrchestratorLogin: load UserData from {fullName}");
            var configData = System.IO.File.ReadAllText(fullName);
            UserData lUserData = UserData.ParseJsonString<UserData>(configData);
            OrchestratorController.Instance.SelfUser.userData = lUserData;
        }

        private void UploadUserData()
        {
            // Also send to orchestrator. This is mainly so that the orchestrator can tell
            // other participants our self-representation.
            OrchestratorController.Instance.UpdateFullUserData(OrchestratorController.Instance.SelfUser.userData);
            Debug.Log($"OrchestratorLogin: uploaded UserData to orchestrator");
        }
        #endregion

        #region UI: global

        public void AllPanels_UpdateUserData()
        {
            if (OrchestratorController.Instance == null || OrchestratorController.Instance.SelfUser == null)
            {
                Debug.LogWarning($"OrchestratorLogin: FillSelfUserData: no SelfUser data yet");
            }
            User user = OrchestratorController.Instance.SelfUser;

            // UserID & Name
            StatusPanelUserId.text = user.userId;
            StatusPanelUserName.text = user.userName;
            HomePanelUserName.text = user.userName;
            if (state == State.Settings)
            {
                SettingsPanel_UpdateUserData();
            }
        }

        private void AllPanels_UpdateAfterStateChange()
        {
            // Get the user name (if we have one) it is used to initialize various fields.
            string uname = OrchestratorController.Instance?.SelfUser?.userName;

            developerPanel.SetActive(developerMode);
            connectPanel.gameObject.SetActive(state == State.Offline);
            loginPanel.SetActive(state == State.Online);
            homePanel.SetActive(state == State.LoggedIn);
            settingsPanel.SetActive(state == State.Settings);
            playPanel.SetActive(state == State.Play);
            createPanel.SetActive(state == State.Create);
            joinPanel.SetActive(state == State.Join);
            lobbyPanel.SetActive(state == State.Lobby);
            // We have to (re)initialize some fields for some of the panels.
            switch (state)
            {
                case State.Offline:
                    break;
                case State.Online:
                    LoadLoginPlayerPrefs();
                    break;
                case State.LoggedIn:
                    HomePanelUserName.text = uname;
                    StatusPanelUserName.text = uname;
                    break;
                case State.Settings:
                    SettingsPanel_UpdateUserData();
                    SettingsPanel_UpdateAfterRepresentationChange();
                    break;
                case State.Play:
                    break;
                case State.Create:
                    // Ensure we always have a default sesssion name (for running without access to a keyboard)
                    if (string.IsNullOrEmpty(CreatePanelSessionNameField.text))
                    {
                         string time = DateTime.Now.ToString("hhmmss");
                        CreatePanelSessionNameField.text = $"{uname}_{time}";
                    }
                    break;
                case State.Join:
                    // Refresh the list of sessions
                    GetSessions();
                    break;
                case State.Lobby:
                    // Ensure only the session master has the Start button.
                    LobbyPanelStartButton.gameObject.SetActive(OrchestratorController.Instance.UserIsMaster);
                    break;
                case State.InGame:
                    break;
                default:
                    break;
            }
            _SelectFirstIInputField();
        }

        private void DeveloperModeToggleClicked()
        {
            developerMode = developerModeButton.isOn;
            PlayerPrefs.SetInt("developerMode", developerMode ? 1 : 0);
            AllPanels_UpdateAfterStateChange();
        }

        private void StartDeveloperSceneButtonPressed()
        {
            PilotController.Instance.LoadNewScene("SoloPlayground");
        }

        public void ChangeState(State _state)
        {
            state = _state;
            AllPanels_UpdateAfterStateChange();
        }
        #endregion

        #region UI: LoginPanel

        // Check saved used credentials.
        private void LoadLoginPlayerPrefs()
        {
            if (PlayerPrefs.HasKey("userNameLoginIF"))
            {
                string userName = PlayerPrefs.GetString("userNameLoginIF");
                if (string.IsNullOrEmpty(userName)) {
                    LoginPanelRememberMeToggle.isOn = false;
                } else
                {
                    LoginPanelRememberMeToggle.isOn = true;
                    LoginPanelUserName.text = userName;
                }
            }
            else
                LoginPanelRememberMeToggle.isOn = false;
        }

        private void SaveLoginPlayerPrefs()
        {
            if (string.IsNullOrEmpty(LoginPanelUserName.text))
            {
                // Don't save an empty username
                LoginPanelRememberMeToggle.isOn = false;
            }
            if (LoginPanelRememberMeToggle.isOn)
            {
                PlayerPrefs.SetString("userNameLoginIF", LoginPanelUserName.text);
            }
            else
            {
                PlayerPrefs.DeleteKey("userNameLoginIF");
            }
        }

        #endregion

        #region UI: SettingsPanel

        private void SettingsPanel_UpdateRepresentations()
        {
            Dropdown dd = SettingsPanelRepresentationDropdown;
            // Fill UserData representation dropdown according to UserRepresentationType enum declaration
            // xxxjack this has the huge disadvantage that they are numerically sorted.
            // xxxjack and the order is difficult to change currently, because the values
            // xxxjack are stored by the orchestrator in the user record, in numerical form...
            dd.ClearOptions();
            dd.AddOptions(new List<string>(Enum.GetNames(typeof(UserRepresentationType))));

        }

        private void SettingsPanel_UpdateWebcams()
        {
            Dropdown dd = SettingsPanelWebcamDropdown;
            // Fill UserData representation dropdown according to UserRepresentationType enum declaration
            dd.ClearOptions();
            WebCamDevice[] devices = WebCamTexture.devices;
            List<string> webcams = new List<string>();
            webcams.Add("None");
            foreach (WebCamDevice device in devices)
                webcams.Add(device.name);
            dd.AddOptions(webcams);
        }

        private void SettingsPanel_UpdateMicrophones()
        {
            Dropdown dd = SettingsPanelMicrophoneDropdown;
            // Fill UserData representation dropdown according to UserRepresentationType enum declaration
            dd.ClearOptions();
            string[] devices = Microphone.devices;
            List<string> microphones = new List<string>();
            microphones.Add("None");
            foreach (string device in devices)
                microphones.Add(device);
            dd.AddOptions(microphones);
        }

        private void SettingsPanel_SetRepresentation(UserRepresentationType _representationType)
        {

            // left change the icon 'userRepresentationLobbyImage'
            switch (_representationType)
            {
                case UserRepresentationType.NoRepresentation:
                    SettingsPanelSelfRepresentationDescription.text = "No representation, no audio. The user can only watch.";
                    break;
                case UserRepresentationType.VideoAvatar:
                    SettingsPanelSelfRepresentationDescription.text = "Avatar with video window from your camera.";
                    break;
                case UserRepresentationType.SimpleAvatar:
                    SettingsPanelSelfRepresentationDescription.text = "3D Synthetic Avatar.";
                    break;
                case UserRepresentationType.PointCloud:
                    SettingsPanelSelfRepresentationDescription.text = "Realistic point cloud user representation, captured live.";
                    break;
                case UserRepresentationType.AudioOnly:
                    SettingsPanelSelfRepresentationDescription.text = "No visual representation, only audio communication.";
                    break;
                case UserRepresentationType.NoRepresentationCamera:
                    SettingsPanelSelfRepresentationDescription.text = "Local video recorder.";
                    break;
                case UserRepresentationType.AppDefinedRepresentationOne:
                    SettingsPanelSelfRepresentationDescription.text = "Application-defined representation 1.";
                    break;
                case UserRepresentationType.AppDefinedRepresentationTwo:
                    SettingsPanelSelfRepresentationDescription.text = "Application-defined representation 2.";
                    break;
                default:
                    Debug.LogError($"OrchestratorLogin: Unknown UserRepresentationType {_representationType}");
                    break;
            }
        }

        private void SettingsPanel_ExtractUserData()
        {
            // UserData info in Config
            UserData lUserData = new UserData
            {
                userAudioUrl = SettingsPanelTCPURLField.text,
                userRepresentationType = (UserRepresentationType)SettingsPanelRepresentationDropdown.value,
                webcamName = (SettingsPanelWebcamDropdown.options.Count <= 0) ? "None" : SettingsPanelWebcamDropdown.options[SettingsPanelWebcamDropdown.value].text,
                microphoneName = (SettingsPanelMicrophoneDropdown.options.Count <= 0) ? "None" : SettingsPanelMicrophoneDropdown.options[SettingsPanelMicrophoneDropdown.value].text
            };
            OrchestratorController.Instance.SelfUser.userData = lUserData;
        }

        public void SettingsPanel_UpdateUserData()
        {
            User user = OrchestratorController.Instance.SelfUser;

            // Config Info
            if (user.userData == null)
            {
                user.userData = new UserData();
            }
            UserData userData = user.userData;

            SettingsPanelTCPURLField.text = userData.userAudioUrl;
            SettingsPanelRepresentationDropdown.value = (int)userData.userRepresentationType;
            SettingsPanelWebcamDropdown.value = 0;

            for (int i = 0; i < SettingsPanelWebcamDropdown.options.Count; ++i)
            {
                if (SettingsPanelWebcamDropdown.options[i].text == userData.webcamName)
                {
                    SettingsPanelWebcamDropdown.value = i;
                    break;
                }
            }
            SettingsPanelMicrophoneDropdown.value = 0;
            for (int i = 0; i < SettingsPanelMicrophoneDropdown.options.Count; ++i)
            {
                if (SettingsPanelMicrophoneDropdown.options[i].text == userData.microphoneName)
                {
                    SettingsPanelMicrophoneDropdown.value = i;
                    break;
                }
            }
        }

        public void SettingsPanel_UpdateAfterRepresentationChange()
        {
            // Dropdown Logic
            SettingsPanelWebcamInfoGO.SetActive(false);


            if ((UserRepresentationType)SettingsPanelRepresentationDropdown.value == UserRepresentationType.VideoAvatar)
            {
                SettingsPanelWebcamInfoGO.SetActive(true);
            }
            // Preview
            SettingsPanel_SetRepresentation((UserRepresentationType)SettingsPanelRepresentationDropdown.value);
            SelfRepresentationPreview.ChangeRepresentation(
                (UserRepresentationType)SettingsPanelRepresentationDropdown.value,
                SettingsPanelWebcamDropdown.options[SettingsPanelWebcamDropdown.value].text
                );
            SelfRepresentationPreview.ChangeMicrophone(
                SettingsPanelMicrophoneDropdown.options[SettingsPanelMicrophoneDropdown.value].text
                );
        }

        public void SettingsPanel_SaveButtonPressed()
        {
            SelfRepresentationPreview.StopMicrophone();
            SettingsPanel_ExtractUserData();
            SaveUserData();
            UploadUserData();
            state = State.LoggedIn;
            AllPanels_UpdateAfterStateChange();
        }

        public void SettingsPanel_BackButtonPressed()
        {
            SelfRepresentationPreview.StopMicrophone();
            state = State.LoggedIn;
            AllPanels_UpdateAfterStateChange();
        }

        #endregion

        #region UI: CreatePanel

        private void CreatePanel_UpdateScenarios()
        {
            // update the dropdown
            CreatePanelScenarioDropdown.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (var sc in ScenarioRegistry.Instance.Scenarios)
            {
                options.Add(new Dropdown.OptionData(sc.scenarioName));
            }

            CreatePanelScenarioDropdown.AddOptions(options);
            CreatePanel_ScenarioSelectionChanged();
        }

        private void CreatePanel_ScenarioSelectionChanged()
        {
            var idx = CreatePanelScenarioDropdown.value;
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
            if (CreatePanelScenarioDescription != null)
            {
                CreatePanelScenarioDescription.text = message;
            }
            CreatePanelCreateButton.interactable = ok;
        }

        private void CreatePanel_UpdateProtocols()
        {
            CreatePanelSessionProtocolDropdown.ClearOptions();
            List<string> names = new List<string>();
            foreach (string protocolName in TransportProtocol.GetNames())
            {
                names.Add(protocolName);
            }
            CreatePanelSessionProtocolDropdown.AddOptions(names);
            CreatePanelSessionProtocolDropdown.value = 0;
        }

        /// <summary>
        /// Should be called whenever eith audio or pointcloud compression toggle has changed value.
        /// </summary>
        public void CreatePanel_UncompressedChanged()
        {
            if (CreatePanelUncompressedPointcloudsToggle.isOn)
            {
                SessionConfig.Instance.pointCloudCodec = "cwi0";
            }
            else
            {
                SessionConfig.Instance.pointCloudCodec = "cwi1";
            }
            if (CreatePanelUncompressedAudioToggle.isOn)
            {
                SessionConfig.Instance.voiceCodec = "VR2a";
            }
            else
            {
                SessionConfig.Instance.voiceCodec = "VR2A";
            }
        }

        /// <summary>
        /// Should be called when the protocol dropdown has changed value, or to force a specific protocol value.
        /// </summary>
        /// <param name="protoString"></param>
        public void CreatePanel_ProtocolChanged(string protoString)
        {
            if (string.IsNullOrEmpty(protoString))
            {
                // Empty string means we're called from the dropdown callback. Get the value from there.
                protoString = CreatePanelSessionProtocolDropdown.options[CreatePanelSessionProtocolDropdown.value].text;
            }
            bool done = false;
            for (int i = 0; i < CreatePanelSessionProtocolDropdown.options.Count; i++)
            {
                if (protoString.ToLower() == CreatePanelSessionProtocolDropdown.options[i].text.ToLower())
                {
                    done = true;
                    CreatePanelSessionProtocolDropdown.value = i;
                }
            }
            if (!done)
            {
                Debug.LogError($"OrchestratorLogin: unknown protocol \"protoString\"");
            }

            SessionConfig.Instance.protocolType = protoString;
        }

        #endregion

        #region UI: JoinPanel

        private void JoinPanel_UpdateSessions()
        {
            Transform container = JoinPanelSessionList;
            _ClearScrollView(container.transform);
            foreach (var session in OrchestratorController.Instance.AvailableSessions)
            {
                _AddTextComponentOnScrollView(container.transform, session.GetGuiRepresentation());
            }

            string selectedOption = "";
            // store selected option in dropdown
            if (JoinPanelSessionDropdown.options.Count > 0)
                selectedOption = JoinPanelSessionDropdown.options[JoinPanelSessionDropdown.value].text;
            // update the dropdown
            JoinPanelSessionDropdown.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (var sess in OrchestratorController.Instance.AvailableSessions)
            {
                options.Add(new Dropdown.OptionData(sess.GetGuiRepresentation()));
            }
            JoinPanelSessionDropdown.AddOptions(options);
            // re-assign selected option in dropdown
            if (JoinPanelSessionDropdown.options.Count > 0)
            {
                for (int i = 0; i < JoinPanelSessionDropdown.options.Count; ++i)
                {
                    if (JoinPanelSessionDropdown.options[i].text == selectedOption)
                        JoinPanelSessionDropdown.value = i;
                }
            }
            JoinPanel_SessionSelectionChanged();
        }

        private void JoinPanel_SessionSelectionChanged()
        {
            var idx = JoinPanelSessionDropdown.value;
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
            JoinPanelSessionDescription.text = description;
            JoinPanelJoinButton.interactable = ok;
        }

        #endregion

        #region UI: LobbyPanel
        public void LobbyPanel_StartButtonPressed()
        {
            SessionConfig cfg = SessionConfig.Instance;
            cfg.scenarioName = OrchestratorController.Instance.CurrentScenario.scenarioName;
            cfg.scenarioVariant = null;
            // protocolType already set
            // pointCloudCodec, voiceCodec and videoCodec already set
            string message = JsonUtility.ToJson(cfg);
            SendMessageToAll("START_" + message);
        }

        private void LobbyPanel_UpdateSessionUsers()
        {
            Transform container = LobbyPanelSessionUsers;
            _ClearScrollView(LobbyPanelSessionUsers.transform);
            Session session = OrchestratorController.Instance.CurrentSession;
            if (session == null)
            {
                Debug.Log("xxxjack OrchestratorLogin: UpdateUsersSession: no current session");
                return;
            }
            User[] sessionUsers = session.GetUsers();
            foreach (User u in sessionUsers)
            {
                _AddUserComponentOnScrollView(container.transform, u);
            }
            LobbyPanelSessionNumUsers.text = sessionUsers.Length.ToString() /*+ "/" + "4"*/;
            Debug.Log($"xxxjack OrchestratorLogin: UpdateUsersSession: {sessionUsers.Length} users in session");
            // We may be able to continue auto-starting
            if (VRTConfig.Instance.AutoStart != null)
                Invoke(nameof(AutoStart_StateUpdate), VRTConfig.Instance.AutoStart.autoDelay);
        }

        // xxxjack is not currently called...
        private void LobbyPanel_SetUserRepresentationGUI(UserRepresentationType _representationType)
        {
            LobbyPanelUserRepresentationText.text = _representationType.ToString();
            // left change the icon 'userRepresentationLobbyImage'

            switch (_representationType)
            {
                case UserRepresentationType.NoRepresentation:
                case UserRepresentationType.AudioOnly:
                    LobbyPanelUserRepresentationImage.sprite = Resources.Load<Sprite>("Icons/URNoneIcon");
                    break;
                case UserRepresentationType.VideoAvatar:
                    LobbyPanelUserRepresentationImage.sprite = Resources.Load<Sprite>("Icons/URCamIcon");
                    break;
                case UserRepresentationType.SimpleAvatar:
                    LobbyPanelUserRepresentationImage.sprite = Resources.Load<Sprite>("Icons/URAvatarIcon");
                    break;
                case UserRepresentationType.PointCloud:
                    LobbyPanelUserRepresentationImage.sprite = Resources.Load<Sprite>("Icons/URSingleIcon");
                    break;
                case UserRepresentationType.NoRepresentationCamera:
                    LobbyPanelUserRepresentationImage.sprite = Resources.Load<Sprite>("Icons/URCameramanIcon");
                    break;

            }
        }
        #endregion

        #region UI: helpers

        // Helper method: add a text line to a scroll view.
        private void _AddTextComponentOnScrollView(Transform container, string value)
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

        // Helper method: add a user description line to a scroll view
        private void _AddUserComponentOnScrollView(Transform container, User user)
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
                case UserRepresentationType.AppDefinedRepresentationOne:
                    imageItem.sprite = Resources.Load<Sprite>("Icons/URAvatarIcon");
                    textItem.text += " - (AppDefined 1)";
                    break;
                case UserRepresentationType.AppDefinedRepresentationTwo:
                    imageItem.sprite = Resources.Load<Sprite>("Icons/URAvatarIcon");
                    textItem.text += " - (AppDefined 2)";
                    break;
                default:
                    Debug.LogError($"OrchestratorLogin: Unknown UserRepresentationType {user.userData.userRepresentationType}");
                    break;
            }
        }

        // Helper method: clear a scroll view.
        private void _ClearScrollView(Transform container)
        {
            for (var i = container.childCount - 1; i >= 0; i--)
            {
                var obj = container.GetChild(i);
                obj.transform.SetParent(null);
                Destroy(obj.gameObject);
            }
        }

        // Helper method to select first input field
        private void _SelectFirstIInputField()
        {
            try
            {
                InputField[] inputFields = FindObjectsOfType<InputField>();
                if (inputFields != null)
                {
                    inputFields[inputFields.Length - 1].OnPointerClick(new PointerEventData(EventSystem.current));  //if it's an input field, also set the text caret
                    inputFields[inputFields.Length - 1].caretWidth = 2;
                    //EventSystem.current.SetSelectedGameObject(first.gameObject, new BaseEventData(EventSystem.current));
                }
            }
            catch { }
        }

        // Helper method to use TAB to navitage between input fields.
        private void _ImplementTabShortcut()
        {
            if (
            Keyboard.current.tabKey.wasPressedThisFrame
                )
            {
                try
                {
                    Selectable current = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
                    if (current != null)
                    {
                        Selectable next = current.FindSelectableOnDown();
                        if (next != null)
                        {
                            InputField inputfield = next.GetComponent<InputField>();
                            if (inputfield != null)
                            {
                                inputfield.OnPointerClick(new PointerEventData(EventSystem.current));  //if it's an input field, also set the text caret
                                inputfield.caretWidth = 2;
                            }

                            EventSystem.current.SetSelectedGameObject(next.gameObject, new BaseEventData(EventSystem.current));
                        }
                        else
                        {
                            // Select the first IF because no more elements exists in the list
                            _SelectFirstIInputField();
                        }
                    }
                    //else Debug.Log("no selectable object selected in event EventSystem.current");
                }
                catch { }
            }
        }

        #endregion

        #region Orchestrator: API event listeners

        // Subscribe to Orchestrator Wrapper Events
        private void InitialiseControllerEvents()
        {
            OrchestratorController.Instance.OnConnectionEvent += UpdateStateOnConnectionEvent;
            OrchestratorController.Instance.OnConnectingEvent += UpdateStateOnConnectingEvent;
            OrchestratorController.Instance.OnGetOrchestratorVersionEvent += UpdateStateOnGetOrchestratorVersionEvent;
            OrchestratorController.Instance.OnLoginEvent += UpdateStateOnLoginEvent;
            OrchestratorController.Instance.OnLogoutEvent += UpdateStateOnLogout;
            OrchestratorController.Instance.OnGetNTPTimeEvent += UpdateStateOnGetNTPTime;
            OrchestratorController.Instance.OnSessionsEvent += UpdateStateOnGetSessions;
            OrchestratorController.Instance.OnAddSessionEvent += UpdateStateOnAddSession;
            OrchestratorController.Instance.OnSessionInfoEvent += UpdateStateOnSessionInfoEvent;
            OrchestratorController.Instance.OnJoinSessionEvent += UpdateStateOnJoinSession;
            OrchestratorController.Instance.OnLeaveSessionEvent += UpdateStateOnLeaveSession;
            OrchestratorController.Instance.OnDeleteSessionEvent += UpdateStateOnOnDeleteSessionEvent;
            OrchestratorController.Instance.OnUserJoinSessionEvent += UpdateStateOnUserJoinedSessionEvent;
            OrchestratorController.Instance.OnUserLeaveSessionEvent += UpdateStateOnUserLeftSessionEvent;

            OrchestratorController.Instance.OnUserMessageReceivedEvent += OnUserMessageReceivedHandler;
            OrchestratorController.Instance.OnMasterEventReceivedEvent += OnMasterEventReceivedHandler;
            OrchestratorController.Instance.OnUserEventReceivedEvent += OnUserEventReceivedHandler;
            OrchestratorController.Instance.OnErrorEvent += OnErrorHandler;
        }

        // Un-Subscribe to Orchestrator Wrapper Events
        private void TerminateControllerEvents()
        {
            OrchestratorController.Instance.OnConnectionEvent -= UpdateStateOnConnectionEvent;
            OrchestratorController.Instance.OnConnectingEvent -= UpdateStateOnConnectingEvent;
            OrchestratorController.Instance.OnGetOrchestratorVersionEvent -= UpdateStateOnGetOrchestratorVersionEvent;
            OrchestratorController.Instance.OnLoginEvent -= UpdateStateOnLoginEvent;
            OrchestratorController.Instance.OnLogoutEvent -= UpdateStateOnLogout;
            OrchestratorController.Instance.OnGetNTPTimeEvent -= UpdateStateOnGetNTPTime;
            OrchestratorController.Instance.OnSessionsEvent -= UpdateStateOnGetSessions;
            OrchestratorController.Instance.OnAddSessionEvent -= UpdateStateOnAddSession;
            OrchestratorController.Instance.OnSessionInfoEvent -= UpdateStateOnSessionInfoEvent;
            OrchestratorController.Instance.OnJoinSessionEvent -= UpdateStateOnJoinSession;
            OrchestratorController.Instance.OnLeaveSessionEvent -= UpdateStateOnLeaveSession;
            OrchestratorController.Instance.OnDeleteSessionEvent -= UpdateStateOnOnDeleteSessionEvent;
            OrchestratorController.Instance.OnUserJoinSessionEvent -= UpdateStateOnUserJoinedSessionEvent;
            OrchestratorController.Instance.OnUserLeaveSessionEvent -= UpdateStateOnUserLeftSessionEvent;

            OrchestratorController.Instance.OnUserMessageReceivedEvent -= OnUserMessageReceivedHandler;
            OrchestratorController.Instance.OnMasterEventReceivedEvent -= OnMasterEventReceivedHandler;
            OrchestratorController.Instance.OnUserEventReceivedEvent -= OnUserEventReceivedHandler;
            OrchestratorController.Instance.OnErrorEvent -= OnErrorHandler;
        }

        #endregion

        #region Orchestrator: Unsolicited events

        private void OnErrorHandler(ResponseStatus status)
        {
            Debug.Log("OrchestratorLogin: OnError: Error code: " + status.Error + ", Error message: " + status.Message);
            ErrorManager.Instance.EnqueueOrchestratorError(status.Error, status.Message);
        }

        private void SocketConnect()
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

        private void UpdateStateOnConnectionEvent(bool pConnected)
        {
            if (pConnected)
            {
                statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
                statusText.color = colorConnected;
                state = State.Online;
                AllPanels_UpdateAfterStateChange();
                // We may want to login automatically.
                AutoStart_StateUpdate();
            }
            else
            {
                UpdateStateOnLogout(true);
                statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
                statusText.color = colorDisconnecting;
                AllPanels_UpdateAfterStateChange();
                state = State.Offline;
            }
        }

        private void UpdateStateOnConnectingEvent()
        {
            statusText.text = OrchestratorController.orchestratorConnectionStatus.__CONNECTING__.ToString();
            statusText.color = colorConnecting;
        }

        private void UpdateStateOnGetOrchestratorVersionEvent(string pVersion)
        {
            // After login we ask the orchestrator for its version.
            // When we get here we now know the version, and we ask the orchestrator for
            // the NTP time.
            Debug.Log("Orchestration Service: " + pVersion);
            StatusPanelOrchestratorVersion.text = pVersion;
            GetNTPTime();
        }


        private void UpdateStateOnSessionInfoEvent(Session session)
        {
            if (session != null)
            {
                // Update the info in LobbyPanel
                LobbyPanelSessionName.text = session.sessionName;
                LobbyPanelSessionDescription.text = session.sessionDescription;
                // Update the list of session users
                LobbyPanel_UpdateSessionUsers();
            }
            else
            {
                LobbyPanelSessionName.text = "";
                LobbyPanelSessionDescription.text = "";
                LobbyPanelScenarioName.text = "";
                LobbyPanelSessionNumUsers.text = "";
                _ClearScrollView(LobbyPanelSessionUsers.transform);
            }
        }

        private void UpdateStateOnOnDeleteSessionEvent()
        {
            if (developerMode) Debug.Log("OrchestratorLogin: UpdateStateOnOnDeleteSessionEvent: Session deleted");
        }

        private void UpdateStateOnUserJoinedSessionEvent(string userID)
        {
        }

        private void UpdateStateOnUserLeftSessionEvent(string userID)
        {
        }
        #endregion

        #region Orchestrator: Commands and responses

        // Login from the main buttons Login & Logout
        private bool Login()
        {
            SaveLoginPlayerPrefs();
            
            var userName = LoginPanelUserName.text;
            if (userName == "")
            {
                if (!VRTConfig.Instance.AutoStart.autoLogin)
                {
                    Debug.LogError("Cannot login if no username specified");
                }
                return false;
            }
            // If we want to autoCreate or autoStart depending on username set the right config flags.
            if (VRTConfig.Instance.AutoStart != null && VRTConfig.Instance.AutoStart.autoCreateForUser != "")
            {
                bool isThisUser = VRTConfig.Instance.AutoStart.autoCreateForUser == LoginPanelUserName.text;
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: user={LoginPanelUserName.text} autoCreateForUser={VRTConfig.Instance.AutoStart.autoCreateForUser} isThisUser={isThisUser}");
                VRTConfig.Instance.AutoStart.autoCreate = isThisUser;
                VRTConfig.Instance.AutoStart.autoJoin = !isThisUser;
            }
            OrchestratorController.Instance.Login(userName, "");
            return true;
        }

        private void UpdateStateOnLoginEvent(bool userLoggedSucessfully)
        {
            if (!userLoggedSucessfully)
            {
                // User login has failed. Treat as logout.
                UpdateStateOnLogout(true);
                return;
            }

            AllPanels_UpdateAfterStateChange();
            // After successful login we ask the Orchestrator for its version
            OrchestratorController.Instance.GetVersion();
        }

        private void UpdateStateOnLoginFullyComplete()
        {
            // We can now load the user data and send it to the orchesrator.
            // Also, we can start the self preview (because the user data is complete)

            LoadUserData();
            UploadUserData();
            StartSelfRepresentationPreview();
            state = State.LoggedIn;
            

            AllPanels_UpdateAfterStateChange();
            AutoStart_StateUpdate();
           
        }

        private void Logout()
        {
            OrchestratorController.Instance.Logout();
        }

        private void UpdateStateOnLogout(bool userLogoutSucessfully)
        {
            if (userLogoutSucessfully)
            {
                StatusPanelUserId.text = "";
                StatusPanelUserName.text = "";
                HomePanelUserName.text = "";
                state = State.Online;
            }
            AllPanels_UpdateAfterStateChange();
        }

        private void GetNTPTime()
        {
            OrchestratorController.Instance.GetNTPTime();
        }

        private void UpdateStateOnGetNTPTime(NtpClock ntpTime)
        {
            // The final step in connecting to the orchestrator and logging in: we have the NTP time.
            // We are now fully logged in.
            double difference = OrchestratorController.GetClockTimestamp(DateTime.UtcNow) - ntpTime.Timestamp;
            if (developerMode) Debug.Log("OrchestratorLogin: OnGetNTPTimeResponse: Difference: " + difference);
            if (Math.Abs(difference) >= VRTConfig.Instance.ntpSyncThreshold)
            {
                Debug.LogError($"This machine has a desynchronization of {difference:F3} sec with the Orchestrator.\nThis is greater than {VRTConfig.Instance.ntpSyncThreshold:F3}.\nYou may suffer some problems as a result.");
            }
            UpdateStateOnLoginFullyComplete();
        }

        private void GetSessions()
        {
            OrchestratorController.Instance.GetSessions();
        }

        private void UpdateStateOnGetSessions(Session[] sessions)
        {
            if (sessions != null)
            {
                // update the list of available sessions
                JoinPanel_UpdateSessions();
                // We may be able to advance auto-connection
                if (VRTConfig.Instance.AutoStart != null)
                    Invoke(nameof(AutoStart_StateUpdate), VRTConfig.Instance.AutoStart.autoDelay);
            }
        }

        private void AddSession()
        {
            string protocol = SessionConfig.Instance.protocolType;

            ScenarioRegistry.ScenarioInfo scenarioInfo = ScenarioRegistry.Instance.Scenarios[CreatePanelScenarioDropdown.value];
            Scenario scenario = scenarioInfo.AsScenario();
            OrchestratorController.Instance.AddSession(scenarioInfo.scenarioId,
                                                        scenario,
                                                        CreatePanelSessionNameField.text,
                                                        CreatePanelSessionDescriptionField.text,
                                                        protocol);
        }

        private void UpdateStateOnAddSession(Session session)
        {
            // Is equal to AddSession + Join Session, except that session is returned (not on JoinSession)
            if (session != null)
            {
                // update the list of available sessions
                JoinPanel_UpdateSessions();

                // Update the info in LobbyPanel
                LobbyPanelSessionName.text = session.sessionName;
                LobbyPanelSessionDescription.text = session.sessionDescription;
          
                // Update the list of session users
                LobbyPanel_UpdateSessionUsers();

                state = State.Lobby;
                AllPanels_UpdateAfterStateChange();
                // We may be able to advance auto-connection
                if (VRTConfig.Instance.AutoStart != null)
                    Invoke(nameof(AutoStart_StateUpdate), VRTConfig.Instance.AutoStart.autoDelay);
            }
            else
            {
                LobbyPanelSessionName.text = "";
                LobbyPanelSessionDescription.text = "";
                LobbyPanelScenarioName.text = "";
                LobbyPanelSessionNumUsers.text = "";
                _ClearScrollView(LobbyPanelSessionUsers.transform);
            }
        }

        private void JoinSession()
        {
            if (JoinPanelSessionDropdown.options.Count <= 0)
                Debug.LogError($"JoinSession: There are no sessions to join.");
            else
            {
                string sessionIdToJoin = OrchestratorController.Instance.AvailableSessions[JoinPanelSessionDropdown.value].sessionId;
                OrchestratorController.Instance.JoinSession(sessionIdToJoin);
            }
        }

        private void UpdateStateOnJoinSession(Session session)
        {
            if (session != null)
            {
                // Update the info in LobbyPanel
                LobbyPanelSessionName.text = session.sessionName;
                LobbyPanelSessionDescription.text = session.sessionDescription;

                // Update the list of session users
                LobbyPanel_UpdateSessionUsers();

                state = State.Lobby;
                AllPanels_UpdateAfterStateChange();
            }
            else
            {
                LobbyPanelSessionName.text = "";
                LobbyPanelSessionDescription.text = "";
                LobbyPanelScenarioName.text = "";
                LobbyPanelSessionNumUsers.text = "";
                _ClearScrollView(LobbyPanelSessionUsers.transform);
            }
        }

        private void LeaveSession()
        {
            OrchestratorController.Instance.LeaveSession();
        }

        private void UpdateStateOnLeaveSession()
        {
            LobbyPanelSessionName.text = "";
            LobbyPanelSessionDescription.text = "";
            LobbyPanelScenarioName.text = "";
            LobbyPanelSessionNumUsers.text = "";
            _ClearScrollView(LobbyPanelSessionUsers.transform);

            state = State.Play;
            AllPanels_UpdateAfterStateChange();
        }
        #endregion

        #region Orchstrator: Handling of messages within the session
        private void SendMessageToAll(string message)
        {
            OrchestratorController.Instance.SendMessageToAll(message);
        }

        private void OnUserMessageReceivedHandler(UserMessage userMessage)
        {
            LoginController.Instance.OnUserMessageReceived(userMessage.message);
        }

        private void OnMasterEventReceivedHandler(UserEvent pMasterEventData)
        {
            Debug.LogError("OrchestratorLogin: OnMasterEventReceivedHandler: Unexpected message from " + pMasterEventData.sceneEventFrom + ": " + pMasterEventData.sceneEventData);
        }

        private void OnUserEventReceivedHandler(UserEvent pUserEventData)
        {
            Debug.LogError("OrchestratorLogin: OnUserEventReceivedHandler: Unexpected message from " + pUserEventData.sceneEventFrom + ": " + pUserEventData.sceneEventData);
        }
        #endregion
    }
}
