using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using VRT.Core;
using VRT.Orchestrator;
using VRT.Pilots.Common;

namespace VRT.Login
{
    /// <summary>
    /// Data produced by Create and CreateStandalone dialogs and consumed by the controller.
    /// </summary>
    public struct CreateSessionData
    {
        public string sessionName;
        public ScenarioRegistry.ScenarioInfo scenarioInfo;
        public string protocolType;
        public bool uncompressedPointclouds;
        public bool uncompressedAudio;
    }

    /// <summary>
    /// Top-level controller for the VRTLoginManager scene.
    ///
    /// Owns the state machine and all orchestrator business logic.
    /// Each sub-dialog (HomeDialog, SettingsDialog, etc.) is a pure view that
    /// fires C# events; this controller reacts to those events and decides what to do.
    ///
    /// On entering the Home state, any existing OrchestratorController singleton is
    /// destroyed. On entering Create/Join, a fresh NetworkOrchestratorController is
    /// instantiated and connected. On entering CreateStandalone, a fresh
    /// StandaloneOrchestratorController is instantiated instead. Cancel always returns
    /// to Home, where cleanup happens.
    /// </summary>
    public class OrchestratorLogin : MonoBehaviour
    {
        // ── Inspector references ────────────────────────────────────────────────
        [Header("Sub-dialog UXML assets")]
        [SerializeField] private VisualTreeAsset homeDialogAsset;
        [SerializeField] private VisualTreeAsset settingsDialogAsset;
        [SerializeField] private VisualTreeAsset createDialogAsset;
        [SerializeField] private VisualTreeAsset joinDialogAsset;
        [SerializeField] private VisualTreeAsset createStandaloneDialogAsset;
        [SerializeField] private VisualTreeAsset lobbyDialogAsset;
        [SerializeField] private VisualTreeAsset previewDialogAsset;

        [Header("Orchestrator")]
        [Tooltip("Prefab that contains the NetworkOrchestratorController component")]
        [SerializeField] private GameObject networkControllerPrefab;
        [Tooltip("Prefab that contains the StandaloneOrchestratorController component")]
        [SerializeField] private GameObject standaloneControllerPrefab;

        [Header("Scene objects")]
        [Tooltip("Player used for this scene")]
        public PlayerControllerSelf player;
        [Tooltip("Camera used to render the preview; disabled when not in preview")]
        [SerializeField] private Camera previewCamera;

        // ── Private state ───────────────────────────────────────────────────────
        private enum State { Home, Settings, Create, Join, CreateStandalone, Lobby, Preview }
        private State _state;

        private UIDocument _uiDocument;
        private VisualElement _contentSlot;
        private Label _titleVersionLabel;
        private VisualElement _orchestratorCheckmark;
        private Label _orchestratorVersionLabel;
        private Label _autoStatusLabel;

        // Active dialog instances (only one non-null at a time)
        private HomeDialog _homeDialog;
        private SettingsDialog _settingsDialog;
        private CreateDialog _createDialog;
        private JoinDialog _joinDialog;
        private CreateStandaloneDialog _createStandaloneDialog;
        private LobbyDialog _lobbyDialog;
        private PreviewDialog _previewDialog;
        private RenderTexture _previewRenderTexture;

        // Pending data set by the dialog before the async orchestrator operation completes
        private CreateSessionData _pendingSessionData;
        private bool _pendingAutoCreate;
        private bool _pendingAutoJoin;
        private bool _pendingAutoCreateStandalone;
        private bool _autoStartDone;
        private bool _autoStartAlreadyStarted;

        // ── Unity lifecycle ─────────────────────────────────────────────────────

        void Start()
        {
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;
            _contentSlot = root.Q<VisualElement>("Content");
            _titleVersionLabel = root.Q<Label>("TitleVersionLabel");
            _orchestratorCheckmark = root.Q<VisualElement>("OrchestratorCheckmark");
            _orchestratorVersionLabel = root.Q<Label>("OrchestratorVersionLabel");
            _autoStatusLabel = root.Q<Label>("AutoStatusLabel");

            _titleVersionLabel.text = $"VR2Gather v{Application.version}";
            UpdateHeaderOrchestratorStatus(false, "Orchestrator: Not connected");

            ShowHome();
        }

