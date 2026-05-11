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
    /// Sections are shown/hidden based on the selected representation and pointcloud variant.
    /// </summary>
    public class SettingsDialog
    {
        public event Action OnSaveClicked;
        public event Action OnCancelClicked;

        private const int TransitionFadeMs = 150;
        private readonly ScrollView _formScrollView;

        // Always-visible fields
        private readonly TextField _userNameField;
        private readonly DropdownField _representationDropdown;
        private readonly DropdownField _webcamDropdown;
        private readonly DropdownField _microphoneDropdown;
        private readonly TextField _tcpURLField;

        // Conditional sections
        private readonly VisualElement _webcamRow;
        private readonly VisualElement _pointcloudSection;
        private readonly VisualElement _cameraVariantSection;
        private readonly VisualElement _remoteVariantSection;
        private readonly VisualElement _proxyVariantSection;
        private readonly VisualElement _syntheticVariantSection;
        private readonly VisualElement _prerecordedVariantSection;

        // Pointcloud fields
        private readonly DropdownField _pointcloudVariantDropdown;
        private readonly TextField _cameraConfigFilenameField;
        private readonly TextField _remoteUrlField;
        private readonly Toggle _remoteIsCompressedToggle;
        private readonly TextField _proxyLocalIPField;
        private readonly TextField _proxyPortField;
        private readonly TextField _syntheticNPointsField;
        private readonly TextField _prerecordedFolderField;
        private readonly TextField _voxelSizeField;
        private readonly TextField _frameRateField;

        public SettingsDialog(VisualElement root)
        {
            _formScrollView = root.Q<ScrollView>("FormScrollView");
            _userNameField = root.Q<TextField>("UserNameField");
            _representationDropdown = root.Q<DropdownField>("RepresentationDropdown");
            _webcamDropdown = root.Q<DropdownField>("WebcamDropdown");
            _microphoneDropdown = root.Q<DropdownField>("MicrophoneDropdown");
            _tcpURLField = root.Q<TextField>("TCPURLField");

            _webcamRow = root.Q<VisualElement>("WebcamRow");
            _pointcloudSection = root.Q<VisualElement>("PointcloudSection");
            _cameraVariantSection = root.Q<VisualElement>("CameraVariantSection");
            _remoteVariantSection = root.Q<VisualElement>("RemoteVariantSection");
            _proxyVariantSection = root.Q<VisualElement>("ProxyVariantSection");
            _syntheticVariantSection = root.Q<VisualElement>("SyntheticVariantSection");
            _prerecordedVariantSection = root.Q<VisualElement>("PrerecordedVariantSection");

            _pointcloudVariantDropdown = root.Q<DropdownField>("PointcloudVariantDropdown");
            _cameraConfigFilenameField = root.Q<TextField>("CameraConfigFilenameField");
            _remoteUrlField = root.Q<TextField>("RemoteUrlField");
            _remoteIsCompressedToggle = root.Q<Toggle>("RemoteIsCompressedToggle");
            _proxyLocalIPField = root.Q<TextField>("ProxyLocalIPField");
            _proxyPortField = root.Q<TextField>("ProxyPortField");
            _syntheticNPointsField = root.Q<TextField>("SyntheticNPointsField");
            _prerecordedFolderField = root.Q<TextField>("PrerecordedFolderField");
            _voxelSizeField = root.Q<TextField>("VoxelSizeField");
            _frameRateField = root.Q<TextField>("FrameRateField");

            root.Q<Button>("SaveButton").clicked += Save;
            root.Q<Button>("CancelButton").clicked += () => OnCancelClicked?.Invoke();

            PopulateFromConfig();

            _representationDropdown.RegisterValueChangedCallback(_ => FadeAndUpdateVisibility());
            _pointcloudVariantDropdown.RegisterValueChangedCallback(_ => FadeAndUpdateVisibility());
        }

        private void FadeAndUpdateVisibility()
        {
            _formScrollView.style.opacity = 0;
            _formScrollView.schedule.Execute(() =>
            {
                UpdateVisibility();
                _formScrollView.style.opacity = 1;
            }).StartingIn(TransitionFadeMs);
        }

        private static void SetVisible(VisualElement el, bool visible)
        {
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateVisibility()
        {
            var repr = (UserRepresentationType)_representationDropdown.index;
            var variant = (RepresentationPointcloudVariant)_pointcloudVariantDropdown.index;
            bool isPointCloud = repr == UserRepresentationType.PointCloud;

            SetVisible(_webcamRow, repr == UserRepresentationType.VideoAvatar);
            SetVisible(_pointcloudSection, isPointCloud);
            SetVisible(_cameraVariantSection, isPointCloud && (
                variant == RepresentationPointcloudVariant.camera ||
                variant == RepresentationPointcloudVariant.developer));
            SetVisible(_remoteVariantSection, isPointCloud && variant == RepresentationPointcloudVariant.remote);
            SetVisible(_proxyVariantSection, isPointCloud && variant == RepresentationPointcloudVariant.proxy);
            SetVisible(_syntheticVariantSection, isPointCloud && variant == RepresentationPointcloudVariant.synthetic);
            SetVisible(_prerecordedVariantSection, isPointCloud && variant == RepresentationPointcloudVariant.prerecorded);
        }

        private void PopulateFromConfig()
        {
            VRTConfig.RepresentationConfigType config = VRTConfig.Instance.RepresentationConfig;
            var pc = config.RepresentationPointcloudConfig;

            _userNameField.value = config.userName;
            _tcpURLField.value = config.userRepresentationTCPUrl;

            var reprNames = new List<string>(Enum.GetNames(typeof(UserRepresentationType)));
            _representationDropdown.choices = reprNames;
            _representationDropdown.index = (int)config.representation;

            var variantNames = new List<string>(Enum.GetNames(typeof(RepresentationPointcloudVariant)));
            _pointcloudVariantDropdown.choices = variantNames;
            _pointcloudVariantDropdown.index = (int)pc.variant;

            var webcams = new List<string> { "None" };
            foreach (WebCamDevice d in WebCamTexture.devices) webcams.Add(d.name);
            _webcamDropdown.choices = webcams;
            int webcamIdx = webcams.IndexOf(config.webcamName);
            _webcamDropdown.index = webcamIdx >= 0 ? webcamIdx : 0;

            var mics = new List<string> { "None", "Muted" };
            foreach (string d in Microphone.devices) mics.Add(d);
            _microphoneDropdown.choices = mics;
            int micIdx = mics.IndexOf(config.microphoneName);
            _microphoneDropdown.index = micIdx >= 0 ? micIdx : 0;

            _cameraConfigFilenameField.value = pc.CameraConfig?.configFilename ?? "";
            _remoteUrlField.value = pc.RemoteConfig?.url ?? "";
            _remoteIsCompressedToggle.value = pc.RemoteConfig?.isCompressed ?? false;
            _proxyLocalIPField.value = pc.ProxyConfig?.localIP ?? "";
            _proxyPortField.value = pc.ProxyConfig?.port.ToString() ?? "0";
            _syntheticNPointsField.value = pc.SyntheticConfig?.nPoints.ToString() ?? "0";
            _prerecordedFolderField.value = pc.PrerecordedConfig?.folder ?? "";
            _voxelSizeField.value = pc.voxelSize.ToString();
            _frameRateField.value = pc.frameRate.ToString();

            UpdateVisibility();
        }

        private void Save()
        {
            VRTConfig.RepresentationConfigType config = VRTConfig.Instance.RepresentationConfig;
            var pc = config.RepresentationPointcloudConfig;

            config.userName = _userNameField.value.Trim();
            config.userRepresentationTCPUrl = _tcpURLField.value.Trim();
            config.representation = (UserRepresentationType)_representationDropdown.index;
            config.webcamName = _webcamDropdown.value == "None" ? "" : _webcamDropdown.value;
            config.microphoneName = _microphoneDropdown.value == "None" ? "" : _microphoneDropdown.value;

            pc.variant = (RepresentationPointcloudVariant)_pointcloudVariantDropdown.index;

            if (pc.CameraConfig == null)
                pc.CameraConfig = new VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.CameraConfigType();
            pc.CameraConfig.configFilename = _cameraConfigFilenameField.value.Trim();

            if (pc.RemoteConfig == null)
                pc.RemoteConfig = new VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.RemoteConfigType();
            pc.RemoteConfig.url = _remoteUrlField.value.Trim();
            pc.RemoteConfig.isCompressed = _remoteIsCompressedToggle.value;

            if (pc.ProxyConfig == null)
                pc.ProxyConfig = new VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.ProxyConfigType();
            pc.ProxyConfig.localIP = _proxyLocalIPField.value.Trim();
            if (int.TryParse(_proxyPortField.value, out int port))
                pc.ProxyConfig.port = port;

            if (pc.SyntheticConfig == null)
                pc.SyntheticConfig = new VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.SyntheticConfigType();
            if (int.TryParse(_syntheticNPointsField.value, out int nPoints))
                pc.SyntheticConfig.nPoints = nPoints;

            if (pc.PrerecordedConfig == null)
                pc.PrerecordedConfig = new VRTConfig.RepresentationConfigType.RepresentationPointcloudConfigType.PrerecordedConfigType();
            pc.PrerecordedConfig.folder = _prerecordedFolderField.value.Trim();

            if (float.TryParse(_voxelSizeField.value, out float voxelSize))
                pc.voxelSize = voxelSize;
            if (float.TryParse(_frameRateField.value, out float frameRate))
                pc.frameRate = frameRate;

            VRTConfig.Instance.SaveUserConfig();
            OnSaveClicked?.Invoke();
        }
    }
}
