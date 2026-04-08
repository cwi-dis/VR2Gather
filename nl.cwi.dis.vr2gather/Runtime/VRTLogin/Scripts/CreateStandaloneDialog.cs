using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VRT.Core;

namespace VRT.Login
{
    /// <summary>
    /// View for the "Create Standalone Session" screen.
    /// Like CreateDialog but without protocol selection, and uses a Start button
    /// that triggers immediate session launch (no lobby wait).
    /// </summary>
    public class CreateStandaloneDialog
    {
        public event Action<CreateSessionData> OnStartClicked;
        public event Action OnCancelClicked;

        private readonly Label _statusLabel;
        private readonly TextField _sessionNameField;
        private readonly DropdownField _scenarioDropdown;
        private readonly Label _scenarioDescriptionLabel;
        private readonly Toggle _uncompressedPointcloudsToggle;
        private readonly Toggle _uncompressedAudioToggle;
        private readonly Button _startButton;

        private List<ScenarioRegistry.ScenarioInfo> _scenarios;

        public CreateStandaloneDialog(VisualElement root)
        {
            _statusLabel = root.Q<Label>("StatusLabel");
            _sessionNameField = root.Q<TextField>("SessionNameField");
            _scenarioDropdown = root.Q<DropdownField>("ScenarioDropdown");
            _scenarioDescriptionLabel = root.Q<Label>("ScenarioDescriptionLabel");
            _uncompressedPointcloudsToggle = root.Q<Toggle>("UncompressedPointcloudsToggle");
            _uncompressedAudioToggle = root.Q<Toggle>("UncompressedAudioToggle");
            _startButton = root.Q<Button>("StartButton");

            root.Q<Button>("CancelButton").clicked += () => OnCancelClicked?.Invoke();
            _startButton.clicked += OnStartButtonClicked;
            _scenarioDropdown.RegisterValueChangedCallback(_ => UpdateScenarioDescription());

            PopulateScenarios();

            string time = DateTime.Now.ToString("HHmmss");
            string userName = VRTConfig.Instance.RepresentationConfig.userName;
            _sessionNameField.value = $"{userName}_{time}";

            _uncompressedPointcloudsToggle.value = SessionConfig.Instance.pointCloudCodec == "cwi0";
            _uncompressedAudioToggle.value = SessionConfig.Instance.voiceCodec == "VR2a";

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
            _startButton.SetEnabled(ready && HasValidScenario());
            if (ready) SetStatus("");
        }

        public void AutoFill(VRTConfig.AutoStartConfigType config)
        {
            if (!string.IsNullOrEmpty(config.sessionName))
                _sessionNameField.value = config.sessionName;

            if (!string.IsNullOrEmpty(config.sessionScenario))
            {
                int idx = _scenarios?.FindIndex(s => s.scenarioName == config.sessionScenario) ?? -1;
                if (idx >= 0) _scenarioDropdown.index = idx;
            }

            _uncompressedPointcloudsToggle.value = config.sessionUncompressed;
            _uncompressedAudioToggle.value = config.sessionUncompressedAudio;
        }

        private void PopulateScenarios()
        {
            _scenarios = ScenarioRegistry.Instance?.Scenarios ?? new List<ScenarioRegistry.ScenarioInfo>();
            var names = new List<string>();
            foreach (var sc in _scenarios) names.Add(sc.scenarioName);
            _scenarioDropdown.choices = names;
            if (names.Count > 0) _scenarioDropdown.index = 0;
            UpdateScenarioDescription();
        }

        private void UpdateScenarioDescription()
        {
            int idx = _scenarioDropdown.index;
            if (_scenarios != null && idx >= 0 && idx < _scenarios.Count)
            {
                _scenarioDescriptionLabel.text = _scenarios[idx].scenarioDescription;
                _startButton.SetEnabled(!string.IsNullOrEmpty(_scenarios[idx].scenarioId));
            }
            else
            {
                _scenarioDescriptionLabel.text = "(no scenario selected)";
                _startButton.SetEnabled(false);
            }
        }

        private bool HasValidScenario()
        {
            int idx = _scenarioDropdown.index;
            return _scenarios != null && idx >= 0 && idx < _scenarios.Count
                && !string.IsNullOrEmpty(_scenarios[idx].scenarioId);
        }

        private void OnStartButtonClicked()
        {
            int scenarioIdx = _scenarioDropdown.index;
            var scenario = _scenarios[scenarioIdx];
            var data = new CreateSessionData
            {
                sessionName = _sessionNameField.value,
                scenarioInfo = scenario,
                protocolType = "socketio",  // standalone always uses socketio for now
                uncompressedPointclouds = _uncompressedPointcloudsToggle.value,
                uncompressedAudio = _uncompressedAudioToggle.value,
            };
            OnStartClicked?.Invoke(data);
        }
    }
}