        void Update()
        {
            if (_state == State.Preview)
                _previewDialog?.MarkPreviewDirty();
        }

        // ── State transitions ───────────────────────────────────────────────────

        private const int TransitionFadeMs = 150;

        /// <summary>
        /// Fades the content slot out, runs doTransition (which should call ClearContent
        /// and set up the new dialog), then fades back in. On the very first call
        /// (no existing content) skips the fade-out and only fades in.
        /// </summary>
        private void TransitionTo(System.Action doTransition)
        {
            if (_contentSlot.childCount == 0)
            {
                doTransition();
                _contentSlot.style.opacity = 0;
                _contentSlot.schedule.Execute(() => _contentSlot.style.opacity = 1).StartingIn(16);
                return;
            }
            _contentSlot.style.opacity = 0;
            _contentSlot.schedule.Execute(() =>
            {
                doTransition();
                _contentSlot.style.opacity = 1;
            }).StartingIn(TransitionFadeMs);
        }

        private void ShowHome()
        {
            TransitionTo(() => {
                CleanupOrchestrator();
                ClearContent();
                FixSelfRepresentation();

                var clone = homeDialogAsset.CloneTree();
                clone.style.flexGrow = 1;
                _contentSlot.Add(clone);
                _homeDialog = new HomeDialog(clone);
                _homeDialog.OnCreateSessionClicked += ShowCreate;
                _homeDialog.OnJoinSessionClicked += ShowJoin;
                _homeDialog.OnCreateStandaloneClicked += ShowCreateStandalone;
                _homeDialog.OnSettingsClicked += ShowSettings;
                _homeDialog.OnPreviewClicked += ShowPreview;
                _homeDialog.OnQuitClicked += OnQuitClicked;

                _state = State.Home;
                AutoStart_OnHome();
            });
        }

        private void ShowSettings()
        {
            TransitionTo(() => {
                ClearContent();
                var clone = settingsDialogAsset.CloneTree();
                clone.style.flexGrow = 1;
                _contentSlot.Add(clone);
                _settingsDialog = new SettingsDialog(clone);
                _settingsDialog.OnSaveClicked += ShowHome;
                _settingsDialog.OnCancelClicked += ShowHome;

                _state = State.Settings;
            });
        }

        private void ShowCreate()
        {
            TransitionTo(() => {
                ClearContent();
                var clone = createDialogAsset.CloneTree();
                clone.style.flexGrow = 1;
                _contentSlot.Add(clone);
                _createDialog = new CreateDialog(clone);
                _createDialog.OnCreateClicked += OnCreateSessionRequested;
                _createDialog.OnCancelClicked += ShowHome;

                _state = State.Create;
                StartOrchestratorConnection();
            });
        }

        private void ShowJoin()
        {
            TransitionTo(() => {
                ClearContent();
                var clone = joinDialogAsset.CloneTree();
                clone.style.flexGrow = 1;
                _contentSlot.Add(clone);
                _joinDialog = new JoinDialog(clone);
                _joinDialog.OnJoinClicked += OnJoinSessionRequested;
                _joinDialog.OnCancelClicked += ShowHome;
                _joinDialog.OnRefreshClicked += RefreshSessions;

                _state = State.Join;
                StartOrchestratorConnection();
            });
        }

        private void ShowPreview()
        {
            TransitionTo(() => {
                ClearContent();
                var clone = previewDialogAsset.CloneTree();
                clone.style.flexGrow = 1;
                _contentSlot.Add(clone);
                _previewDialog = new PreviewDialog(clone);
                _previewDialog.OnOkClicked += ShowHome;

                _state = State.Preview;

                if (previewCamera != null)
                {
                    _previewRenderTexture = new RenderTexture(1280, 720, 24);
                    previewCamera.targetTexture = _previewRenderTexture;
                    previewCamera.gameObject.SetActive(true);
                    _previewDialog.SetPreviewTexture(_previewRenderTexture);
                }
                else
                {
                    Debug.LogWarning("OrchestratorLogin: previewCamera not assigned");
                }
            });
        }

