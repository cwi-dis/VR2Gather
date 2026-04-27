using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Suppresses Input Action Maps when UI Toolkit captures keyboard or scroll-wheel input,
    /// preventing locomotion controls from firing during text entry or UI scrolling.
    ///
    /// Attach to the same GameObject as the UIDocument. Configure the two blocking
    /// behaviours independently in the Inspector.
    /// </summary>
    public class UIInputBlocker : MonoBehaviour
    {
        [Tooltip("InputActionAsset to suppress. If left empty, VR2GatherInputActions is loaded from Resources.")]
        [SerializeField] InputActionAsset m_ActionAsset;

        [Header("Text field focus")]
        [Tooltip("Disable the listed action maps while any TextField in this panel has keyboard focus.")]
        [SerializeField] bool m_BlockOnTextFieldFocus = true;
        [SerializeField] string[] m_TextFieldActionMaps = { "Handsfree Locomotion", "ViewPosition" };

        [Header("Scroll wheel over UI")]
        [Tooltip("Disable the listed action maps each Input System tick while the mouse pointer is over this panel.")]
        [SerializeField] bool m_BlockOnScrollWheelOverUI = true;
        [SerializeField] string[] m_ScrollActionMaps = { "ViewPosition" };

        private UIDocument m_UIDocument;
        private readonly List<InputActionMap> m_TextFieldMaps = new();
        private readonly List<InputActionMap> m_ScrollMaps = new();
        private bool m_TextFieldFocused;
        private bool m_FocusCallbacksRegistered;

        private void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();

            if (m_ActionAsset == null)
                m_ActionAsset = Resources.Load<InputActionAsset>("VR2GatherInputActions");

            if (m_ActionAsset == null)
            {
                Debug.LogWarning($"{nameof(UIInputBlocker)}: no InputActionAsset found — blocking disabled. Assign one in the Inspector or ensure VR2GatherInputActions is in a Resources folder.");
                return;
            }

            CollectMaps(m_TextFieldActionMaps, m_TextFieldMaps);
            CollectMaps(m_ScrollActionMaps, m_ScrollMaps);
        }

        private void Start()
        {
            // rootVisualElement is not guaranteed available during Awake/OnEnable on first init.
            TryRegisterFocusCallbacks();
        }

        private void OnEnable()
        {
            if (m_BlockOnScrollWheelOverUI && m_ScrollMaps.Count > 0)
                InputSystem.onBeforeUpdate += OnBeforeInputUpdate;

            // Re-register after a disable/enable cycle (rootVisualElement already available).
            TryRegisterFocusCallbacks();
        }

        private void OnDisable()
        {
            InputSystem.onBeforeUpdate -= OnBeforeInputUpdate;
            UnregisterFocusCallbacks();

            if (m_TextFieldFocused)
            {
                m_TextFieldFocused = false;
                SetMapsEnabled(m_TextFieldMaps, true);
            }
            // Restore scroll maps — they may have been left disabled by OnBeforeInputUpdate.
            SetMapsEnabled(m_ScrollMaps, true);
        }

        // ── Text field focus ────────────────────────────────────────────────────

        private void TryRegisterFocusCallbacks()
        {
            if (!m_BlockOnTextFieldFocus || m_FocusCallbacksRegistered) return;
            var root = m_UIDocument?.rootVisualElement;
            if (root == null) return;
            root.RegisterCallback<FocusInEvent>(OnFocusIn);
            root.RegisterCallback<FocusOutEvent>(OnFocusOut);
            m_FocusCallbacksRegistered = true;
        }

        private void UnregisterFocusCallbacks()
        {
            if (!m_FocusCallbacksRegistered) return;
            var root = m_UIDocument?.rootVisualElement;
            if (root != null)
            {
                root.UnregisterCallback<FocusInEvent>(OnFocusIn);
                root.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            }
            m_FocusCallbacksRegistered = false;
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            if (m_TextFieldFocused || !IsInsideTextField(evt.target as VisualElement)) return;
            m_TextFieldFocused = true;
            SetMapsEnabled(m_TextFieldMaps, false);
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            if (!m_TextFieldFocused || !IsInsideTextField(evt.target as VisualElement)) return;
            // Stay blocked if focus is moving directly to another text field (e.g. Tab key).
            if (IsInsideTextField(evt.relatedTarget as VisualElement)) return;
            m_TextFieldFocused = false;
            SetMapsEnabled(m_TextFieldMaps, true);
        }

        // ── Scroll wheel over UI ────────────────────────────────────────────────

        private void OnBeforeInputUpdate()
        {
            bool overUI = IsMouseOverPanel();
            foreach (var map in m_ScrollMaps)
            {
                if (overUI)
                {
                    map.Disable();
                }
                else
                {
                    // Don't re-enable a map that is already held disabled by text-field focus.
                    bool heldByTextFocus = m_TextFieldFocused && m_TextFieldMaps.Contains(map);
                    if (!heldByTextFocus)
                        map.Enable();
                }
            }
        }

        private bool IsMouseOverPanel()
        {
            var panel = m_UIDocument?.rootVisualElement?.panel;
            if (panel == null) return false;
            if (Mouse.current == null) return false;
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
            return panel.Pick(panelPos) != null;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        // In UI Toolkit, clicking into a TextField focuses its inner TextInput child, not
        // the TextField itself. Walk up the ancestor chain to detect this case.
        private static bool IsInsideTextField(VisualElement element)
        {
            var current = element;
            while (current != null)
            {
                if (current is TextField) return true;
                current = current.parent;
            }
            return false;
        }

        private static void SetMapsEnabled(List<InputActionMap> maps, bool enabled)
        {
            foreach (var map in maps)
            {
                if (enabled) map.Enable();
                else map.Disable();
            }
        }

        private void CollectMaps(string[] names, List<InputActionMap> target)
        {
            foreach (string name in names)
            {
                var map = m_ActionAsset.FindActionMap(name);
                if (map != null)
                    target.Add(map);
                else
                    Debug.LogWarning($"{nameof(UIInputBlocker)}: action map '{name}' not found in {m_ActionAsset.name}.");
            }
        }
    }
}
