using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Suppresses Input Action Maps when this panel is in focus — because a pointer
    /// (mouse or XR ray) is over it, or a TextField has keyboard focus.
    ///
    /// Attach to the same GameObject as the UIDocument (the interactable side).
    /// </summary>
    public class VRTPanelInputGuard : MonoBehaviour
    {
        [Tooltip("InputActionAssets to suppress. If empty, VR2GatherInputActions is loaded from Resources.")]
        [SerializeField] InputActionAsset[] m_ActionAssets;

        [Header("Text field focus")]
        [Tooltip("Disable the listed action maps while any TextField in this panel has keyboard focus.")]
        [SerializeField] bool m_BlockOnTextFieldFocus = true;
        [SerializeField] string[] m_TextFieldActionMaps = { "Desktop Locomotion", "ViewPosition" };

        [Header("Panel hover (any pointer: mouse or XR ray)")]
        [Tooltip("Disable the listed action maps while any pointer is over this panel.")]
        [SerializeField] bool m_BlockOnPanelHover = true;
        [SerializeField] string[] m_HoverActionMaps = { "Desktop Locomotion", "ViewPosition" };

        private UIDocument m_UIDocument;
        private VisualElement m_Root;
        private readonly List<InputActionMap> m_TextFieldMaps = new();
        private readonly List<InputActionMap> m_HoverMaps = new();
        private bool m_TextFieldFocused;
        private bool m_PointerOver;
        private bool m_FocusCallbacksRegistered;
        private bool m_HoverCallbacksRegistered;

        private void Awake()
        {
            m_UIDocument = GetComponent<UIDocument>();

            if (m_ActionAssets == null || m_ActionAssets.Length == 0)
            {
                var fallback = Resources.Load<InputActionAsset>("VR2GatherInputActions");
                if (fallback != null)
                    m_ActionAssets = new[] { fallback };
            }

            if (m_ActionAssets == null || m_ActionAssets.Length == 0)
            {
                Debug.LogWarning($"{nameof(VRTPanelInputGuard)}: no InputActionAsset found — blocking disabled. Assign one in the Inspector or ensure VR2GatherInputActions is in a Resources folder.");
                return;
            }

            CollectMaps(m_TextFieldActionMaps, m_TextFieldMaps);
            CollectMaps(m_HoverActionMaps, m_HoverMaps);
        }

        private void Start()
        {
            // rootVisualElement is not guaranteed available during Awake/OnEnable on first init.
            TryRegisterCallbacks();
        }

        private void OnEnable()
        {
            // Re-register after a disable/enable cycle (rootVisualElement already available).
            TryRegisterCallbacks();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();

            if (m_TextFieldFocused)
            {
                m_TextFieldFocused = false;
                SetMapsEnabled(m_TextFieldMaps, true);
            }
            m_PointerOver = false;
            SetMapsEnabled(m_HoverMaps, true);
        }

        // ── Callback registration ────────────────────────────────────────────────

        private void TryRegisterCallbacks()
        {
            var root = m_UIDocument?.rootVisualElement;
            if (root == null) return;
            if (root == m_Root && m_FocusCallbacksRegistered && m_HoverCallbacksRegistered) return;

            UnregisterCallbacks();
            m_Root = root;

            if (m_BlockOnTextFieldFocus && !m_FocusCallbacksRegistered)
            {
                m_Root.RegisterCallback<FocusInEvent>(OnFocusIn);
                m_Root.RegisterCallback<FocusOutEvent>(OnFocusOut);
                m_FocusCallbacksRegistered = true;
            }

            if (m_BlockOnPanelHover && m_HoverMaps.Count > 0 && !m_HoverCallbacksRegistered)
            {
                // TrickleDown fires on the root as events pass through; filtering by
                // evt.target == root catches pointer crossing the panel boundary only.
                m_Root.RegisterCallback<PointerEnterEvent>(OnPanelPointerEnter, TrickleDown.TrickleDown);
                m_Root.RegisterCallback<PointerLeaveEvent>(OnPanelPointerLeave, TrickleDown.TrickleDown);
                m_HoverCallbacksRegistered = true;
            }
        }

        private void UnregisterCallbacks()
        {
            if (m_Root == null) return;

            if (m_FocusCallbacksRegistered)
            {
                m_Root.UnregisterCallback<FocusInEvent>(OnFocusIn);
                m_Root.UnregisterCallback<FocusOutEvent>(OnFocusOut);
                m_FocusCallbacksRegistered = false;
            }

            if (m_HoverCallbacksRegistered)
            {
                m_Root.UnregisterCallback<PointerEnterEvent>(OnPanelPointerEnter, TrickleDown.TrickleDown);
                m_Root.UnregisterCallback<PointerLeaveEvent>(OnPanelPointerLeave, TrickleDown.TrickleDown);
                m_HoverCallbacksRegistered = false;
            }

            m_Root = null;
        }

        // ── Pointer hover ────────────────────────────────────────────────────────

        private void OnPanelPointerEnter(PointerEnterEvent evt)
        {
            if (evt.target != m_Root) return;
            m_PointerOver = true;
            ApplyHoverMapsState();
        }

        private void OnPanelPointerLeave(PointerLeaveEvent evt)
        {
            if (evt.target != m_Root) return;
            m_PointerOver = false;
            ApplyHoverMapsState();
        }

        private void ApplyHoverMapsState()
        {
            foreach (var map in m_HoverMaps)
            {
                if (m_PointerOver)
                {
                    map.Disable();
                }
                else
                {
                    bool heldByTextFocus = m_TextFieldFocused && m_TextFieldMaps.Contains(map);
                    if (!heldByTextFocus)
                        map.Enable();
                }
            }
        }

        // ── Text field focus ─────────────────────────────────────────────────────

        private void OnFocusIn(FocusInEvent evt)
        {
            if (m_TextFieldFocused || !IsInsideTextField(evt.target as VisualElement)) return;
            m_TextFieldFocused = true;
            SetMapsEnabled(m_TextFieldMaps, false);
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            if (!m_TextFieldFocused || !IsInsideTextField(evt.target as VisualElement)) return;
            if (IsInsideTextField(evt.relatedTarget as VisualElement)) return;
            m_TextFieldFocused = false;
            SetMapsEnabled(m_TextFieldMaps, true);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

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
                bool found = false;
                foreach (var asset in m_ActionAssets)
                {
                    if (asset == null) continue;
                    var map = asset.FindActionMap(name);
                    if (map != null)
                    {
                        target.Add(map);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    Debug.LogWarning($"{nameof(VRTPanelInputGuard)}: action map '{name}' not found in any assigned asset.", this);
            }
        }
    }
}