        private void ShowCreateStandalone()
        {
            TransitionTo(() => {
                ClearContent();
                var clone = createStandaloneDialogAsset.CloneTree();
                clone.style.flexGrow = 1;
                _contentSlot.Add(clone);
                _createStandaloneDialog = new CreateStandaloneDialog(clone);
                _createStandaloneDialog.OnStartClicked += OnCreateStandaloneRequested;
                _createStandaloneDialog.OnCancelClicked += ShowHome;

                _state = State.CreateStandalone;
                StartStandaloneOrchestrator();
            });
        }

        private void ShowLobby()
        {
            TransitionTo(() => {
                ClearContent();
                var clone = lobbyDialogAsset.CloneTree();
                clone.style.flexGrow = 1;
                _contentSlot.Add(clone);
                _lobbyDialog = new LobbyDialog(clone);
                _lobbyDialog.OnStartClicked += OnStartSessionRequested;
                _lobbyDialog.OnLeaveClicked += OnLeaveSessionRequested;

                _lobbyDialog.SetIsMaster(VRTOrchestratorSingleton.Login.UserIsMaster);
                _lobbyDialog.SetSession(VRTOrchestratorSingleton.Login.CurrentSession);

                int autoStartWith = VRTConfig.Instance.AutoStartConfig.autoStartWith;
                if (autoStartWith > 0)
                    _lobbyDialog.SetAutoStartInfo($"AutoStart: waiting for {autoStartWith} participant{(autoStartWith == 1 ? "" : "s")}");


                _state = State.Lobby;
                AutoStart_OnLobby();
            });
        }

        private void ClearContent()
        {
            if (_state == State.Preview && previewCamera != null)
            {
                previewCamera.gameObject.SetActive(false);
                previewCamera.targetTexture = null;
                if (_previewRenderTexture != null)
                {
                    _previewRenderTexture.Release();
                    _previewRenderTexture = null;
                }
            }

            _contentSlot.Clear();
            _homeDialog = null;
            _settingsDialog = null;
            _createDialog = null;
            _joinDialog = null;
            _createStandaloneDialog = null;
            _lobbyDialog = null;
            _previewDialog = null;
        }

        // ── Self representation ──────────────────────────────────────────────

        private void FixSelfRepresentation()
        {
            if (player == null)
            {
                Debug.Log("OrchestratorLogin: self representation not assigned yet");
                return;
            }
            player.SetUpSelfPlayerController();
        }
        
        // ── Orchestrator lifecycle ──────────────────────────────────────────────

        private void StartOrchestratorConnection()
        {
            if (networkControllerPrefab == null)
            {
                Debug.LogError("OrchestratorLogin: networkControllerPrefab not assigned");
                SetActiveDialogStatus("Error: NetworkOrchestratorController prefab missing", isError: true);
                return;
            }

            SetActiveDialogStatus("Connecting to orchestrator...");
            var go = Instantiate(networkControllerPrefab);
            go.SetActive(true);

            RegisterOrchestratorEvents();
            VRTOrchestratorSingleton.Login.Connect(VRTConfig.Instance.orchestratorURL);
        }

        private void StartStandaloneOrchestrator()
        {
            if (standaloneControllerPrefab == null)
            {
                Debug.LogError("OrchestratorLogin: standaloneControllerPrefab not assigned");
                SetActiveDialogStatus("Error: StandaloneOrchestratorController prefab missing", isError: true);
                return;
            }

            var go = Instantiate(standaloneControllerPrefab);
            go.SetActive(true);
            RegisterOrchestratorEvents();
            // SocketConnect on the standalone controller fires the connection → login →
            // version → ntp chain synchronously, ending with SetReady(true) on the dialog.
            VRTOrchestratorSingleton.Login.Connect("");
        }

        private void CleanupOrchestrator()
        {
            if (VRTOrchestratorSingleton.Login == null) return;
            UnregisterOrchestratorEvents();
            VRTOrchestratorSingleton.Login.Shutdown();
            UpdateHeaderOrchestratorStatus(false, "Orchestrator: Not connected");
        }

        private void UpdateAutoStatus(string text)
        {
            _autoStatusLabel.text = text;
        }

        private void UpdateHeaderOrchestratorStatus(bool connected, string statusText)
        {
            _orchestratorCheckmark.style.display = connected ? DisplayStyle.Flex : DisplayStyle.None;
            _orchestratorVersionLabel.text = statusText;
        }

