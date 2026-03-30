using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace VRT.Pilots.Common {
    /// <summary>
    /// Sample class that demonstrates how to bind to a UI Toolkit button click event.
    /// </summary>
    public class UIButtonHandler : MonoBehaviour
    {
        [Serializable]
        public class ButtonDefinition
        {
            public ButtonDefinition(UIButtonHandler _parent)
            {
                parent = _parent;
            }
            
            [SerializeField][Tooltip("The name of the button in the UI document.")]
            public string k_buttonName = null;

            [SerializeField] [DisableEditing][Tooltip("If this is set during runtime or after populate there is no button with this name")]
            public bool is_invalid = false;
            [SerializeField][Tooltip("Callback for this button")]
            public UnityEvent m_OnButtonClicked = new UnityEvent();
            public Button m_Button;
            public UIButtonHandler parent;
            /// <summary>
            /// Event to be invoked when the UI Toolkit button is clicked.
            /// </summary>
            public void HandleButtonClicked()
            {
                if (parent.debug) Debug.Log($"UIButtonHandler({parent.name}): button '{k_buttonName}' clicked");
                if (m_OnButtonClicked != null)
                {
                    if (m_OnButtonClicked.GetPersistentEventCount() == 0)
                    {
                        Debug.LogWarning($"UIButtonHandler({parent.name}): button '{k_buttonName}' clicked but no events were invoked");
                    }
                    m_OnButtonClicked.Invoke();
                }
                else
                {
                    Debug.LogError($"UIButtonHandler({parent.name}): unexpected HandleButtonClicked for button named {k_buttonName}");
                }
            }
            
        }

        [SerializeField][Tooltip("UI button callbacks. Hint: Use editor context menu to populate.")]
        private List<ButtonDefinition> buttons;

        [SerializeField] private bool debug = false;
        
#if UNITY_EDITOR
        [ContextMenu("Populate and check buttons from attached UI document")]
        private void PopulateButtons()
        {
            var uiToolkitDoc = GetComponent<UIDocument>();
            if (uiToolkitDoc == null)
            {
                Debug.LogError($"UIButtonHandler({name}): UI Toolkit document not found");
                return;
            }
            var root = uiToolkitDoc.rootVisualElement;
            // First check which buttons don't exist.
            foreach (var button in buttons)
            {
                if (string.IsNullOrEmpty(button.k_buttonName)) {
                    Debug.LogError($"UIButtonHandler({name}): UI button name is empty");
                    button.is_invalid = true;
                    continue;
                }

                if (root.Q<Button>(button.k_buttonName) == null)
                {
                    Debug.LogError($"UIButtonHandler({name}): UI button {button.k_buttonName} not found");
                    button.is_invalid = true;
                    continue;
                }
                Debug.Log($"UIButtonHandler: button '{button.k_buttonName}' is valid");
                button.is_invalid = false;
            }
            // Now go over all buttons and add entries if they don't have one
            foreach (Button uiButton in root.Query<Button>().Build())
            {
                var name = uiButton.name;
                if (string.IsNullOrEmpty(name)) {
                    Debug.LogWarning($"UIButtonHandler({name}): UI button name is empty, ignoring");
                    continue;
                }
                var button = buttons.Find(b => b.k_buttonName == name);
                if (button == null)
                {
                    // It does not exist yet.
                    button = new ButtonDefinition(this);
                    button.k_buttonName = name;
                    button.is_invalid = false;
                    buttons.Add(button);
                    Debug.Log($"UIButtonHandler: button '{button.k_buttonName}' has been added");
                }
            }
        }

#endif

        void Start()
        {
            InstallButtons();
        }
        
        void InstallButtons() {
            var uiToolkitDoc = GetComponent<UIDocument>();
            if (uiToolkitDoc == null)
            {
                Debug.LogError($"UIButtonHandler({name}): UI Toolkit document not found");
                return;
            }

            var root = uiToolkitDoc.rootVisualElement;
            foreach (var button in buttons)
            {
                button.parent = this;
                if (string.IsNullOrEmpty(button.k_buttonName)) {
                    Debug.LogError($"UIButtonHandler({name}): UI button name is empty");
                    continue;
                }
                button.m_Button = root.Q<Button>(button.k_buttonName);
                if (button.m_Button != null)
                {
                    if (debug) Debug.Log($"UIButtonHandler({name}): installed clicked handler for {button.k_buttonName}");
                    button.m_Button.clicked += button.HandleButtonClicked;
                    button.is_invalid = false;
                }
                else
                {
                    button.is_invalid = true;
                    Debug.LogError($"UIButtonHandler({name}): UI button '{button.k_buttonName}' not found");
                }
            }
        }

        void UninstallButtons()
        {
            foreach (var button in buttons)
            {
                if (button.m_Button != null) 
                {
                    if (debug) Debug.Log($"UIButtonHandler({name}): uninstall handler for {button.k_buttonName}");
                    button.m_Button.clicked -= button.HandleButtonClicked;
                }
                button.m_Button = null;
            }
        }
    }
}