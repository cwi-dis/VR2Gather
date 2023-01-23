using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEditor;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.Voice;
using VRT.Core;
using Cwipc;
using VRT.Pilots.Common;

namespace VRT.Pilots.LoginManager
{

    public enum State
    {
        Offline, Online, Logged, Config, Play, Create, Join, Lobby, InGame
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

        public static OrchestratorLogin Instance { get { return instance; } }

        #region GUI Components

        [Tooltip("This user is the master of the session")]
        [DisableEditing] public bool isMaster = false;
        [DisableEditing] public string userID = "";

        private State state = State.Offline;
        private AutoState autoState = AutoState.DidNone;

        // Because we re-order the scenarios in the menu (to get usable ones near the top) we need to also keep
        // a list in order of the menu.
        private List<string> scenarioIDs;


        [Header("Developer")]
        [SerializeField] private Toggle developerModeButton = null;
        [SerializeField] private GameObject developerPanel = null;
        [SerializeField] private Text statusText = null;
        [SerializeField] private Text userId = null;
        [SerializeField] private Text userName = null;
        [SerializeField] private Text orchURLText = null;
        [SerializeField] private Text nativeVerText = null;
        [SerializeField] private Text playerVerText = null;
        [SerializeField] private Text orchVerText = null;
        [SerializeField] private Text ntpText = null;
        [SerializeField] private Button developerSessionButton = null;

        [Header("Connect")]
        [SerializeField] private GameObject connectPanel = null;
      
        [Header("Login")]
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
        [SerializeField] private GameObject webcamInfoGO = null;
        [SerializeField] private InputField tcpPointcloudURLConfigIF = null;
        [SerializeField] private InputField tcpAudioURLConfigIF = null;
        [SerializeField] private Dropdown representationTypeConfigDropdown = null;
        [SerializeField] private Dropdown webcamDropdown = null;
        [SerializeField] private Dropdown microphoneDropdown = null;
        [SerializeField] private RectTransform VUMeter = null;
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
        [SerializeField] private Toggle socketProtocolToggle = null;
        [SerializeField] private Toggle dashProtocolToggle = null;
        [SerializeField] private Toggle tcpProtocolToggle = null;
        [SerializeField] private Toggle uncompressedPointcloudsToggle = null;
        [SerializeField] private Toggle uncompressedAudioToggle = null;

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
            if (OrchestratorController.Instance.ConnectedUsers != null)
            {
                foreach (User u in OrchestratorController.Instance.ConnectedUsers)
                {
                    //AddTextComponentOnContent(container.transform, u.userName);
                    AddUserComponentOnContent(container.transform, u);
                }
                sessionNumUsersText.text = OrchestratorController.Instance.ConnectedUsers.Length.ToString() /*+ "/" + "4"*/;
                // We may be able to continue auto-starting
                if (VRTConfig.Instance.AutoStart != null)
                    Invoke("AutoStateUpdate", VRTConfig.Instance.AutoStart.autoDelay);
            }
            else
            {
                if (developerMode) Debug.Log("OrchestratorLogin: UpdateUsersSession: ConnectedUsers was null");
            }
        }

        private void UpdateSessions(Transform container, Dropdown dd)
        {
            RemoveComponentsFromList(container.transform);
            foreach (var session in OrchestratorController.Instance.AvailableSessions)
            {
                AddTextComponentOnContent(container.transform, session.GetGuiRepresentation());
            }

            string selectedOption = "";
            // store selected option in dropdown
            if (dd.options.Count > 0)
                selectedOption = dd.options[dd.value].text;
            // update the dropdown
            dd.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (var sess in OrchestratorController.Instance.AvailableSessions)
            {
                options.Add(new Dropdown.OptionData(sess.GetGuiRepresentation()));
            }
            dd.AddOptions(options);
            // re-assign selected option in dropdown
            if (dd.options.Count > 0)
            {
                for (int i = 0; i < dd.options.Count; ++i)
                {
                    if (dd.options[i].text == selectedOption)
                        dd.value = i;
                }
            }
        }