        private void RegisterOrchestratorEvents()
        {
            var oc = VRTOrchestratorSingleton.Login;
            oc.OnConnectionEvent += OnConnectionEvent;
            oc.OnConnectingEvent += OnConnectingEvent;
            oc.OnLoginEvent += OnLoginEvent;
            oc.OnGetOrchestratorVersionEvent += OnGetVersionEvent;
            oc.OnGetNTPTimeEvent += OnGetNTPTimeEvent;
            oc.OnSessionsEvent += OnSessionsEvent;
            oc.OnAddSessionEvent += OnAddSessionEvent;
            oc.OnJoinSessionEvent += OnJoinSessionEvent;
            oc.OnLeaveSessionEvent += OnLeaveSessionEvent;
            oc.OnSessionInfoEvent += OnSessionInfoEvent;
            oc.OnUserJoinSessionEvent += OnUserJoinSessionEvent;
            oc.OnUserLeaveSessionEvent += OnUserLeaveSessionEvent;
            oc.OnUserMessageReceivedEvent += OnUserMessageReceivedEvent;
            oc.OnErrorEvent += OnErrorEvent;
        }

        private void UnregisterOrchestratorEvents()
        {
            var oc = VRTOrchestratorSingleton.Login;
            if (oc == null) return;
            oc.OnConnectionEvent -= OnConnectionEvent;
            oc.OnConnectingEvent -= OnConnectingEvent;
            oc.OnLoginEvent -= OnLoginEvent;
            oc.OnGetOrchestratorVersionEvent -= OnGetVersionEvent;
            oc.OnGetNTPTimeEvent -= OnGetNTPTimeEvent;
            oc.OnSessionsEvent -= OnSessionsEvent;
            oc.OnAddSessionEvent -= OnAddSessionEvent;
            oc.OnJoinSessionEvent -= OnJoinSessionEvent;
            oc.OnLeaveSessionEvent -= OnLeaveSessionEvent;
            oc.OnSessionInfoEvent -= OnSessionInfoEvent;
            oc.OnUserJoinSessionEvent -= OnUserJoinSessionEvent;
            oc.OnUserLeaveSessionEvent -= OnUserLeaveSessionEvent;
            oc.OnUserMessageReceivedEvent -= OnUserMessageReceivedEvent;
            oc.OnErrorEvent -= OnErrorEvent;
        }

        // ── Orchestrator event handlers ─────────────────────────────────────────

        private void OnConnectingEvent()
        {
            SetActiveDialogStatus("Connecting to orchestrator...");
            UpdateHeaderOrchestratorStatus(false, "Orchestrator: Connecting...");
        }

        private void OnConnectionEvent(bool connected)
        {
            if (!connected)
            {
                SetActiveDialogStatus("Disconnected from orchestrator.", isError: true);
                UpdateHeaderOrchestratorStatus(false, "Orchestrator: Not connected");
                return;
            }

            SetActiveDialogStatus("Connected to orchestrator.");
            UpdateHeaderOrchestratorStatus(true, "Orchestrator: Getting version...");
        }

        private void OnLoginEvent(bool success)
        {
            // xxxjack this is an implementation detail.
            if (!success)
            {
                SetActiveDialogStatus("Login failed.", isError: true);
                return;
            }
            SetActiveDialogStatus("Logged in to orchestrator");
            
            SetActiveDialogStatus("Synchronising clocks...");
            VRTOrchestratorSingleton.Login.GetVersion();
        }

        private void OnGetVersionEvent(string version)
        {
            UpdateHeaderOrchestratorStatus(true, $"Orchestrator: {version}");
            VRTOrchestratorSingleton.Login.GetNTPTime();
        }

        private void OnGetNTPTimeEvent(NtpClock ntpTime)
        {
            double diff = VRTOrchestratorSingleton.GetClockTimestamp(DateTime.UtcNow) - ntpTime.Timestamp;
            if (Math.Abs(diff) >= VRTConfig.Instance.ntpSyncThreshold)
            {
                Debug.LogWarning($"OrchestratorLogin: clock desync {diff:F2}s (threshold {VRTConfig.Instance.ntpSyncThreshold:F2}s)");
            }

            // Fully connected and logged in — enable the active dialog.
            switch (_state)
            {
                case State.Create:
                    _createDialog?.SetReady(true);
                    AutoStart_OnCreate();
                    break;
                case State.Join:
                    _joinDialog?.SetReady(true);
                    StartCoroutine(RefreshSessionsWhileNeeded());
                    break;
                case State.CreateStandalone:
                    _createStandaloneDialog?.SetReady(true);
                    AutoStart_OnCreateStandalone();
                    break;
            }
        }

