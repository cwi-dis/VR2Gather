using System;
using UnityEngine;
using UnityEngine.UIElements;
using VRT.Orchestrator;

namespace VRT.Login
{
    /// <summary>
    /// View for the "Join Session" screen.
    /// The controller populates sessions via SetSessions() and calls SetStatus() to
    /// reflect connection state. OnJoinClicked carries the selected session ID.
    /// </summary>
    public class JoinDialog
    {
        public event Action<string> OnJoinClicked;
        public event Action OnCancelClicked;
        public event Action OnRefreshClicked;

        private readonly Label _statusLabel;
        private readonly ScrollView _sessionListScrollView;
        private readonly Label _sessionDescriptionLabel;
        private readonly Button _joinButton;

        private Session[] _sessions = Array.Empty<Session>();
        private int _selectedIndex = -1;

        // CSS class applied to the currently-selected session button
        private const string SelectedClass = "vrt-list-item--selected";

        public JoinDialog(VisualElement root)
        {
            _statusLabel = root.Q<Label>("StatusLabel");
            _sessionListScrollView = root.Q<ScrollView>("SessionListScrollView");
            _sessionDescriptionLabel = root.Q<Label>("SessionDescriptionLabel");
            _joinButton = root.Q<Button>("JoinButton");

            root.Q<Button>("CancelButton").clicked += () => OnCancelClicked?.Invoke();
            root.Q<Button>("RefreshButton").clicked += () => OnRefreshClicked?.Invoke();
            _joinButton.clicked += OnJoinButtonClicked;

            SetReady(false);
        }

        public void SetStatus(string message, bool isError = false)
        {
            _statusLabel.text = message;
            _statusLabel.style.color = isError
                ? new StyleColor(new Color(1f, 0.3f, 0.3f))
                : new StyleColor(new Color(1f, 0.78f, 0f));
            _statusLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void SetReady(bool ready)
        {
            _joinButton.SetEnabled(ready && _selectedIndex >= 0);
            if (ready) SetStatus("");
        }

        public void SetSessions(Session[] sessions)
        {
            // Keep old selected session (if any) so we can re-select it.
            var oldSelectedIndex = _selectedIndex;
            string oldSelectedSessionId = null;
            if (_sessions != null && oldSelectedIndex >= 0 && oldSelectedIndex < sessions.Length)
            {
                oldSelectedSessionId = _sessions[oldSelectedIndex].sessionId;
            }
            _sessions = sessions ?? Array.Empty<Session>();
            _selectedIndex = -1;
            _sessionDescriptionLabel.text = "";
            _sessionListScrollView.Clear();

            for (int i = 0; i < _sessions.Length; i++)
            {
                int capturedIndex = i;
                var session = _sessions[i];
                var btn = new Button(() => SelectSession(capturedIndex))
                {
                    text = session.GetGuiRepresentation(),
                    name = $"Session_{i}"
                };
                btn.AddToClassList("vrt-list-item");
                _sessionListScrollView.Add(btn);
            }
            // See if we can re-select the session.
            if (oldSelectedSessionId != null && oldSelectedIndex >= 0 && oldSelectedIndex < _sessions.Length &&
                _sessions[oldSelectedIndex].sessionId == oldSelectedSessionId)
            {
                // The session hasn't changed. Re-select it.
                SelectSession(oldSelectedIndex);
                _joinButton.SetEnabled(true);
            }
            else
            {
                _joinButton.SetEnabled(false);
            }
        }

        /// <summary>After sessions are loaded, auto-join the one matching sessionName.</summary>
        public void AutoJoin(string sessionName)
        {
            for (int i = 0; i < _sessions.Length; i++)
            {
                if (_sessions[i].sessionName == sessionName
                    || _sessions[i].GetGuiRepresentation().StartsWith(sessionName + " "))
                {
                    SelectSession(i);
                    OnJoinButtonClicked();
                    return;
                }
            }
        }

        private void SelectSession(int index)
        {
            // Clear previous selection highlight
            for (int i = 0; i < _sessionListScrollView.childCount; i++)
            {
                _sessionListScrollView[i].RemoveFromClassList(SelectedClass);
            }

            _selectedIndex = index;
            _sessionListScrollView[index].AddToClassList(SelectedClass);

            // Build description
            var session = _sessions[index];
            var masterUser = session.GetUser(session.sessionMaster);
            string masterName = masterUser?.userName ?? session.sessionMaster;
            var scenarioInfo = ScenarioRegistry.Instance?.GetScenarioById(session.scenarioId);
            if (scenarioInfo == null)
            {
                _sessionDescriptionLabel.text =
                    $"{session.sessionName} by {masterName}\nCannot join: scenario not implemented here.";
                _joinButton.SetEnabled(false);
            }
            else
            {
                _sessionDescriptionLabel.text =
                    $"{session.sessionName} by {masterName}\n{scenarioInfo.scenarioDescription}";
                _joinButton.SetEnabled(true);
            }
        }

        private void OnJoinButtonClicked()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _sessions.Length) return;
            OnJoinClicked?.Invoke(_sessions[_selectedIndex].sessionId);
        }
    }
}
