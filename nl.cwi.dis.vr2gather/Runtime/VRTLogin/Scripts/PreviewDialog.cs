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

        public PreviewDialog(VisualElement root)
        {
            _previewImage = root.Q<VisualElement>("PreviewImage");
            root.Q<Button>("OkButton").clicked += () => OnOkClicked?.Invoke();
        }

        public void SetPreviewTexture(RenderTexture rt)
        {
            _previewImage.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
        }

        public void MarkPreviewDirty()
        {
            _previewImage.MarkDirtyRepaint();
        }
    }
}
