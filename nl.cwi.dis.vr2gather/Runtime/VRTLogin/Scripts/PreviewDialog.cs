using System;
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

        public PreviewDialog(VisualElement root)
        {
            root.Q<Button>("OkButton").clicked += () => OnOkClicked?.Invoke();
        }
    }
}
