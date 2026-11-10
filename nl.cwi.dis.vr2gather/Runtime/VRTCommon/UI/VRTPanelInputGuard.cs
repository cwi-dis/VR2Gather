using System.Collections.Generic;
using System.Linq;
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
    ///
    /// Restoring a map re-enables only the actions that were enabled when this guard suppressed it.
    /// Other components switch individual actions on and off too: XRI's ControllerInputActionManager
    /// keeps only one of Move and Teleport enabled per stick, so enabling the whole map would make
    /// a stick move and teleport at the same time (#409).
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
        // Maps this guard has suppressed, with the actions that were enabled at that moment.
        private readonly Dictionary<InputActionMap, List<InputAction>> m_Suppressed = new();
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

            m_TextFieldFocused = false;
            m_PointerOver = false;
            ApplyMapsState();
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
            ApplyMapsState();
        }

        private void OnPanelPointerLeave(PointerLeaveEvent evt)
        {
            if (evt.target != m_Root) return;
            m_PointerOver = false;
            ApplyMapsState();
        }

        // Suppress every map that should be suppressed now (pointer over the panel, or a text field
        // focused), and restore the ones this guard suppressed that no longer should be.
        private void ApplyMapsState()
        {
            foreach (var map in m_HoverMaps.Concat(m_TextFieldMaps).Distinct())
            {
                bool suppress = (m_PointerOver && m_HoverMaps.Contains(map))
                             || (m_TextFieldFocused && m_TextFieldMaps.Contains(map));
                if (suppress)
                    Suppress(map);
                else
                    Restore(map);
            }
        }

        private void Suppress(InputActionMap map)
        {
            if (m_Suppressed.ContainsKey(map)) return;
            m_Suppressed[map] = map.actions.Where(a => a.enabled).ToList();
            map.Disable();
        }

        private void Restore(InputActionMap map)
        {
            if (!m_Suppressed.TryGetValue(map, out var wasEnabled)) return;
            m_Suppressed.Remove(map);
            foreach (var action in wasEnabled)
                action.Enable();
        }

        // ── Text field focus ─────────────────────────────────────────────────────

        private void OnFocusIn(FocusInEvent evt)
        {
            if (m_TextFieldFocused || !IsInsideInteractiveField(evt.target as VisualElement)) return;
            m_TextFieldFocused = true;
            ApplyMapsState();
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            if (!m_TextFieldFocused || !IsInsideInteractiveField(evt.target as VisualElement)) return;
            // Keep suppressed if focus moved into the dropdown popup overlay.
            if (IsInsideDropdownPopup(evt.relatedTarget as VisualElement)) return;
            if (IsInsideInteractiveField(evt.relatedTarget as VisualElement)) return;
            m_TextFieldFocused = false;
            ApplyMapsState();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        // Returns true for elements that require keyboard input (TextField, DropdownField).
        private static bool IsInsideInteractiveField(VisualElement element)
        {
            var current = element;
            while (current != null)
            {
                if (current is TextField) return true;
                if (current is DropdownField) return true;
                current = current.parent;
            }
            return false;
        }

        // Returns true when the element is inside a DropdownField popup overlay.
        private static bool IsInsideDropdownPopup(VisualElement element)
        {
            var current = element;
            while (current != null)
            {
                if (current.ClassListContains("unity-base-dropdown")) return true;
                current = current.parent;
            }
            return false;
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
