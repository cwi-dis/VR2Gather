using System;
using UnityEngine;
using UnityEngine.UIElements;
using VRT.Core;

namespace VRT.Login
{
    /// <summary>
    /// View for the home screen. Fires events when the user picks an action.
    /// All business logic lives in OrchestratorLogin (the controller).
    /// </summary>
    public class HomeDialog
    {
        public event Action OnCreateSessionClicked;
        public event Action OnJoinSessionClicked;
        public event Action OnCreateStandaloneClicked;
        public event Action OnSettingsClicked;

        private readonly Label _welcomeLabel;

        public HomeDialog(VisualElement root)
        {
            _welcomeLabel = root.Q<Label>("WelcomeLabel");

            root.Q<Button>("CreateSessionButton").clicked += () => OnCreateSessionClicked?.Invoke();
            root.Q<Button>("JoinSessionButton").clicked += () => OnJoinSessionClicked?.Invoke();
            root.Q<Button>("CreateStandaloneButton").clicked += () => OnCreateStandaloneClicked?.Invoke();
            root.Q<Button>("SettingsButton").clicked += () => OnSettingsClicked?.Invoke();

            string userName = VRTConfig.Instance.RepresentationConfig.userName;
            if (!string.IsNullOrEmpty(userName))
            {
                _welcomeLabel.text = $"Logged in as: {userName}";
            }
        }
    }
}
