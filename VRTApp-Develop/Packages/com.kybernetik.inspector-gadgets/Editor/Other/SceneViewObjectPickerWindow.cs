// Inspector Gadgets // https://kybernetik.com.au/inspector-gadgets // Copyright 2017-2025 Kybernetik //
// Based on an implementation by yasirkula // https://gist.github.com/yasirkula/06edc780beaa4d8705b3564d60886fa6 //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
#if UNITY_UI
using UnityEngine.UI;
#endif
#if UNITY_2021_2_OR_NEWER
using PrefabStageUtility = UnityEditor.SceneManagement.PrefabStageUtility;
#elif UNITY_2018_3_OR_NEWER
using PrefabStageUtility = UnityEditor.Experimental.SceneManagement.PrefabStageUtility;
#endif

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only] A context menu for picking UI objects in the Scene View.</summary>
    internal class SceneViewObjectPickerWindow : EditorWindow
    {
        /************************************************************************************************************************/

        private readonly struct ObjectInfo
        {
            public readonly RectTransform RectTransform;
            public readonly List<ObjectInfo> Children;

            public ObjectInfo(RectTransform rectTransform)
            {
                RectTransform = rectTransform;
                Children = new(2);
            }
        }

        /************************************************************************************************************************/

        private const float Padding = 1;

        private static readonly Color HighlightColor = new(1f, 1f, 0f, 0.2f);

        private static readonly Vector3[] HoveredUIObjectCorners = new Vector3[4];
        private static readonly List<ICanvasRaycastFilter> RaycastFilters = new(4);

        private static Func<Rect, bool, bool, Rect> _FitRectToScreen;
        private static GUIStyle _RowStyle;

        private static RectTransform _HoveredUIObject;
        private static bool _IsActivationClick;
        private static bool _BlockSceneViewInput;

        private static float LineHeight
            => EditorGUIUtility.singleLineHeight;

        /************************************************************************************************************************/

        private static bool Initialize()
        {
            if (_RowStyle == null)
                _RowStyle = "MenuItem";

            return FindFitRectToScreen();
        }

        /************************************************************************************************************************/

        private static bool FindFitRectToScreen()
        {
            if (_FitRectToScreen != null)
                return true;

            const string TypeName = "UnityEditor.ContainerWindow";
            var type = typeof(EditorWindow).Assembly.GetType(TypeName);
            if (type == null)
            {
                Debug.LogError($"Unable to find {TypeName}.");
                return false;
            }

            const string MethodName = "FitRectToScreen";
            var method = type.GetMethod("FitRectToScreen", IGEditorUtils.StaticBindings);
            if (type == null)
            {
                Debug.LogError($"Unable to find {TypeName}.{MethodName}.");
                return false;
            }

            _FitRectToScreen = (Func<Rect, bool, bool, Rect>)Delegate.CreateDelegate(
                typeof(Func<Rect, bool, bool, Rect>),
                method);
            return true;
        }

        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        private static void OnSceneViewGUI()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += sceneView =>
#else
            SceneView.onSceneGUIDelegate += sceneView =>
#endif
            {
                var currentEvent = Event.current;
                switch (currentEvent.type)
                {
                    case EventType.MouseDown:
                        _IsActivationClick = false;

                        if (ActivationModifiers.AreKeysDown(currentEvent) &&
                            currentEvent.button == ActivationMouseButton.Value)
                        {
                            _IsActivationClick = true;
                        }
                        else if (_BlockSceneViewInput)
                        {
                            // User has clicked outside the context window to close it.
                            // Ignore this click in Scene view if it's left click
                            _BlockSceneViewInput = false;

                            if (currentEvent.button == 0)
                            {
                                GUIUtility.hotControl = 0;
                                currentEvent.Use();
                            }
                        }
                        break;

                    case EventType.MouseDrag:
                        _IsActivationClick = false;
                        break;

                    case EventType.MouseUp:
                        if (_IsActivationClick &&
                            currentEvent.button == ActivationMouseButton.Value)
                        {
                            TryGatherTargetsAndOpenWindow(sceneView.camera);
                        }
                        break;
                }

                if (_HoveredUIObject != null)
                {
                    _HoveredUIObject.GetWorldCorners(HoveredUIObjectCorners);
                    Handles.DrawSolidRectangleWithOutline(
                        HoveredUIObjectCorners,
                        HighlightColor,
                        Color.black);
                }
            };
        }

        /************************************************************************************************************************/

        private static void TryGatherTargetsAndOpenWindow(Camera camera)
        {
            if (!Initialize())
                return;

            // Find all UI objects under the cursor
            var pointerPosition = HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition);
            var root = new ObjectInfo(null);