        private void OnSessionsEvent(Session[] sessions)
        {
            if (_state == State.Join)
            {
                _joinDialog?.SetSessions(sessions);
                AutoStart_OnSessionsLoaded(sessions);
            }
        }

        private void OnAddSessionEvent(Session session)
        {
            if (session == null)
            {
                SetActiveDialogStatus("Failed to create session.", isError: true);
                return;
            }
            if (_state == State.CreateStandalone)
            {
                // No lobby for standalone: start immediately.
                OnStartSessionRequested();
            }
            else
            {
                // Regular create: wait in lobby for other participants.
                ShowLobby();
            }
        }

        private void OnJoinSessionEvent(Session session)
        {
            if (session == null)
            {
                SetActiveDialogStatus("Failed to join session.", isError: true);
                return;
            }
            ShowLobby();
        }

        private void OnLeaveSessionEvent()
        {
            ShowHome();
        }

        private void OnSessionInfoEvent(Session session)
        {
            if (_state == State.Lobby && session != null)
            {
                _lobbyDialog?.SetSession(session);
                AutoStart_OnLobbyUpdated(session);
            }
        }

        private void OnUserJoinSessionEvent(string userId)
        {
            // Session info will follow via OnSessionInfoEvent; nothing extra needed here.
        }

        private void OnUserLeaveSessionEvent(string userId)
        {
            // Session info will follow via OnSessionInfoEvent.
        }

        private void OnUserMessageReceivedEvent(UserMessage userMessage)
        {
            PilotController.Instance?.OnUserMessageReceived(userMessage.message);
        }

        private void OnErrorEvent(ResponseStatus status)
        {
            Debug.LogError($"OrchestratorLogin: orchestrator error {status.Error}: {status.Message}");
            SetActiveDialogStatus($"Error: {status.Message}", isError: true);
        }

        // ── Dialog-driven business logic ────────────────────────────────────────

        private void OnCreateSessionRequested(CreateSessionData data)
        {
            _pendingSessionData = data;
            ApplyCodecConfig(data);

            var scenario = data.scenarioInfo.AsScenario();
            SetActiveDialogStatus("Creating session...");
            VRTOrchestratorSingleton.Login.AddSession(
                data.scenarioInfo.scenarioId,
                scenario,
                data.sessionName,
                "",
                data.protocolType);
        }

        private void OnJoinSessionRequested(string sessionId)
        {
            SetActiveDialogStatus("Joining session...");
            VRTOrchestratorSingleton.Login.JoinSession(sessionId);
        }

        private void OnCreateStandaloneRequested(CreateSessionData data)
        {
            _pendingSessionData = data;
            ApplyCodecConfig(data);

            var scenario = data.scenarioInfo.AsScenario();
            SetActiveDialogStatus("Creating standalone session...");
            // Create the session, then on OnAddSessionEvent we'll start it immediately.
            VRTOrchestratorSingleton.Login.AddSession(
                data.scenarioInfo.scenarioId,
                scenario,
                data.sessionName,
                "",
                data.protocolType);
        }

        private void OnStartSessionRequested()
        {
            var cfg = SessionConfig.Instance;
            cfg.scenarioName = VRTOrchestratorSingleton.Login.CurrentScenario?.scenarioName ?? "";
            cfg.scenarioVariant = null;
            string message = JsonUtility.ToJson(cfg);
            VRTOrchestratorSingleton.Login.SendMessageToAll("START_" + message);
        }

        private void OnQuitClicked()
        {
            PilotController.Instance.OnUserCommand("exit");
        }

        private void OnLeaveSessionRequested()
        {
            VRTOrchestratorSingleton.Login.LeaveSession();
            // ShowHome() will be called via OnLeaveSessionEvent
        }

        private void RefreshSessions()
        {
            if (VRTOrchestratorSingleton.Login != null)
            {
                VRTOrchestratorSingleton.Login.GetSessions();
            }
        }

