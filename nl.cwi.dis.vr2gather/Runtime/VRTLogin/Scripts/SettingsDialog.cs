using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VRT.Core;

namespace VRT.Login
{
    /// <summary>
    /// View for the settings screen. Reads current values from VRTConfig on construction
    /// and writes them back to VRTConfig (and saves) when the user clicks Save.
    /// </summary>
    public class SettingsDialog
    {
        public event Action OnSaveClicked;
        public event Action OnCancelClicked;

        private readonly TextField _userNameField;
        private readonly DropdownField _representationDropdown;
        private readonly DropdownField _pointcloudVariantDropdown;
        private readonly DropdownField _webcamDropdown;
        private readonly DropdownField _microphoneDropdown;
        private readonly TextField _tcpURLField;

        public SettingsDialog(VisualElement root)
        {
            _userNameField = root.Q<TextField>("UserNameField");
            _representationDropdown = root.Q<DropdownField>("RepresentationDropdown");
            _pointcloudVariantDropdown = root.Q<DropdownField>("PointcloudVariantDropdown");
            _webcamDropdown = root.Q<DropdownField>("WebcamDropdown");
            _microphoneDropdown = root.Q<DropdownField>("MicrophoneDropdown");
            _tcpURLField = root.Q<TextField>("TCPURLField");

            root.Q<Button>("SaveButton").clicked += Save;
            root.Q<Button>("CancelButton").clicked += () => OnCancelClicked?.Invoke();

            PopulateFromConfig();
        }

        private void PopulateFromConfig()
        {
            VRTConfig.RepresentationConfigType config = VRTConfig.Instance.RepresentationConfig;

            _userNameField.value = config.userName;
            _tcpURLField.value = config.userRepresentationTCPUrl;

            // Representation dropdown
            var reprNames = new List<string>(Enum.GetNames(typeof(UserRepresentationType)));
            _representationDropdown.choices = reprNames;
            _representationDropdown.index = (int)config.representation;

            // Pointcloud variant dropdown
            var variantNames = new List<string>(Enum.GetNames(typeof(RepresentationPointcloudVariant)));
            _pointcloudVariantDropdown.choices = variantNames;
            _pointcloudVariantDropdown.index = (int)config.RepresentationPointcloudConfig.variant;

            // Webcam dropdown
            var webcams = new List<string> { "None" };
            foreach (WebCamDevice d in WebCamTexture.devices) webcams.Add(d.name);
            _webcamDropdown.choices = webcams;
            int webcamIdx = webcams.IndexOf(config.webcamName);
            _webcamDropdown.index = webcamIdx >= 0 ? webcamIdx : 0;

            // Microphone dropdown
            var mics = new List<string> { "None" };
            foreach (string d in Microphone.devices) mics.Add(d);
            _microphoneDropdown.choices = mics;
            int micIdx = mics.IndexOf(config.microphoneName);
            _microphoneDropdown.index = micIdx >= 0 ? micIdx : 0;
        }

        private void Save()
        {
            VRTConfig.RepresentationConfigType config = VRTConfig.Instance.RepresentationConfig;

            config.userName = _userNameField.value.Trim();
            config.userRepresentationTCPUrl = _tcpURLField.value.Trim();
            config.representation = (UserRepresentationType)_representationDropdown.index;
            config.RepresentationPointcloudConfig.variant = (RepresentationPointcloudVariant)_pointcloudVariantDropdown.index;
            config.webcamName = _webcamDropdown.value == "None" ? "" : _webcamDropdown.value;
            config.microphoneName = _microphoneDropdown.value == "None" ? "" : _microphoneDropdown.value;

            VRTConfig.Instance.SaveUserConfig();
            OnSaveClicked?.Invoke();
        }
    }
}
