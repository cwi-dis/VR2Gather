using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.UIElements;
using VRT.Core;

namespace VRT.Pilots.Common
{
    public class VRTHeadsUpDisplay : MonoBehaviour, ErrorManagerSink
    {
        [Tooltip("The Input System Action that will show/hide the HUD")]
        [SerializeField] InputActionProperty m_ShowHideAction;
        [Tooltip("How far away is the HMD from the users eyes?")]
        public float distance = 1;
        [Tooltip("How far up/down from the center is the HMD?")]
        public float height = 0;
        [Tooltip("How fast should it move when the user changes position/orientation?")]
        public float velocity = 0.01f;
        [Tooltip("How far away from its preferred position can it be before we start moving it?")]
        public float maxOffset = 1;
        [Tooltip("How far away from its preferred orientation can it be before we start moving it?")]
        public float maxAngle = 1;
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
        [Tooltip("Dialog needs to move")]
        [DisableEditing][SerializeField] bool shouldChange;

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

        void LateUpdate()
        {
            if (Camera.main == null)
            {
                // Self player has not initialized yet.
                return;
            }
            Vector3 forward = Camera.main.transform.forward;
            forward.y = height;
            forward = forward.normalized;

            Vector3 position = Camera.main.transform.position + forward * distance;
            Quaternion rotation = Quaternion.LookRotation(forward);
            if (Vector3.Distance(transform.position, position) > maxOffset ||
                Quaternion.Angle(transform.rotation, rotation) > maxAngle)
            {
                shouldChange = true;
            }
            if (shouldChange)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, velocity);
                transform.position = Vector3.Lerp(transform.position, position, velocity);
                //transform.localScale = Vector3.Lerp(transform.localScale, scale, scaleVel);
                if (Vector3.Distance(transform.position, position) < maxOffset / 10 &&
                   Quaternion.Angle(transform.rotation, rotation) < maxAngle / 10)
                {
                    shouldChange = false;
                }
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
