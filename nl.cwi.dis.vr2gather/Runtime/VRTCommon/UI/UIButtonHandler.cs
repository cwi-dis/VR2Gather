using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace VRT.Pilots.Common {
    /// <summary>
    /// Sample class that demonstrates how to bind to a UI Toolkit button click event.
    /// </summary>
    public class UIButtonHandler : MonoBehaviour
    {
        [SerializeField]
        UnityEvent m_OnButtonClicked = new UnityEvent();

        /// <summary>
        /// Event to be invoked when the UI Toolkit button is clicked.
        /// </summary>
        public UnityEvent onButtonClicked
        {
            get => m_OnButtonClicked;
            set => m_OnButtonClicked = value;
        }
        
        [SerializeField]
        string k_buttonName = null;
        [SerializeField]
        string k_LabelName = "DebugLabel";
        Button m_Button;
        Label m_Label;

        void Start()
        {
            var uiToolkitDoc = GetComponent<UIDocument>();
            if(uiToolkitDoc != null)
            {
                var root = uiToolkitDoc.rootVisualElement;
                m_Button = root.Q<Button>(string.IsNullOrEmpty(k_buttonName) ? null : k_buttonName);
                if (m_Button != null)
                {
                    k_buttonName = m_Button.name;
                    m_Button.clicked += HandleButtonClicked;
                }

                // Find label by name
                m_Label = root.Q<Label>(k_LabelName);
            }
        }

        private void HandleButtonClicked()
        {
            if (m_OnButtonClicked != null)
                m_OnButtonClicked.Invoke();

            if (m_Label != null)
                m_Label.text = "Button clicked at: " + Time.time;
        }
    }
}