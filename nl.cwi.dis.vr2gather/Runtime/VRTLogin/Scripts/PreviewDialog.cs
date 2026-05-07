using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRT.Login
{
    /// <summary>
    /// View for the preview screen. Fires an event when the user clicks OK.
    /// All business logic lives in OrchestratorLogin (the controller).
    /// </summary>
    public class PreviewDialog
    {
        public event Action OnOkClicked;

        private readonly VisualElement _previewImage;
        private readonly Label _noMicLabel;
        private readonly VisualElement _voiceLevelRow;
        private readonly VisualElement _voiceLevelBar;

        public PreviewDialog(VisualElement root)
        {
            _previewImage = root.Q<VisualElement>("PreviewImage");
            _noMicLabel = root.Q<Label>("NoMicLabel");
            _voiceLevelRow = root.Q<VisualElement>("VoiceLevelRow");
            _voiceLevelBar = root.Q<VisualElement>("VoiceLevelBar");
            root.Q<Button>("OkButton").clicked += () => OnOkClicked?.Invoke();
            SetMicrophoneActive(false);
        }

        public void SetPreviewTexture(RenderTexture rt)
        {
            _previewImage.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
        }

        public void MarkPreviewDirty()
        {
            _previewImage.MarkDirtyRepaint();
        }

        public void SetMicrophoneActive(bool active)
        {
            _noMicLabel.style.display = active ? DisplayStyle.None : DisplayStyle.Flex;
            _voiceLevelRow.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetVoiceLevel(float level)
        {
            _voiceLevelBar.style.width = Length.Percent(Mathf.Clamp01(level) * 100f);
        }
    }
}
