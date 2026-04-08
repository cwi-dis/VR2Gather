using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
using VRT.Orchestrator.Responses;
using VRT.Orchestrator.Elements;
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
    /// destroyed. On entering Create/Join/CreateStandalone, a fresh OrchestratorController
    /// is instantiated and connected. Cancel always returns to Home, where cleanup happens.
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

        [Header("Orchestrator")]
        [Tooltip("Prefab that contains the OrchestratorController component")]
        [SerializeField] private GameObject orchestratorControllerPrefab;

        // ── Private state ───────────────────────────────────────────────────────
        private enum State { Home, Settings, Create, Join, CreateStandalone, Lobby }
        private State _state;

        private UIDocument _uiDocument;
        private VisualElement _contentSlot;
        private Label _versionLabel;

        // Active dialog instances (only one non-null at a time)
        private HomeDialog _homeDialog;
        private SettingsDialog _settingsDialog;
        private CreateDialog _createDialog;
        private JoinDialog _joinDialog;
        private CreateStandaloneDialog _createStandaloneDialog;
        private LobbyDialog _lobbyDialog;

        // Pending data set by the dialog before the async orchestrator operation completes
        private CreateSessionData _pendingSessionData;
        private bool _pendingAutoCreate;
        private bool _pendingAutoJoin;
        private string _pendingAutoJoinName;
        private int _autoStartWithUsers = -1;

        // ── Unity lifecycle ─────────────────────────────────────────────────────

        void Start()
        {
            _uiDocument = GetComponent<UIDocument>();
            var root = _uiDocument.rootVisualElement;
            _contentSlot = root.Q<VisualElement>("Content");
            _versionLabel = root.Q<Label>("VersionLabel");

            string version = Application.version;
            _versionLabel.text = $"v{version}";

            ShowHome();
        }

        // ── State transitions ───────────────────────────────────────────────────

        private void ShowHome()
        {
            CleanupOrchestrator();
            ClearContent();

            var clone = homeDialogAsset.CloneTree();
            _contentSlot.Add(clone);
            _homeDialog = new HomeDialog(clone);
            _homeDialog.OnCreateSessionClicked += ShowCreate;
            _homeDialog.OnJoinSessionClicked += ShowJoin;
            _homeDialog.OnCreateStandaloneClicked += ShowCreateStandalone;
            _homeDialog.OnSettingsClicked += ShowSettings;

            _state = State.Home;
            AutoStart_OnHome();
        }

        private void ShowSettings()
        {
            ClearContent();
            var clone = settingsDialogAsset.CloneTree();
            _contentSlot.Add(clone);
            _settingsDialog = new SettingsDialog(clone);
            _settingsDialog.OnSaveClicked += ShowHome;
            _settingsDialog.OnCancelClicked += ShowHome;

            _state = State.Settings;
        }

        private void ShowCreate()
        {
            ClearContent();
            var clone = createDialogAsset.CloneTree();
            _contentSlot.Add(clone);
            _createDialog = new CreateDialog(clone);
            _createDialog.OnCreateClicked += OnCreateSessionRequested;
            _createDialog.OnCancelClicked += ShowHome;

            _state = State.Create;
            StartOrchestratorConnection();
        }

        private void ShowJoin()
        {
            ClearContent();
            var clone = joinDialogAsset.CloneTree();
            _contentSlot.Add(clone);
            _joinDialog = new JoinDialog(clone);
            _joinDialog.OnJoinClicked += OnJoinSessionRequested;
            _joinDialog.OnCancelClicked += ShowHome;
            _joinDialog.OnRefreshClicked += RefreshSessions;

            _state = State.Join;
            StartOrchestratorConnection();
        }

        private void ShowCreateStandalone()
        {
            ClearContent();
            var clone = createStandaloneDialogAsset.CloneTree();
            _contentSlot.Add(clone);
            _createStandaloneDialog = new CreateStandaloneDialog(clone);
            _createStandaloneDialog.OnStartClicked += OnCreateStandaloneRequested;
            _createStandaloneDialog.OnCancelClicked += ShowHome;

            _state = State.CreateStandalone;
            StartOrchestratorConnection();
        }

        private void ShowLobby()
        {
            ClearContent();
            var clone = lobbyDialogAsset.CloneTree();
            _contentSlot.Add(clone);
            _lobbyDialog = new LobbyDialog(clone);
            _lobbyDialog.OnStartClicked += OnStartSessionRequested;
            _lobbyDialog.OnLeaveClicked += OnLeaveSessionRequested;

            _lobbyDialog.SetIsMaster(OrchestratorController.Instance.UserIsMaster);
            _lobbyDialog.SetSession(OrchestratorController.Instance.CurrentSession);

            _state = State.Lobby;
            AutoStart_OnLobby();
        }

        private void ClearContent()
        {
            _contentSlot.Clear();
            _homeDialog = null;
            _settingsDialog = null;
            _createDialog = null;
            _joinDialog = null;
            _createStandaloneDialog = null;
            _lobbyDialog = null;
        }

        // ── Orchestrator lifecycle ──────────────────────────────────────────────

        private void StartOrchestratorConnection()
        {
            if (orchestratorControllerPrefab == null)
            {
                Debug.LogError("OrchestratorLogin: orchestratorControllerPrefab not assigned");
                SetActiveDialogStatus("Error: OrchestratorController prefab missing", isError: true);
                return;
            }

            SetActiveDialogStatus("Connecting to orchestrator...");
            Instantiate(orchestratorControllerPrefab);

            RegisterOrchestratorEvents();
            OrchestratorController.Instance.SocketConnect(VRTConfig.Instance.orchestratorURL);
        }

        private void CleanupOrchestrator()
        {
            if (OrchestratorController.Instance == null) return;
            UnregisterOrchestratorEvents();
            Destroy(OrchestratorController.Instance.gameObject);
        }

        private void RegisterOrchestratorEvents()
        {
            var oc = OrchestratorController.Instance;
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
            var oc = OrchestratorController.Instance;
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
        }

        private void OnConnectionEvent(bool connected)
        {
            if (!connected)
            {
                SetActiveDialogStatus("Disconnected from orchestrator.", isError: true);
                return;
            }

            SetActiveDialogStatus("Logging in...");
            string userName = VRTConfig.Instance.RepresentationConfig.userName;
            OrchestratorController.Instance.Login(userName, "");
        }

        private void OnLoginEvent(bool success)
        {
            if (!success)
            {
                SetActiveDialogStatus("Login failed.", isError: true);
                return;
            }

            // Upload user data so other participants know our representation
            var config = VRTConfig.Instance.RepresentationConfig;
            OrchestratorController.Instance.SelfUser.userData = new UserData
            {
                userRepresentation = config.representation,
                userRepresentationTCPUrl = config.userRepresentationTCPUrl,
                hasVoice = !string.IsNullOrEmpty(config.microphoneName) && config.microphoneName != "None",
            };
            OrchestratorController.Instance.UpdateFullUserData(
                OrchestratorController.Instance.SelfUser.userData);

            SetActiveDialogStatus("Synchronising clocks...");
            OrchestratorController.Instance.GetVersion();
        }

        private void OnGetVersionEvent(string version)
        {
            OrchestratorController.Instance.GetNTPTime();
        }

        private void OnGetNTPTimeEvent(NtpClock ntpTime)
        {
            double diff = OrchestratorController.GetClockTimestamp(DateTime.UtcNow) - ntpTime.Timestamp;
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
                    RefreshSessions();
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
            // AddSession also joins the created session, so we go straight to lobby.
            ShowLobby();
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
            OrchestratorController.Instance.AddSession(
                data.scenarioInfo.scenarioId,
                scenario,
                data.sessionName,
                "",
                data.protocolType);
        }

        private void OnJoinSessionRequested(string sessionId)
        {
            SetActiveDialogStatus("Joining session...");
            OrchestratorController.Instance.JoinSession(sessionId);
        }

        private void OnCreateStandaloneRequested(CreateSessionData data)
        {
            _pendingSessionData = data;
            ApplyCodecConfig(data);

            var scenario = data.scenarioInfo.AsScenario();
            SetActiveDialogStatus("Creating standalone session...");
            // Create the session, then on OnAddSessionEvent we'll start it immediately.
            OrchestratorController.Instance.AddSession(
                data.scenarioInfo.scenarioId,
                scenario,
                data.sessionName,
                "",
                data.protocolType);
        }

        private void OnStartSessionRequested()
        {
            var cfg = SessionConfig.Instance;
            cfg.scenarioName = OrchestratorController.Instance.CurrentScenario?.scenarioName ?? "";
            cfg.scenarioVariant = null;
            string message = JsonUtility.ToJson(cfg);
            OrchestratorController.Instance.SendMessageToAll("START_" + message);
        }

        private void OnLeaveSessionRequested()
        {
            OrchestratorController.Instance.LeaveSession();
            // ShowHome() will be called via OnLeaveSessionEvent
        }

        private void RefreshSessions()
        {
            if (OrchestratorController.Instance != null)
                OrchestratorController.Instance.GetSessions();
        }

        // ── AutoStart ───────────────────────────────────────────────────────────

        private void AutoStart_OnHome()
        {
            var config = VRTConfig.Instance.AutoStartConfig;
            if (config == null || !config.autoLogin) return;
            if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
            {
                Debug.Log("OrchestratorLogin: AutoStart suppressed (Shift held)");
                return;
            }

            string userName = VRTConfig.Instance.RepresentationConfig.userName;
            // If autoCreateForUser is set, create for that user and join for all others.
            if (!string.IsNullOrEmpty(config.autoCreateForUser))
            {
                bool isCreateUser = config.autoCreateForUser.Equals(userName, StringComparison.OrdinalIgnoreCase);
                _pendingAutoCreate = isCreateUser;
                _pendingAutoJoin = !isCreateUser;
            }
            else
            {
                _pendingAutoCreate = config.autoCreate;
                _pendingAutoJoin = config.autoJoin;
            }

            _pendingAutoJoinName = config.sessionName;
            _autoStartWithUsers = config.autoStartWith;

            if (_pendingAutoCreate)
                Invoke(nameof(ShowCreate), config.autoDelay);
            else if (_pendingAutoJoin)
                Invoke(nameof(ShowJoin), config.autoDelay);
        }

        private void AutoStart_OnCreate()
        {
            if (!_pendingAutoCreate) return;
            var config = VRTConfig.Instance.AutoStartConfig;
            _createDialog?.AutoFill(config);
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
            // Same as Create autostart path but for standalone.
            if (!_pendingAutoCreate) return;
            var config = VRTConfig.Instance.AutoStartConfig;
            _createStandaloneDialog?.AutoFill(config);
            Invoke(nameof(AutoStart_TriggerCreateStandalone), config.autoDelay);
        }

        private void AutoStart_TriggerCreateStandalone()
        {
            // Mirror of AutoStart_TriggerCreate for standalone.
            AutoStart_TriggerCreate();
        }

        private void AutoStart_OnSessionsLoaded(Session[] sessions)
        {
            if (!_pendingAutoJoin || string.IsNullOrEmpty(_pendingAutoJoinName)) return;
            var config = VRTConfig.Instance.AutoStartConfig;
            Invoke(nameof(AutoStart_TriggerJoin), config.autoDelay);
        }

        private void AutoStart_TriggerJoin()
        {
            _joinDialog?.AutoJoin(_pendingAutoJoinName);
        }

        private void AutoStart_OnLobby()
        {
            // Nothing extra; lobby checks user count on each update.
        }

        private void AutoStart_OnLobbyUpdated(Session session)
        {
            if (_autoStartWithUsers <= 0) return;
            if (OrchestratorController.Instance == null || !OrchestratorController.Instance.UserIsMaster) return;

            var users = session.GetUsers();
            if (users != null && users.Length >= _autoStartWithUsers)
            {
                var config = VRTConfig.Instance.AutoStartConfig;
                _autoStartWithUsers = -1;  // prevent re-triggering
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