        private IEnumerator RefreshSessionsWhileNeeded()
        {
            while (VRTOrchestratorSingleton.Login != null && _state == State.Join)
            {
                VRTOrchestratorSingleton.Login.GetSessions();
                yield return new WaitForSeconds(5);
            }
        }

        // ── AutoStart ───────────────────────────────────────────────────────────

        private void AutoStart_OnHome()
        {
            var config = VRTConfig.Instance.AutoStartConfig;
            if (config == null || _autoStartDone) return;
            string userName = VRTConfig.Instance.RepresentationConfig.userName;
            if (!string.IsNullOrEmpty(config.autoCreateForUser))
            {
                bool isCreateUser = config.autoCreateForUser.Equals(userName, StringComparison.OrdinalIgnoreCase);
                _pendingAutoCreate = isCreateUser;
                _pendingAutoJoin = !isCreateUser;
                _pendingAutoCreateStandalone = false;
            }
            else
            {
                _pendingAutoCreate = config.autoCreate;
                _pendingAutoJoin = config.autoJoin;
                _pendingAutoCreateStandalone = config.autoCreateStandalone;
            }

            if (!_pendingAutoCreate && !_pendingAutoJoin && !_pendingAutoCreateStandalone) return;

            int count = (_pendingAutoCreate ? 1 : 0) + (_pendingAutoJoin ? 1 : 0) + (_pendingAutoCreateStandalone ? 1 : 0);
            if (count > 1)
            {
                Debug.LogError("OrchestratorLogin: AutoStart config error — multiple auto-actions requested. Suppressing auto-start.");
                _pendingAutoCreate = _pendingAutoJoin = _pendingAutoCreateStandalone = false;
                return;
            }

            _autoStartAlreadyStarted = false;
            _autoStartDone = true;

            UpdateAutoStatus("AutoStart: CapsLock to cancel");
            // Defer the actual trigger so the user has autoDelay seconds to press CapsLock.
            Invoke(nameof(AutoStart_Fire), config.autoDelay);
        }

        private void AutoStart_Fire()
        {
            if (Keyboard.current != null && Keyboard.current.capsLockKey.isPressed)
            {
                Debug.Log("OrchestratorLogin: AutoStart suppressed (CapsLock held)");
                UpdateAutoStatus("AutoStart: cancelled");
                return;
            }

            if (_pendingAutoCreate)
                ShowCreate();
            else if (_pendingAutoJoin)
                ShowJoin();
            else
                ShowCreateStandalone();
        }

        private void AutoStart_OnCreate()
        {
            if (!_pendingAutoCreate) return;
            var config = VRTConfig.Instance.AutoStartConfig;
            _createDialog?.AutoFill(config);
            UpdateAutoStatus($"AutoStart: Creating session \"{config.sessionName}\"...");
            // The dialog's Create button click is triggered programmatically after a delay.
            Invoke(nameof(AutoStart_TriggerCreate), config.autoDelay);
        }

        private void AutoStart_TriggerCreate()
        {
            if (_createDialog == null) return;
            // Reuse the same logic as clicking Create: read current dialog state.
            // We achieve this by having the dialog fire OnCreateClicked normally.
            // Since we can't click the button programmatically, we duplicate the logic here.
            var config = VRTConfig.Instance.AutoStartConfig;
            var scenarios = ScenarioRegistry.Instance?.Scenarios;
            ScenarioRegistry.ScenarioInfo scenarioInfo = null;
            if (scenarios != null)
            {
                int idx = scenarios.FindIndex(s => s.scenarioName == config.sessionScenario);
                if (idx >= 0) scenarioInfo = scenarios[idx];
                else if (scenarios.Count > 0) scenarioInfo = scenarios[0];
            }
            if (scenarioInfo == null) return;

            ApplyCodecConfig(new CreateSessionData {
                uncompressedPointclouds = config.sessionUncompressed,
                uncompressedAudio = config.sessionUncompressedAudio,
            });

            var data = new CreateSessionData
            {
                sessionName = string.IsNullOrEmpty(config.sessionName)
                    ? _pendingSessionData.sessionName
                    : config.sessionName,
                scenarioInfo = scenarioInfo,
                protocolType = string.IsNullOrEmpty(config.sessionTransportProtocol)
                    ? "socketio"
                    : config.sessionTransportProtocol,
                uncompressedPointclouds = config.sessionUncompressed,
                uncompressedAudio = config.sessionUncompressedAudio,
            };
            OnCreateSessionRequested(data);
        }

