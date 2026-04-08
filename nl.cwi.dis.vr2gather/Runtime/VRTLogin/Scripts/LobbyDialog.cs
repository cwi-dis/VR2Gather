using System;
using UnityEngine.UIElements;
using VRT.Orchestrator.Elements;

namespace VRT.Login
{
    /// <summary>
    /// View for the lobby (waiting room) screen.
    /// The controller updates session/user info via SetSession() and SetIsMaster().
    /// Start button is only enabled for the session master.
    /// </summary>
    public class LobbyDialog
    {
        public event Action OnStartClicked;
        public event Action OnLeaveClicked;

        private readonly Label _sessionNameLabel;
        private readonly Label _sessionDescriptionLabel;
        private readonly Label _scenarioLabel;
        private readonly Label _userCountLabel;
        private readonly ScrollView _userListScrollView;
        private readonly Button _startButton;

        public LobbyDialog(VisualElement root)
        {
            _sessionNameLabel = root.Q<Label>("SessionNameLabel");
            _sessionDescriptionLabel = root.Q<Label>("SessionDescriptionLabel");
            _scenarioLabel = root.Q<Label>("ScenarioLabel");
            _userCountLabel = root.Q<Label>("UserCountLabel");
            _userListScrollView = root.Q<ScrollView>("UserListScrollView");
            _startButton = root.Q<Button>("StartButton");

            root.Q<Button>("LeaveButton").clicked += () => OnLeaveClicked?.Invoke();
            _startButton.clicked += () => OnStartClicked?.Invoke();

            // Start hidden until we know if user is master
            _startButton.style.display = DisplayStyle.None;
        }

        public void SetIsMaster(bool isMaster)
        {
            _startButton.style.display = isMaster ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetSession(Session session)
        {
            if (session == null) return;

            _sessionNameLabel.text = session.sessionName;
            _sessionDescriptionLabel.text = session.sessionDescription;

            var scenarioInfo = ScenarioRegistry.Instance?.GetScenarioById(session.scenarioId);
            _scenarioLabel.text = scenarioInfo != null
                ? $"Scenario: {scenarioInfo.scenarioName}"
                : $"Scenario ID: {session.scenarioId}";

            SetUsers(session.GetUsers());
        }

        public void SetUsers(User[] users)
        {
            _userListScrollView.Clear();
            int count = users?.Length ?? 0;
            _userCountLabel.text = $"Participants: {count}";

            if (users == null) return;
            foreach (var user in users)
            {
                var label = new Label(user.userName)
                {
                    style = { fontSize = 22, color = new UnityEngine.Color(1, 1, 1) }
                };
                _userListScrollView.Add(label);
            }
        }
    }
}
