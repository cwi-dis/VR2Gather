using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VRT.Core;

namespace VRT.Login
{
    /// <summary>
    /// View for the "Create Session" screen.
    /// Exposes OnCreateClicked with a CreateSessionData payload.
    /// The controller calls SetStatus() to show connecting state and enable/disable buttons.
    /// </summary>
    public class CreateDialog
    {
        public event Action<CreateSessionData> OnCreateClicked;
        public event Action OnCancelClicked;

        private readonly Label _statusLabel;
        private readonly TextField _sessionNameField;
        private readonly DropdownField _scenarioDropdown;
        private readonly Label _scenarioDescriptionLabel;
        private readonly DropdownField _protocolDropdown;
        private readonly Toggle _uncompressedPointcloudsToggle;
        private readonly Toggle _uncompressedAudioToggle;
        private readonly Button _createButton;

        private List<ScenarioRegistry.ScenarioInfo> _scenarios;

        public CreateDialog(VisualElement root)
        {
            _statusLabel = root.Q<Label>("StatusLabel");
            _sessionNameField = root.Q<TextField>("SessionNameField");
            _scenarioDropdown = root.Q<DropdownField>("ScenarioDropdown");
            _scenarioDescriptionLabel = root.Q<Label>("ScenarioDescriptionLabel");
            _protocolDropdown = root.Q<DropdownField>("ProtocolDropdown");
            _uncompressedPointcloudsToggle = root.Q<Toggle>("UncompressedPointcloudsToggle");
            _uncompressedAudioToggle = root.Q<Toggle>("UncompressedAudioToggle");
            _createButton = root.Q<Button>("CreateButton");

            root.Q<Button>("CancelButton").clicked += () => OnCancelClicked?.Invoke();
            _createButton.clicked += OnCreateButtonClicked;
            _scenarioDropdown.RegisterValueChangedCallback(_ => UpdateScenarioDescription());

            PopulateScenarios();
            PopulateProtocols();

            // Set a default session name
            string time = DateTime.Now.ToString("HHmmss");
            string userName = VRTConfig.Instance.RepresentationConfig.userName;
            _sessionNameField.value = $"{userName}_{time}";

            // Default codec settings from SessionConfig
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
            _createButton.SetEnabled(ready && HasValidScenario());
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

            if (!string.IsNullOrEmpty(config.sessionTransportProtocol))
            {
                int idx = _protocolDropdown.choices.IndexOf(config.sessionTransportProtocol);
                if (idx >= 0) _protocolDropdown.index = idx;
            }

            _uncompressedPointcloudsToggle.value = config.sessionUncompressed;
            _uncompressedAudioToggle.value = config.sessionUncompressedAudio;
        }

        private void PopulateScenarios()
        {
            _scenarios = ScenarioRegistry.Instance?.Scenarios ?? new List<ScenarioRegistry.ScenarioInfo>();
            var names = new List<string>();
            foreach (var sc in _scenarios)
                names.Add(sc.scenarioName);
            _scenarioDropdown.choices = names;
            if (names.Count > 0) _scenarioDropdown.index = 0;
            UpdateScenarioDescription();
        }

        private void PopulateProtocols()
        {
            var names = new List<string>(TransportProtocol.GetNames());
            _protocolDropdown.choices = names;
            if (names.Count > 0) _protocolDropdown.index = 0;
        }

        private void UpdateScenarioDescription()
        {
            int idx = _scenarioDropdown.index;
            if (_scenarios != null && idx >= 0 && idx < _scenarios.Count)
            {
                _scenarioDescriptionLabel.text = _scenarios[idx].scenarioDescription;
                _createButton.SetEnabled(
                    !string.IsNullOrEmpty(_scenarios[idx].scenarioId));
            }
            else
            {
                _scenarioDescriptionLabel.text = "(no scenario selected)";
                _createButton.SetEnabled(false);
            }
        }

        private bool HasValidScenario()
        {
            int idx = _scenarioDropdown.index;
            return _scenarios != null && idx >= 0 && idx < _scenarios.Count
                && !string.IsNullOrEmpty(_scenarios[idx].scenarioId);
        }

        private void OnCreateButtonClicked()
        {
            int scenarioIdx = _scenarioDropdown.index;
            var scenario = _scenarios[scenarioIdx];
            var data = new CreateSessionData
            {
                sessionName = _sessionNameField.value,
                scenarioInfo = scenario,
                protocolType = _protocolDropdown.value,
                uncompressedPointclouds = _uncompressedPointcloudsToggle.value,
                uncompressedAudio = _uncompressedAudioToggle.value,
            };
            OnCreateClicked?.Invoke(data);
        }
    }
}