#if UNITY_2018_3_OR_NEWER
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null &&
                prefabStage.stageHandle.IsValid() &&
                prefabStage.prefabContentsRoot.transform is RectTransform prefabStageRoot)
            {
                GatherObjectsRecursive(prefabStageRoot, pointerPosition, camera, false, root.Children);
            }
            else
#endif
            {
#if UNITY_2022_3_OR_NEWER
                var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
#else
                var canvases = FindObjectsOfType<Canvas>();
#endif
                Array.Sort(canvases, (c1, c2) => c1.sortingOrder.CompareTo(c2.sortingOrder));
                foreach (var canvas in canvases)
                {
                    if (canvas != null && canvas.gameObject.activeInHierarchy && canvas.isRootCanvas)
                        GatherObjectsRecursive(
                            (RectTransform)canvas.transform,
                            pointerPosition,
                            camera,
                            false,
                            root.Children);
                }
            }

            // Remove non-Graphic root entries with no children from the results.
            root.Children.RemoveAll(canvasEntry
                => canvasEntry.Children.Count == 0
#if UNITY_UI
                && !canvasEntry.RectTransform.TryGetComponent<Graphic>(out _)
#endif
                );

            // If any results found, show the window.
            if (root.Children.Count > 0)
                CreateInstance<SceneViewObjectPickerWindow>().ShowContextWindow(root.Children);
        }

        /************************************************************************************************************************/

        private readonly List<RectTransform> ObjectTransforms = new(16);
        private readonly List<string> ObjectLabels = new(16);

        /************************************************************************************************************************/

        private void ShowContextWindow(List<ObjectInfo> results)
        {
            var text = new StringBuilder(100);
            InitializeObjectsRecursive(results, 0, text);

            var rowGUIStyle = _RowStyle;
            var preferredWidth = 0f;

            foreach (var label in ObjectLabels)
                preferredWidth = Mathf.Max(
                    preferredWidth,
                    rowGUIStyle.CalcSize(IGEditorUtils.TempContent(label)).x);

            ShowAsDropDown(default, new(
                preferredWidth + Padding * 2f,
                ObjectTransforms.Count * LineHeight + Padding * 2f));

            // Show dropdown above the cursor instead of below the cursor
            var rect = new Rect(
                GUIUtility.GUIToScreenPoint(Event.current.mousePosition) - new Vector2(0f, position.height),
                position.size);
            position = _FitRectToScreen(rect, true, true);
        }

        /************************************************************************************************************************/

        private void InitializeObjectsRecursive(List<ObjectInfo> objects, int depth, StringBuilder text)
        {
            foreach (ObjectInfo info in objects)
            {
                text.Length = 0;

                ObjectTransforms.Add(info.RectTransform);
                ObjectLabels.Add(text.Append(' ', depth * 4).Append(info.RectTransform.name).ToString());

                if (info.Children.Count > 0)
                    InitializeObjectsRecursive(info.Children, depth + 1, text);
            }
        }

        /************************************************************************************************************************/

        protected virtual void OnEnable()
        {
            wantsMouseMove = wantsMouseEnterLeaveWindow = true;
#if UNITY_2020_1_OR_NEWER
            wantsLessLayoutEvents = false;
#endif
            _BlockSceneViewInput = true;
        }

        /************************************************************************************************************************/

        protected virtual void OnDisable()
        {
            _HoveredUIObject = null;
            SceneView.RepaintAll();
        }

        /************************************************************************************************************************/

        protected virtual void OnGUI()
        {
            var currentEvent = Event.current;

            var rowWidth = position.width - Padding * 2f;
            var rowHeight = LineHeight;
            var rowGUIStyle = _RowStyle;
            var hoveredRowIndex = -1;
            for (int i = 0; i < ObjectTransforms.Count; i++)
            {
                var rect = new Rect(
                    Padding,
                    Padding + i * rowHeight,
                    rowWidth,
                    rowHeight);

                if (GUI.Button(rect, ObjectLabels[i], rowGUIStyle))
                {
                    if (ObjectTransforms[i] != null)
                        Selection.activeTransform = ObjectTransforms[i];

                    _BlockSceneViewInput = false;
                    currentEvent.Use();
                    Close();
                    GUIUtility.ExitGUI();
                }

                if (hoveredRowIndex < 0 &&
                    currentEvent.type == EventType.MouseMove &&
                    rect.Contains(currentEvent.mousePosition))
                    hoveredRowIndex = i;
            }

            switch (currentEvent.type)
            {
                case EventType.MouseMove:
                case EventType.MouseLeaveWindow:
                    var hoveredUIObject = hoveredRowIndex >= 0
                        ? ObjectTransforms[hoveredRowIndex]
                        : null;

                    if (_HoveredUIObject != hoveredUIObject)
                    {
                        _HoveredUIObject = hoveredUIObject;
                        Repaint();
                        SceneView.RepaintAll();
                    }
                    break;
            }
        }

        /************************************************************************************************************************/

        private static void GatherObjectsRecursive(
            RectTransform rectTransform,
            Vector2 pointerPos,
            Camera camera,
            bool culledByCanvasGroup,
            List<ObjectInfo> objects)
        {
            if (rectTransform.TryGetComponent<Canvas>(out var canvas) &&
                !canvas.enabled)
                return;

            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPos, camera) &&
                ShouldGatherObject(rectTransform, pointerPos, camera, ref culledByCanvasGroup))
            {
                var info = new ObjectInfo(rectTransform);
                objects.Add(info);
                objects = info.Children;
            }

            for (int i = 0, childCount = rectTransform.childCount; i < childCount; i++)
            {
                var childRectTransform = rectTransform.GetChild(i) as RectTransform;
                if (childRectTransform != null && childRectTransform.gameObject.activeSelf)
                    GatherObjectsRecursive(childRectTransform, pointerPos, camera, culledByCanvasGroup, objects);
            }
        }

        /************************************************************************************************************************/

        private static bool ShouldGatherObject(
            RectTransform rectTransform,
            Vector2 pointerPosition,
            Camera camera,
            ref bool culledByCanvasGroup)
        {
#if UNITY_2019_3_OR_NEWER
            if (SceneVisibilityManager.instance.IsHidden(rectTransform.gameObject, false))
                return false;

            if (SceneVisibilityManager.instance.IsPickingDisabled(rectTransform.gameObject, false))
                return false;
#endif

            if (rectTransform.TryGetComponent<CanvasRenderer>(out var canvasRenderer) && canvasRenderer.cull)
                return false;

            if (rectTransform.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                if (canvasGroup.ignoreParentGroups)
                    culledByCanvasGroup = canvasGroup.alpha == 0f;
                else if (canvasGroup.alpha == 0f)
                    culledByCanvasGroup = true;
            }

            if (!culledByCanvasGroup)
            {
#if UNITY_UI
                // If the target is a MaskableGraphic that ignores masks (i.e. visible outside masks)
                // and isn't fully transparent, accept it.
                if (rectTransform.TryGetComponent<MaskableGraphic>(out var maskableGraphic) &&
                    !maskableGraphic.maskable &&
                    maskableGraphic.color.a > 0f)
                    return true;
#endif

                RaycastFilters.Clear();
                rectTransform.GetComponentsInParent(false, RaycastFilters);
                foreach (var raycastFilter in RaycastFilters)
                {
                    if (!raycastFilter.IsRaycastLocationValid(pointerPosition, camera))
                        return false;
                }
            }

            return !culledByCanvasGroup;
        }

        /************************************************************************************************************************/
        #region Preferences
        /************************************************************************************************************************/

        public static readonly ModifierKeysPref ActivationModifiers = new(
            EditorStrings.PrefsKeyPrefix + nameof(SceneViewObjectPickerWindow) + "." + nameof(ActivationModifiers),
            ModifierKey.Ctrl);

        public static readonly AutoPrefs.EditorInt ActivationMouseButton = new(
            EditorStrings.PrefsKeyPrefix + nameof(SceneViewObjectPickerWindow) + "." + nameof(ActivationMouseButton),
            1);

        /************************************************************************************************************************/

        public const string Headding = "Scene View Object Picker";

        private static readonly GUIContent
            ActivationModifiersLabel = new GUIContent("Modifier Keys",
                "Keys which must be held while clicking to open the picker window."),
            ActivationMouseButtonLabel = new GUIContent("Mouse Button",
                "Determines which Mouse Button will be used to open the picker window." +
                "\n- Left Click = 0" +
                "\n- Right Click = 1" +
                "\n- Middle Click = 2");

        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        public static void DoPrefsGUI()
        {
            Preferences.DoSceneViewObjectPickerWindowGUI += () =>
            {
                Preferences.DoSectionHeader(Headding);

                ActivationModifiers.DoGUI(ActivationModifiersLabel);
                ActivationMouseButton.DoGUI(ActivationMouseButtonLabel);

                ActivationMouseButton.Value = Mathf.Clamp(ActivationMouseButton, 0, 2);
            };
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
