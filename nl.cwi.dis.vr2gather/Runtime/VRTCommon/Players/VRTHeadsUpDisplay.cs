using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.UIElements;
using VRT.Core;

namespace VRT.Pilots.Common
{
    public class VRTHeadsUpDisplay : MonoBehaviour, ErrorManagerSink
    {
        [Tooltip("The Input System Action that will show/hide the HUD")]
        [SerializeField] InputActionProperty m_ShowHideAction;
        [Tooltip("HUD distance (metres) when not in VR — tune to control how much of the screen it fills")]
        [SerializeField] float nonVRDistance = 2f;
        [Tooltip("Should this HUD intercept and display error messages?")]
        [SerializeField] bool interceptErrors = true;
        [Tooltip("Auto-show messages")]
        [SerializeField] bool autoShowMessages = true;
        [Tooltip("Filter out duplicate messages")] [SerializeField]
        private bool filterDuplicates = true;
        [Tooltip("Prefab for error messages")] 
        [SerializeField] GameObject errorPrefab;

        [Tooltip("Player controller (found dynamically)")]
        [DisableEditing] [SerializeField] PlayerControllerSelf playerController;

        private List<string> currentMessageList  = new List<string>();
        private string currentMessageString = null;
        private bool _hudVisible = false;

        void OnAutoShowMessagesChanged(ChangeEvent<bool> evt)
        {
            autoShowMessages = evt.newValue;
        }

        void OnFilterDuplicatesChanged(ChangeEvent<bool> evt)
        {
            filterDuplicates = evt.newValue;
        }

        void SetHudVisible(bool visible)
        {
            var root = GetRoot();
            if (root != null)
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _hudVisible = visible;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (playerController == null) playerController = GetComponentInParent<PlayerControllerSelf>();
            var lazyFollow = gameObject.AddComponent<LazyFollow>();
            lazyFollow.rotationFollowMode = LazyFollow.RotationFollowMode.LookAtWithWorldUp;
            float hudDistance = XRSettings.isDeviceActive ? 1f : nonVRDistance;
            lazyFollow.targetOffset = new Vector3(0f, 0f, hudDistance);
            lazyFollow.snapOnEnable = true;
            lazyFollow.maxAngleAllowed = 20f;
            gameObject.AddComponent<BoxCollider>();
            if (interceptErrors)
            {
                Hide();

                if (ErrorManager.Instance == null)
                {
                    Debug.LogError("HeadsUpDisplay: interceptErrors is true, but there is no ErrorManager");
                    return;
                }
                ErrorManager.Instance.RegisterSink(this);
            }
            var toggle = GetRoot()?.Q<Toggle>("AutoShowMessagesToggle");
            if (toggle != null)
            {
                toggle.value = autoShowMessages;
                toggle.RegisterValueChangedCallback(OnAutoShowMessagesChanged);
            }
            var filterToggle = GetRoot()?.Q<Toggle>("FilterDuplicatesToggle");
            if (filterToggle != null)
            {
                filterToggle.value = filterDuplicates;
                filterToggle.RegisterValueChangedCallback(OnFilterDuplicatesChanged);
            }
            var clearButton = GetRoot()?.Q<Button>("ClearMessagesButton");
            if (clearButton != null)
                clearButton.clicked += ClearMessages;
        }

        VisualElement GetRoot()
        {
            var uiDoc = GetComponent<UIDocument>();
            return uiDoc?.rootVisualElement;
        }

        // Update is called once per frame
        void Update()
        {
            if (m_ShowHideAction.action.WasPressedThisFrame())
            {
                SetHudVisible(!_hudVisible);
            }

            if (currentMessageString != null && _hudVisible)
            {
                GetRoot().Q<Label>("MessagesContent").text = currentMessageString;
                currentMessageString = null;
            }
        }

        void SetActiveTab(string activePanelName)
        {
            var root = GetRoot();
            foreach (var name in new[] { "CommandsPanel", "MessagesPanel" })
            {
                root.Q<VisualElement>(name)?.EnableInClassList("vrt-tab-panel--active", name == activePanelName);
            }
            foreach (var (name, buttonName) in new[] { ("CommandsPanel", "CommandsTabButton"), ("MessagesPanel", "MessagesTabButton") })
            {
                root.Q<Button>(buttonName)?.EnableInClassList("vrt-tab-button--active", name == activePanelName);
            }
        }

        public void ShowCommandsTab()
        {
            SetActiveTab("CommandsPanel");
        }

        public void ShowMessagesTab()
        {
            SetActiveTab("MessagesPanel");
        }

        public void ClearMessages()
        {
            currentMessageList.Clear();
            currentMessageString = "";
        }

        public void FillError(string title, string message)
        {
            // First we filter out some messages that are not interesting to the end user.
            if (message.Contains("Hand Tracking Subsystem not found or not running",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            string newMessage = $"{title}: {message}";
            if (filterDuplicates)
            {
                currentMessageList.Remove(newMessage);
            }
            currentMessageList.Add(newMessage);
            currentMessageString = String.Join("\n", currentMessageList);
            if (autoShowMessages)
            {
                SetHudVisible(true);
                SetActiveTab("MessagesPanel");
            }
            
#if OLD_CODE
            var popupGO = Instantiate(errorPrefab, MessagesGO.transform);
            popupGO.SetActive(true);
            ErrorPopup errorPopup = popupGO.GetComponent<ErrorPopup>();
            errorPopup.FillError(title, message);
            ShowMessages();
#endif
        }

        public void Hide()
        {
            SetHudVisible(false);
#if OLD_CODE
            PilotController.Instance.EnableDirectInteraction();
            UserInterfaceGO.SetActive(true);
            MessagesGO.SetActive(false);
            HelpGO.SetActive(false);
            UserInterfaceToggle.isOn = true;
            MessagesToggle.isOn = false;
            HelpToggle.isOn = false;
#endif
        }

        public void ToggleChanged()
        {
#if OLD_CODE
            if (UserInterfaceToggle.isOn)
            {
                ShowCommands();
            }
            else if (MessagesToggle.isOn)
            {
                ShowMessages();
            }
            else
            {
                ShowHelp();
            }
#endif
        }


        public void OnHUDCommand(string command)
        {
            Debug.Log($"HeadsUpDisplay: OnHUDCommand({command})");
            bool ok = false;
            if (playerController != null)
            {
                ok = playerController.OnUserCommand(command);
            }
            if (!ok)
            {
                ok = PilotController.Instance.OnUserCommand(command);
            }
            if (!ok)
            {
                Debug.LogError($"HeadsUpDisplay: unknown command \"{command}\"");
            }
        }
    }
}