        private void UpdateScenarios(Dropdown dd)
        {
            // update the dropdown
            dd.ClearOptions();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            // Add scenarios we have implemented first, others afterwards after a blank line
            scenarioIDs = new List<string>();
            foreach (var scenario in OrchestratorController.Instance.AvailableScenarios)
            {

                if (PilotRegistry.Instance.GetSceneNameForPilotName(scenario.scenarioName, "") != null)
                {
                    options.Add(new Dropdown.OptionData(scenario.GetGuiRepresentation()));
                    scenarioIDs.Add(scenario.scenarioId);
                }
            }
            options.Add(new Dropdown.OptionData(""));
            scenarioIDs.Add("");
            foreach (var scenario in OrchestratorController.Instance.AvailableScenarios)
            {

                if (PilotRegistry.Instance.GetSceneNameForPilotName(scenario.scenarioName, "") == null)
                {
                    options.Add(new Dropdown.OptionData(scenario.GetGuiRepresentation()));
                    scenarioIDs.Add(scenario.scenarioId);
                }
            }
            dd.AddOptions(options);
        }

        private void UpdateRepresentations(Dropdown dd)
        {
            // Fill UserData representation dropdown according to UserRepresentationType enum declaration
            dd.ClearOptions();
            //dd.AddOptions(new List<string>(Enum.GetNames(typeof(UserRepresentationType))));
            List<string> finalNames = new List<string>();
            foreach (string type in Enum.GetNames(typeof(UserRepresentationType)))
            {
                string enumName;
                switch (type)
                {
                    case "__NONE__":
                        enumName = "No Representation";
                        break;
                    case "__2D__":
                        enumName = "Video Avatar";
                        break;
                    case "__AVATAR__":
                        enumName = "Avatar";
                        break;
                    case "__PCC_CWI_":
                        enumName = "PointCloud (RealSense)";
                        break;
                    case "__PCC_CWIK4A_":
                        enumName = "PointCloud (Kinect)";
                        break;
                    case "__PCC_PROXY__":
                        enumName = "PointCloud (5G phone)";
                        break;
                    case "__PCC_SYNTH__":
                        enumName = "Synthetic PointCloud";
                        break;
                    case "__PCC_PRERECORDED__":
                        enumName = "Prerecorded PointCloud";
                        break;
                    case "__SPECTATOR__":
                        enumName = "Voice-only Spectator";
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
                case UserRepresentationType.__NONE__:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URNoneIcon");
                    userRepresentationLobbyText.text = "NO REPRESENTATION";
                    break;
                case UserRepresentationType.__2D__:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URCamIcon");
                    userRepresentationLobbyText.text = "VIDEO";
                    break;
                case UserRepresentationType.__AVATAR__:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URAvatarIcon");
                    userRepresentationLobbyText.text = "AVATAR";
                    break;
                case UserRepresentationType.__PCC_CWI_:
                case UserRepresentationType.__PCC_CWIK4A_:
                case UserRepresentationType.__PCC_PROXY__:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URSingleIcon");
                    userRepresentationLobbyText.text = "POINTCLOUD";
                    break;
                case UserRepresentationType.__PCC_SYNTH__:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URAvatarIcon");
                    userRepresentationLobbyText.text = "SYNTHETIC PC";
                    break;
                case UserRepresentationType.__PCC_PRERECORDED__:
                    userRepresentationLobbyImage.sprite = Resources.Load<Sprite>("Icons/URSingleIcon");
                    userRepresentationLobbyText.text = "PRERECORDED PC";
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

        private void SetUserRepresentationDescription(UserRepresentationType _representationType)
        {
            // left change the icon 'userRepresentationLobbyImage'
            switch (_representationType)
            {
                case UserRepresentationType.__NONE__:
                    selfRepresentationDescription.text = "No representation, no audio. The user can only watch.";
                    break;
                case UserRepresentationType.__2D__:
                    selfRepresentationDescription.text = "Avatar with video window from your camera.";
                    break;
                case UserRepresentationType.__AVATAR__:
                    selfRepresentationDescription.text = "3D Synthetic Avatar.";
                    break;
                case UserRepresentationType.__PCC_CWI_:
                    selfRepresentationDescription.text = "Realistic point cloud user representation, captured with RealSense cameras.";
                    break;
                case UserRepresentationType.__PCC_CWIK4A_:
                    selfRepresentationDescription.text = "Realistic point cloud user representation, captured with Azure Kinect cameras.";
                    break;
                case UserRepresentationType.__PCC_PROXY__:
                    selfRepresentationDescription.text = "Realistic point cloud user representation, captured with 5G phone camera.";
                    break;
                case UserRepresentationType.__PCC_SYNTH__:
                    selfRepresentationDescription.text = "3D Synthetic point cloud avatar.";
                    break;
                case UserRepresentationType.__PCC_PRERECORDED__:
                    selfRepresentationDescription.text = "3D Pre-recorded point cloud.";
                    break;
                case UserRepresentationType.__SPECTATOR__:
                    selfRepresentationDescription.text = "No visual representation, only audio communication.";
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
            orchURLText.text = VRTConfig.Instance.orchestratorURL;
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
            developerModeButton.onValueChanged.AddListener(delegate { DeveloperModeButtonClicked(); });
            developerSessionButton.onClick.AddListener(delegate { StartDeveloperSession(); });
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

            socketProtocolToggle.isOn = true;
            dashProtocolToggle.isOn = false;
            tcpProtocolToggle.isOn = false;
            uncompressedPointcloudsToggle.isOn = SessionConfig.Instance.pointCloudCodec == "cwi0";
            uncompressedAudioToggle.isOn = SessionConfig.Instance.voiceCodec == "VR2a";

            if (OrchestratorController.Instance.UserIsLogged)
            { // Comes from another scene
              // Set status to online
                statusText.text = OrchestratorController.Instance.ConnectionStatus.ToString();
                statusText.color = connectedCol;
                FillSelfUserData();
                UpdateSessions(orchestratorSessions, sessionIdDrop);
                UpdateScenarios(scenarioIdDrop);
                Debug.Log("OrchestratorLogin: Coming from another Scene");

                OrchestratorController.Instance.OnLoginResponse(new ResponseStatus(), userId.text);
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
            if (state == State.Create)
            {
                AudioToggle();
            }
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
                    switch (config.sessionTransportProtocol)
                    {
                        case "socketio":
                            socketProtocolToggle.isOn = true;
                            break;
                        case "dash":
                            dashProtocolToggle.isOn = true;
                            break;
                        case "tcp":
                            tcpProtocolToggle.isOn = true;
                            break;
                        default:
                            Debug.LogError($"OrchestratorLogin: AutoStart: Unknown sessionTransportProtocol {config.sessionTransportProtocol}");
                            break;
                    }
                    SetProtocol(config.sessionTransportProtocol);
                }
                else
                {
                    // No default set. Use socketio.
                    socketProtocolToggle.isOn = true;
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
                        if (entry.text.Contains(config.sessionScenario + " "))
                        {
                            if (found)
                            {
                                Debug.LogError($"OrchestratorLogin: AutoStart: Multiple scenarios match {config.sessionScenario}");
                            }
                            found = true;
                            scenarioIdDrop.value = idx;
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
                    Invoke("AddSession", config.autoDelay);
                }
                autoState = AutoState.DidCompleteCreation;

            }
            if (state == State.Lobby && autoState == AutoState.DidCompleteCreation && config.autoStartWith >= 1)
            {
                if (sessionNumUsersText.text == config.autoStartWith.ToString())
                {
                    if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: autoCreate: starting with {config.autoStartWith} users");
                    Invoke("ReadyButton", config.autoDelay);
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
                        Invoke("JoinSession", config.autoDelay);
                    }
                }
            }
        }

        public void FillSelfUserData()
        {
            if (OrchestratorController.Instance == null || OrchestratorController.Instance.SelfUser == null) return;
            User user = OrchestratorController.Instance.SelfUser;

            // UserID & Name
            userId.text = user.userId;
            userName.text = user.userName;
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
            developerPanel.SetActive(developerMode);
            connectPanel.gameObject.SetActive(state == State.Offline);
            loginPanel.SetActive(state == State.Online);
            vrtPanel.SetActive(state == State.Logged);
            configPanel.SetActive(state == State.Config);
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
                case State.Logged:
                   
                    break;
                case State.Config:
                   
                    // Behaviour
                    SelfRepresentationChanger();
                    break;
                case State.Play:
                    
                    break;
                case State.Create:
                   
                    break;
                case State.Join:
                    // Behaviour
                    GetSessions();
                    break;
                case State.Lobby:
                    readyButton.gameObject.SetActive(OrchestratorController.Instance.UserIsMaster);
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
          
         
            if ((UserRepresentationType)representationTypeConfigDropdown.value == UserRepresentationType.__2D__)
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

        private void SigninButton()
        {
            loginPanel.SetActive(false);
            signinPanel.SetActive(true);
        }

        public void RegisterButton(bool register)
        {
            if (register)
            {
                if (userPasswordRegisterIF.text == confirmPasswordRegisterIF.text)
                {
                    SignIn();
                    confirmPasswordRegisterIF.textComponent.color = Color.white;
                }
                else
                {
                    confirmPasswordRegisterIF.textComponent.color = Color.red;
                }
            }
            else
            {
                loginPanel.SetActive(true);
                signinPanel.SetActive(false);
                confirmPasswordRegisterIF.textComponent.color = Color.white;
            }
        }

        public void SaveConfigButton()
        {
            selfRepresentationPreview.StopMicrophone();
            UpdateUserData();
            state = State.Logged;
            PanelChanger();
        }

        public void ExitConfigButton()
        {
            selfRepresentationPreview.StopMicrophone();
            GetUserInfo();
            state = State.Logged;
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
            cfg.scenarioName = OrchestratorController.Instance.MyScenario.scenarioName;
            cfg.scenarioVariant = null;
            // protocolType already set
            // pointCloudCodec, voiceCodec and videoCodec already set
            string message = JsonUtility.ToJson(cfg);
            SendMessageToAll("START_" + message);
        }

     

        #endregion

        #region Toggles 

        private void AudioToggle()
        {
            socketProtocolToggle.interactable = !socketProtocolToggle.isOn;
            dashProtocolToggle.interactable = !dashProtocolToggle.isOn;
            tcpProtocolToggle.interactable = !tcpProtocolToggle.isOn;
        }

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

        public void SetProtocol(string proto)
        {
            switch (proto)
            {
                case "socketio": // Socket
                    if (socketProtocolToggle.isOn)
                    {
                        // Set AudioType
                        SessionConfig.Instance.protocolType = SessionConfig.ProtocolType.SocketIO;
                        // Set Toggles
                        dashProtocolToggle.isOn = false;
                        tcpProtocolToggle.isOn = false;
                    }
                    break;
                case "dash": // Dash
                    if (dashProtocolToggle.isOn)
                    {
                        // Set AudioType
                        SessionConfig.Instance.protocolType = SessionConfig.ProtocolType.Dash;
                        // Set Toggles
                        socketProtocolToggle.isOn = false;
                        tcpProtocolToggle.isOn = false;
                    }
                    break;
                case "tcp": // Dash
                    if (tcpProtocolToggle.isOn)
                    {
                        // Set AudioType
                        SessionConfig.Instance.protocolType = SessionConfig.ProtocolType.TCP;
                        // Set Toggles
                        socketProtocolToggle.isOn = false;
                        dashProtocolToggle.isOn = false;
                    }
                    break;
                default:
                    break;
            }
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
            OrchestratorController.Instance.OnSignInEvent += OnSignIn;
            OrchestratorController.Instance.OnGetNTPTimeEvent += OnGetNTPTimeResponse;
            OrchestratorController.Instance.OnSessionsEvent += OnSessionsHandler;
            OrchestratorController.Instance.OnAddSessionEvent += OnAddSessionHandler;
            OrchestratorController.Instance.OnSessionInfoEvent += OnSessionInfoHandler;
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
        private void TerminateControllerEvents()
        {
            OrchestratorController.Instance.OnConnectionEvent -= OnConnect;
            OrchestratorController.Instance.OnConnectingEvent -= OnConnecting;
            OrchestratorController.Instance.OnConnectionEvent -= OnDisconnect;
            OrchestratorController.Instance.OnGetOrchestratorVersionEvent -= OnGetOrchestratorVersionHandler;
            OrchestratorController.Instance.OnLoginEvent -= OnLogin;
            OrchestratorController.Instance.OnLogoutEvent -= OnLogout;
            OrchestratorController.Instance.OnSignInEvent -= OnSignIn;
            OrchestratorController.Instance.OnGetNTPTimeEvent -= OnGetNTPTimeResponse;
            OrchestratorController.Instance.OnSessionsEvent -= OnSessionsHandler;
            OrchestratorController.Instance.OnAddSessionEvent -= OnAddSessionHandler;
            OrchestratorController.Instance.OnSessionInfoEvent -= OnSessionInfoHandler;
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

        private void socketDisconnect()
        {
            OrchestratorController.Instance.socketDisconnect();
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
            orchVerText.text = pVersion;
            OrchestratorController.Instance.GetNTPTime();
        }

        #endregion

      
        #region Login/Logout

        private void SignIn()
        {
            if (developerMode) Debug.Log("OrchestratorLogin: SignIn: Send SignIn registration for user " + userNameRegisterIF.text);
            OrchestratorController.Instance.SignIn(userNameRegisterIF.text, userPasswordRegisterIF.text);
        }

        private void OnSignIn()
        {
            if (developerMode) Debug.Log("OrchestratorLogin: OnSignIn: User " + userNameLoginIF.text + " successfully registered.");
            userNameLoginIF.text = userNameRegisterIF.text;
            userPasswordLoginIF.text = userPasswordRegisterIF.text;
            loginPanel.SetActive(true);
            signinPanel.SetActive(false);
        }

        // Login from the main buttons Login & Logout
        private void Login()
        {
            if (rememberMeButton.isOn)
            {
                PlayerPrefs.SetString("userNameLoginIF", userNameLoginIF.text);
                PlayerPrefs.SetString("userPasswordLoginIF", userPasswordLoginIF.text);
            }
            else
            {
                PlayerPrefs.DeleteKey("userNameLoginIF");
                PlayerPrefs.DeleteKey("userPasswordLoginIF");
            }
            // If we want to autoCreate or autoStart depending on username set the right config flags.
            if (VRTConfig.Instance.AutoStart != null && VRTConfig.Instance.AutoStart.autoCreateForUser != "")
            {
                bool isThisUser = VRTConfig.Instance.AutoStart.autoCreateForUser == userNameLoginIF.text;
                if (developerMode) Debug.Log($"OrchestratorLogin: AutoStart: user={userNameLoginIF.text} autoCreateForUser={VRTConfig.Instance.AutoStart.autoCreateForUser} isThisUser={isThisUser}");
                VRTConfig.Instance.AutoStart.autoCreate = isThisUser;
                VRTConfig.Instance.AutoStart.autoJoin = !isThisUser;
            }
            OrchestratorController.Instance.Login(userNameLoginIF.text, userPasswordLoginIF.text);
        }

        // Check saved used credentials.
        private void CheckRememberMe()
        {
            if (PlayerPrefs.HasKey("userNameLoginIF") && PlayerPrefs.HasKey("userPasswordLoginIF"))
            {
                rememberMeButton.isOn = true;
                userNameLoginIF.text = PlayerPrefs.GetString("userNameLoginIF");
                userPasswordLoginIF.text = PlayerPrefs.GetString("userPasswordLoginIF");
            }
            else
                rememberMeButton.isOn = false;
        }

        private void OnLogin(bool userLoggedSucessfully)
        {
            if (userLoggedSucessfully)
            {
                OrchestratorController.Instance.StartRetrievingData();

                // UserData info in Login
                //UserData lUserData = new UserData {
                //    userMQexchangeName = exchangeNameLoginIF.text,
                //    userMQurl = connectionURILoginIF.text,
                //    userRepresentationType = (UserRepresentationType)representationTypeLoginDropdown.value
                //};
                //OrchestratorController.Instance.UpdateUserData(lUserData);
                state = State.Logged;
            }
            else
            {
                this.userId.text = "";
                userName.text = "";
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
                Invoke("AutoStateUpdate", VRTConfig.Instance.AutoStart.autoDelay);
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
                userId.text = "";
                userName.text = "";
                userNameVRTText.text = "";
                state = State.Online;
            }
            PanelChanger();
        }

        #endregion

        #region NTP clock

        private void GetNTPTime()
        {
            OrchestratorController.Instance.GetNTPTime();
        }

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
                UpdateSessions(orchestratorSessions, sessionIdDrop);
                // We may be able to advance auto-connection
                if (VRTConfig.Instance.AutoStart != null)
                    Invoke("AutoStateUpdate", VRTConfig.Instance.AutoStart.autoDelay);
            }
        }

        private void AddSession()
        {
            OrchestratorController.Instance.AddSession(scenarioIDs[scenarioIdDrop.value],
                                                        sessionNameIF.text,
                                                        sessionDescriptionIF.text);
        }

        private void OnAddSessionHandler(Session session)
        {
            // Is equal to AddSession + Join Session, except that session is returned (not on JoinSession)
            if (session != null)
            {
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
                // We may be able to advance auto-connection
                if (VRTConfig.Instance.AutoStart != null)
                    Invoke("AutoStateUpdate", VRTConfig.Instance.AutoStart.autoDelay);
            }
            else
            {
                isMaster = false;
                sessionNameText.text = "";
                sessionDescriptionText.text = "";
                scenarioIdText.text = "";
                sessionNumUsersText.text = "";
                sessionMasterID = "";
                RemoveComponentsFromList(usersSession.transform);
            }
        }

        private void OnSessionInfoHandler(Session session)
        {
            if (session != null)
            {
                // Update the info in LobbyPanel
                isMaster = OrchestratorController.Instance.UserIsMaster;
                sessionNameText.text = session.sessionName;
                sessionDescriptionText.text = session.sessionDescription;
                if (session.sessionMaster != "")
                    sessionMasterID = OrchestratorController.Instance.GetUser(session.sessionMaster).userName;
                // Update the list of session users
                UpdateUsersSession(usersSession);
            }
            else
            {
                isMaster = false;
                sessionNameText.text = "";
                sessionDescriptionText.text = "";
                scenarioIdText.text = "";
                sessionNumUsersText.text = "";
                sessionMasterID = "";
                RemoveComponentsFromList(usersSession.transform);
            }
        }

        private void OnGetScenarioInstanceInfoHandler(ScenarioInstance scenario)
        {
            if (scenario != null)
            {
                scenarioIdText.text = scenario.scenarioName;
                // Update the list of session users
                UpdateUsersSession(usersSession);
            }
        }

        private void DeleteSession()
        {
            OrchestratorController.Instance.DeleteSession(OrchestratorController.Instance.MySession.sessionId);
        }

        private void OnDeleteSessionHandler()
        {
            if (developerMode) Debug.Log("OrchestratorLogin: OnDeleteSessionHandler: Session deleted");
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
            else
            {
                isMaster = false;
                sessionNameText.text = "";
                sessionDescriptionText.text = "";
                scenarioIdText.text = "";
                sessionNumUsersText.text = "";
                sessionMasterID = "";
                RemoveComponentsFromList(usersSession.transform);
            }
        }

        private void LeaveSession()
        {
            OrchestratorController.Instance.LeaveSession();
        }

        private void OnLeaveSessionHandler()
        {
            isMaster = false;
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
            if (!string.IsNullOrEmpty(userID))
            {
                OrchestratorController.Instance.GetUsers();
            }
        }

        private void OnUserLeftSessionHandler(string userID)
        {
            if (!string.IsNullOrEmpty(userID))
            {
                OrchestratorController.Instance.GetUsers();
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
            if (scenarios != null && scenarios.Length > 0)
            {
                //update the data in the dropdown
                UpdateScenarios(scenarioIdDrop);
                // We may be able to advance auto-connection
                if (VRTConfig.Instance.AutoStart != null)
                    Invoke("AutoStateUpdate", VRTConfig.Instance.AutoStart.autoDelay);
            }
        }

        #endregion

        #region Live

        private void OnGetLivePresenterDataHandler(LivePresenterData liveData)
        {
            //Debug.Log("[OrchestratorLogin][OnGetLivePresenterDataHandler] Not implemented");
        }

        #endregion

        #region Users

        private void GetUsers()
        {
            OrchestratorController.Instance.GetUsers();
        }

        private void OnGetUsersHandler(User[] users)
        {
            if (developerMode) Debug.Log("OrchestratorLogin: OnGetUsersHandler: Users Updated");

            // Update the sfuData if is in session.
            if (OrchestratorController.Instance.ConnectedUsers != null)
            {
                for (int i = 0; i < OrchestratorController.Instance.ConnectedUsers.Length; ++i)
                {
                    foreach (User u in users)
                    {
                        if (OrchestratorController.Instance.ConnectedUsers[i].userId == u.userId)
                        {
                            OrchestratorController.Instance.ConnectedUsers[i].sfuData = u.sfuData;
                            OrchestratorController.Instance.ConnectedUsers[i].userData = u.userData;
                        }
                    }
                }
            }

            UpdateUsersSession(usersSession);
        }

        private void AddUser()
        {
            if (developerMode) Debug.Log("OrchestratorLogin: AddUser: Send AddUser registration for user " + userNameRegisterIF.text);
            OrchestratorController.Instance.AddUser(userNameRegisterIF.text, userPasswordRegisterIF.text);
        }

        private void OnAddUserHandler(User user)
        {
            if (developerMode) Debug.Log("OrchestratorLogin: OnAddUserHandler: User " + user.userName + " registered with exit.");
            loginPanel.SetActive(true);
            signinPanel.SetActive(false);
            userNameLoginIF.text = userNameRegisterIF.text;
        }

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
            OrchestratorController.Instance.UpdateFullUserData(lUserData);
        }

        private void GetUserInfo()
        {
            OrchestratorController.Instance.GetUserInfo(OrchestratorController.Instance.SelfUser.userId);
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
                    userNameVRTText.text = user.userName;

                    //UserData
                    tcpPointcloudURLConfigIF.text = user.userData.userPCurl;
                    tcpAudioURLConfigIF.text = user.userData.userAudioUrl;
                    representationTypeConfigDropdown.value = (int)user.userData.userRepresentationType;

                    SetUserRepresentationGUI(user.userData.userRepresentationType);
                    // Session name

                    string time = DateTime.Now.ToString("hhmmss");
                    sessionNameIF.text = $"{user.userName}_{time}";

                }

                GetUsers(); // To update the user representation

                // Update the sfuData and UserData if is in session.
                if (OrchestratorController.Instance.ConnectedUsers != null)
                {
                    for (int i = 0; i < OrchestratorController.Instance.ConnectedUsers.Length; ++i)
                    {
                        if (OrchestratorController.Instance.ConnectedUsers[i].userId == user.userId)
                        {
                            // sfuData
                            OrchestratorController.Instance.ConnectedUsers[i].sfuData = user.sfuData;
                            // UserData
                            OrchestratorController.Instance.ConnectedUsers[i].userData = user.userData;
                        }
                    }
                }
            }
        }

        private void DeleteUser()
        {
            Debug.LogError("OrchestratorLogin: DeleteUser: Not implemented");
        }

        #endregion

        #region Rooms

        private void GetRooms()
        {
            OrchestratorController.Instance.GetRooms();
        }

        private void OnGetRoomsHandler(RoomInstance[] rooms)
        {
            if (developerMode) Debug.Log("OrchestratorLogin: OnGetRoomsHandler: Send GetUsers command");

            OrchestratorController.Instance.GetUsers();
        }

        private void JoinRoom()
        {
            Debug.LogError("OrchestratorLogin: JoinRoom: Not implemented");
        }

        private void OnJoinRoomHandler(bool hasJoined)
        {
            Debug.LogError("OrchestratorLogin: OnJoinRoomHandler: Not implemented");
        }

        private void LeaveRoom()
        {
            OrchestratorController.Instance.LeaveRoom();
        }

        private void OnLeaveRoomHandler()
        {
            Debug.LogError("OrchestratorLogin: OnLeaveRoomHandler: Not implemented");
        }

        #endregion

        #region Messages

        private void SendMessage()
        {
            Debug.LogError("OrchestratorLogin: SendMessage: Not implemented");
        }

        private void SendMessageToAll(string message)
        {
            OrchestratorController.Instance.SendMessageToAll(message);
        }

        private void OnUserMessageReceivedHandler(UserMessage userMessage)
        {
            LoginController.Instance.OnUserMessageReceived(userMessage.message);
        }

        #endregion

        #region Events

        private void SendEventToMaster()
        {
            Debug.LogError("OrchestratorLogin: SendEventToMaster: Not implemented");
        }

        private void SendEventToUser()
        {
            Debug.LogError("OrchestratorLogin: SendEventToUser: Not implemented");
        }

        private void SendEventToAll()
        {
            Debug.LogError("OrchestratorLogin: SendEventToAll: Not implemented");
        }

        private void OnMasterEventReceivedHandler(UserEvent pMasterEventData)
        {
            Debug.LogError("OrchestratorLogin: OnMasterEventReceivedHandler: Unexpected message from " + pMasterEventData.fromId + ": " + pMasterEventData.message);
        }

        private void OnUserEventReceivedHandler(UserEvent pUserEventData)
        {
            Debug.LogError("OrchestratorLogin: OnUserEventReceivedHandler: Unexpected message from " + pUserEventData.fromId + ": " + pUserEventData.message);
        }

        #endregion

        #region Data Stream

        private void GetAvailableDataStreams()
        {
            Debug.Log("[OrchestratorLogin][GetAvailableDataStreams] Not implemented");
        }

        private void GetRegisteredDataStreams()
        {
            OrchestratorController.Instance.GetRegisteredDataStreams();
        }

        #endregion

        #region Errors

        private void OnErrorHandler(ResponseStatus status)
        {
            Debug.Log("OrchestratorLogin: OnError: Error code: " + status.Error + ", Error message: " + status.Message);
            ErrorManager.Instance.EnqueueOrchestratorError(status.Error, status.Message);
        }

        #endregion

        #endregion

    }

}