        private void AutoStart_OnCreateStandalone()
        {
            if (!_pendingAutoCreateStandalone) return;
            var config = VRTConfig.Instance.AutoStartConfig;
            _createStandaloneDialog?.AutoFill(config);
            UpdateAutoStatus($"AutoStart: Creating standalone session \"{config.sessionName}\"...");
            Invoke(nameof(AutoStart_TriggerCreateStandalone), config.autoDelay);
        }

        private void AutoStart_TriggerCreateStandalone()
        {
            var config = VRTConfig.Instance.AutoStartConfig;
            var scenarios = ScenarioRegistry.Instance?.Scenarios;
            ScenarioRegistry.ScenarioInfo scenarioInfo = null;
            if (scenarios != null)
            {
                int idx = scenarios.FindIndex(s => s.scenarioName == config.sessionScenario);
                if (idx >= 0) scenarioInfo = scenarios[idx];
                else if (scenarios.Count > 0) scenarioInfo = scenarios[0];
            }
            if (scenarioInfo == null) return;

            var data = new CreateSessionData
            {
                sessionName = string.IsNullOrEmpty(config.sessionName)
                    ? _pendingSessionData.sessionName
                    : config.sessionName,
                scenarioInfo = scenarioInfo,
                protocolType = "socketio",
                uncompressedPointclouds = false,
                uncompressedAudio = false,
            };
            OnCreateStandaloneRequested(data);
        }

        private void AutoStart_OnSessionsLoaded(Session[] sessions)
        {
            var config = VRTConfig.Instance.AutoStartConfig;
            if (!_pendingAutoJoin || string.IsNullOrEmpty(config.sessionName)) return;
            _joinDialog?.SetStatus($"AutoJoin: waiting for session \"{config.sessionName}\"");
            UpdateAutoStatus($"AutoStart: Waiting for session \"{config.sessionName}\"...");
            Invoke(nameof(AutoStart_TriggerJoin), config.autoDelay);
        }

        private void AutoStart_TriggerJoin()
        {
            UpdateAutoStatus($"AutoStart: Joining session \"{VRTConfig.Instance.AutoStartConfig.sessionName}\"...");
            _joinDialog?.AutoJoin(VRTConfig.Instance.AutoStartConfig.sessionName);
        }

        private void AutoStart_OnLobby()
        {
            // Check immediately in case we're already at the required user count.
            var session = VRTOrchestratorSingleton.Login?.CurrentSession;
            if (session != null)
                AutoStart_OnLobbyUpdated(session);
        }

        private void AutoStart_OnLobbyUpdated(Session session)
        {
            var config = VRTConfig.Instance.AutoStartConfig;
            if (config.autoStartWith <= 0 || _autoStartAlreadyStarted) return;
            if (VRTOrchestratorSingleton.Login == null || !VRTOrchestratorSingleton.Login.UserIsMaster) return;

            var users = session.GetUsers();
            if (users == null) return;
            UpdateAutoStatus($"AutoStart: Waiting for participants ({users.Length}/{config.autoStartWith})...");
            if (users.Length >= config.autoStartWith)
            {
                _autoStartAlreadyStarted = true;
                UpdateAutoStatus("AutoStart: Starting session...");
                Invoke(nameof(OnStartSessionRequested), config.autoDelay);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private void SetActiveDialogStatus(string message, bool isError = false)
        {
            _createDialog?.SetStatus(message, isError);
            _joinDialog?.SetStatus(message, isError);
            _createStandaloneDialog?.SetStatus(message, isError);
        }

        private static void ApplyCodecConfig(CreateSessionData data)
        {
            SessionConfig.Instance.pointCloudCodec = data.uncompressedPointclouds ? "cwi0" : "cwi1";
            SessionConfig.Instance.voiceCodec = data.uncompressedAudio ? "VR2a" : "VR2A";
            SessionConfig.Instance.protocolType = data.protocolType;
        }
    }
}
