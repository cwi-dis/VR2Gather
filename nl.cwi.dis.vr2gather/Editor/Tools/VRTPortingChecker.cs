using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.Login;

namespace VRT.Tools
{
    public class VRTPortingCheckerWindow : EditorWindow
    {
        [MenuItem("Tools/VR2Gather Porting Check")]
        static void Open() => GetWindow<VRTPortingCheckerWindow>("VR2Gather Porting Check");

        List<PortingCheck> _checks;
        Vector2 _scroll;
        string[] _sceneNames = new string[0];
        string[] _scenePaths = new string[0];
        int _selectedScene = -1;

        void OnEnable()
        {
            _checks = new List<PortingCheck>
            {
                new RequiredSamplesCheck(),
                new InputActionsCheck(),
                new ScenarioRegistryCheck(),
                new OrchestratorRefCheck(),
                new PhysicsLayerCheck(),
                new TeleportLayerCheck(),
                new InteractionLayerCheck(),
                new TeleportableTagCheck(),
            };
            InitScenes();
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
        }

        void OnDisable()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
        }

        void OnActiveSceneChanged(Scene _, Scene __)
        {
            InitScenes();
            Repaint();
        }

        void InitScenes()
        {
            var buildScenes = EditorBuildSettings.scenes;
            _sceneNames = buildScenes.Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();
            _scenePaths = buildScenes.Select(s => s.path).ToArray();

            string active = Path.GetFileNameWithoutExtension(EditorSceneManager.GetActiveScene().path);
            int idx = Array.IndexOf(_sceneNames, active);
            _selectedScene = idx >= 0 ? idx : (_sceneNames.Length > 0 ? 0 : -1);
            UpdateSceneCheckTargets();
        }

        void UpdateSceneCheckTargets()
        {
            string path = _selectedScene >= 0 && _selectedScene < _scenePaths.Length
                ? _scenePaths[_selectedScene] : null;
            foreach (var c in _checks.OfType<SceneCheck>())
                c.TargetScenePath = path;
        }

        void OnGUI()
        {
            if (GUILayout.Button("Run All Checks"))
            {
                UpdateSceneCheckTargets();
                foreach (var c in _checks)
                    c.RunAndStore();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            CheckCategory? current = null;
            foreach (var check in _checks)
            {
                if (check.Category != current)
                {
                    current = check.Category;
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(check.Category.ToString().ToUpper(), EditorStyles.boldLabel);
                    if (current == CheckCategory.Scene)
                        DrawScenePicker();
                }
                DrawRow(check);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawScenePicker()
        {
            if (_sceneNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No scenes in Build Settings — add scenes to File → Build Settings first.", MessageType.Warning);
                return;
            }

            string activeName = Path.GetFileNameWithoutExtension(EditorSceneManager.GetActiveScene().path);
            bool activeInList = Array.IndexOf(_sceneNames, activeName) >= 0;

            EditorGUILayout.BeginHorizontal();

            Color prev = GUI.color;
            if (!activeInList)
            {
                GUI.color = ColorWarning;
                GUILayout.Label("Select VR2Gather scene:", GUILayout.Width(160));
                GUI.color = prev;
            }
            else
            {
                GUILayout.Label("Scene:", GUILayout.Width(50));
            }

            int sel = _selectedScene >= 0 ? _selectedScene : 0;
            int newSel = EditorGUILayout.Popup(sel, _sceneNames);
            if (newSel != _selectedScene)
            {
                _selectedScene = newSel;
                UpdateSceneCheckTargets();
            }

            bool loaded = _selectedScene >= 0 &&
                EditorSceneManager.GetSceneByPath(_scenePaths[_selectedScene]).isLoaded;
            if (!loaded && GUILayout.Button("Open", GUILayout.Width(50)))
                EditorSceneManager.OpenScene(_scenePaths[_selectedScene]);

            EditorGUILayout.EndHorizontal();
        }

        static readonly Color ColorOK      = new Color(0.3f, 0.9f, 0.3f);
        static readonly Color ColorWarning = new Color(1.0f, 0.8f, 0.0f);
        static readonly Color ColorError   = new Color(1.0f, 0.3f, 0.3f);
        static readonly Color ColorGray    = new Color(0.6f, 0.6f, 0.6f);

        void DrawRow(PortingCheck check)
        {
            Color prev = GUI.color;
            EditorGUILayout.BeginHorizontal();

            string icon;
            Color iconColor;
            switch (check.Result.Status)
            {
                case CheckStatus.OK:      icon = "✓"; iconColor = ColorOK;      break;
                case CheckStatus.Warning: icon = "⚠"; iconColor = ColorWarning; break;
                case CheckStatus.Error:   icon = "✗"; iconColor = ColorError;   break;
                default:                  icon = "–"; iconColor = ColorGray;    break;
            }

            GUI.color = iconColor;
            GUILayout.Label(icon, GUILayout.Width(18));
            GUI.color = prev;
            GUILayout.Label(check.Name, GUILayout.Width(160));

            GUILayout.Label(check.Result.Summary ?? "", GUILayout.ExpandWidth(true));
            if (check.Result.FixAction != null && GUILayout.Button("Fix", GUILayout.Width(36)))
                check.Result.FixAction();
            if (check.Result.SelectAction != null && GUILayout.Button("Select", GUILayout.Width(50)))
                check.Result.SelectAction();
            if (check.Result.OpenAction != null && GUILayout.Button(check.Result.OpenLabel ?? "Open", GUILayout.Width(80)))
                check.Result.OpenAction();
            if (GUILayout.Button("↺", GUILayout.Width(22)))
            {
                UpdateSceneCheckTargets();
                check.RunAndStore();
            }

            EditorGUILayout.EndHorizontal();

            if (check.Result.Details != null && check.Result.Details.Count > 0)
            {
                EditorGUI.indentLevel += 2;
                foreach (var d in check.Result.Details)
                    EditorGUILayout.LabelField(d, EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel -= 2;
            }
        }
    }

    // ── Data model ───────────────────────────────────────────────────────────────

    enum CheckCategory { Global, Scripts, Scene }

    enum CheckStatus { NotRun, OK, Warning, Error, Skipped }

    class CheckResult
    {
        public CheckStatus Status = CheckStatus.NotRun;
        public string Summary;
        public List<string> Details;
        public Action FixAction;
        public Action SelectAction;
        public Action OpenAction;
        public string OpenLabel;
    }

    abstract class PortingCheck
    {
        public abstract string Name { get; }
        public abstract CheckCategory Category { get; }
        public CheckResult Result { get; private set; } = new CheckResult();
        public void RunAndStore() => Result = Run();
        protected abstract CheckResult Run();
    }

    abstract class SceneCheck : PortingCheck
    {
        public string TargetScenePath { get; set; }
        public override CheckCategory Category => CheckCategory.Scene;

        protected override CheckResult Run()
        {
            if (string.IsNullOrEmpty(TargetScenePath))
                return new CheckResult { Status = CheckStatus.Skipped, Summary = "No scene selected" };

            var scene = EditorSceneManager.GetSceneByPath(TargetScenePath);
            if (!scene.isLoaded)
                return new CheckResult
                {
                    Status = CheckStatus.Skipped,
                    Summary = $"Open {Path.GetFileNameWithoutExtension(TargetScenePath)} to run this check",
                };

            return RunInScene(scene);
        }

        protected abstract CheckResult RunInScene(Scene scene);

        protected static IEnumerable<GameObject> AllObjects(Scene scene) =>
            scene.GetRootGameObjects()
                 .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                 .Select(t => t.gameObject);
    }

    // ── Global checks ────────────────────────────────────────────────────────────

    class RequiredSamplesCheck : PortingCheck
    {
        public override string Name => "Required Samples";
        public override CheckCategory Category => CheckCategory.Global;

        protected override CheckResult Run()
        {
            var missing = new List<string>();

            string xritRoot = Path.Combine(Application.dataPath, "Samples", "XR Interaction Toolkit");
            if (!Directory.Exists(xritRoot))
            {
                missing.Add("XR Interaction Toolkit samples folder absent — import via Package Manager → XR Interaction Toolkit → Samples");
            }
            else
            {
                foreach (var sample in new[] { "Starter Assets", "Hands Interaction Demo", "World Space UI" })
                {
                    bool found = Directory.GetDirectories(xritRoot)
                        .Any(v => Directory.Exists(Path.Combine(v, sample)));
                    if (!found)
                        missing.Add($"XRIT '{sample}' not imported — Package Manager → XR Interaction Toolkit → Samples → {sample} → Import");
                }
            }

            if (!Directory.Exists(Path.Combine(Application.dataPath, "TextMesh Pro")))
                missing.Add("TMP Essentials missing — Window → TextMeshPro → Import TMP Essential Resources");

            if (missing.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = "All required samples present" };

            return new CheckResult { Status = CheckStatus.Error, Summary = $"{missing.Count} item(s) missing", Details = missing };
        }
    }

    class InputActionsCheck : PortingCheck
    {
        public override string Name => "Input Actions";
        public override CheckCategory Category => CheckCategory.Global;

        protected override CheckResult Run()
        {
            EditorBuildSettings.TryGetConfigObject("com.unity.input.settings.actions", out UnityEngine.Object obj);
            Action openSettings = () => SettingsService.OpenProjectSettings("Project/Input System Package");

            if (obj == null)
                return new CheckResult
                {
                    Status = CheckStatus.Error,
                    Summary = "No project-wide Input Actions set",
                    Details = new List<string> { "Assign VR2GatherInputActions in Project Settings → Input System Package" },
                    OpenAction = openSettings,
                    OpenLabel = "Open Settings",
                };

            if (obj.name != "VR2GatherInputActions")
                return new CheckResult
                {
                    Status = CheckStatus.Warning,
                    Summary = $"Actions set to '{obj.name}', expected VR2GatherInputActions",
                    OpenAction = openSettings,
                    OpenLabel = "Open Settings",
                };

            return new CheckResult { Status = CheckStatus.OK, Summary = "VR2GatherInputActions is set" };
        }
    }

    class ScenarioRegistryCheck : PortingCheck
    {
        public override string Name => "Scenario Registry";
        public override CheckCategory Category => CheckCategory.Global;

        protected override CheckResult Run()
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene VRTLoginManager");
            if (guids.Length == 0)
                return new CheckResult { Status = CheckStatus.Error, Summary = "VRTLoginManager scene not found in project" };

            string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var scene = EditorSceneManager.GetSceneByPath(scenePath);

            if (!scene.isLoaded)
                return new CheckResult
                {
                    Status = CheckStatus.Skipped,
                    Summary = "Open VRTLoginManager scene to run this check",
                    OpenAction = () => EditorSceneManager.OpenScene(scenePath),
                    OpenLabel = "Open Scene",
                };

            var registry = UnityEngine.Object.FindObjectsByType<ScenarioRegistry>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                               .FirstOrDefault();
            if (registry == null)
                return new CheckResult { Status = CheckStatus.Error, Summary = "No ScenarioRegistry found in VRTLoginManager scene" };

            var errors = new List<string>();
            var warnings = new List<string>();

            // a) User prefab variant check — prefab must live in Assets/, not in Packages/ or Assets/Samples/
            var source = PrefabUtility.GetCorrespondingObjectFromSource(registry.gameObject);
            if (source == null)
                warnings.Add("ScenarioRegistry is not a prefab instance — save it as a prefab variant in your own Assets folder");
            else if (!AssetDatabase.GetAssetPath(source).StartsWith("Assets/") ||
                     AssetDatabase.GetAssetPath(source).StartsWith("Assets/Samples/"))
                warnings.Add($"ScenarioRegistry prefab is from '{AssetDatabase.GetAssetPath(source)}' — create a user prefab variant in your own Assets folder");

            // b) Consistency between ScenarioRegistry and Build Settings (bi-directional)
            var buildSceneNames = EditorBuildSettings.scenes
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .ToHashSet();
            var registrySceneNames = registry.Scenarios
                .Select(s => s.scenarioSceneName)
                .ToHashSet();

            foreach (var s in registry.Scenarios)
                if (!buildSceneNames.Contains(s.scenarioSceneName))
                    errors.Add($"Scene '{s.scenarioSceneName}' (scenario '{s.scenarioName}') is in ScenarioRegistry but missing from Build Settings");

            foreach (var buildName in buildSceneNames)
                if (buildName != "VRTLoginManager" && !registrySceneNames.Contains(buildName))
                    warnings.Add($"Scene '{buildName}' is in Build Settings but not registered in ScenarioRegistry");

            var allIssues = warnings.Concat(errors).ToList();
            return new CheckResult
            {
                Status = errors.Count > 0 ? CheckStatus.Error : warnings.Count > 0 ? CheckStatus.Warning : CheckStatus.OK,
                Summary = allIssues.Count == 0
                    ? $"{registry.Scenarios.Count} scenario(s) all OK"
                    : $"{errors.Count} error(s), {warnings.Count} warning(s)",
                Details = allIssues.Count > 0 ? allIssues : null,
            };
        }
    }

    // ── Script checks ────────────────────────────────────────────────────────────

    class OrchestratorRefCheck : PortingCheck
    {
        public override string Name => "Orchestrator API";
        public override CheckCategory Category => CheckCategory.Scripts;

        protected override CheckResult Run()
        {
            var hits = new List<string>();
            foreach (var file in Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
            {
                if (File.ReadAllText(file).Contains("OrchestratorController.Instance"))
                {
                    string rel = "Assets" + file.Substring(Application.dataPath.Length);
                    hits.Add($"{rel} → replace with VRTOrchestratorSingleton.Comm");
                    Debug.LogWarning($"VRTPortingCheck: OrchestratorController.Instance in {rel}");
                }
            }

            if (hits.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = "No OrchestratorController.Instance references" };

            return new CheckResult { Status = CheckStatus.Warning, Summary = $"{hits.Count} file(s) use old API", Details = hits };
        }
    }

    // ── Scene checks ─────────────────────────────────────────────────────────────

    class PhysicsLayerCheck : SceneCheck
    {
        public override string Name => "Physics Layers";

        protected override CheckResult RunInScene(Scene scene)
        {
            var wrong = AllObjects(scene)
                .Where(go => go.layer == 28 || go.layer == 29)
                .ToList();

            if (wrong.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = "No objects on old VRT layers 28/29" };

            return new CheckResult
            {
                Status = CheckStatus.Error,
                Summary = $"{wrong.Count} object(s) on layer 28 or 29 — should be Default (0)",
                Details = wrong.Select(go => $"[layer {go.layer}] {PortingCheckerUtil.HierarchyPath(go)}").ToList(),
                SelectAction = () => Selection.objects = wrong.Cast<UnityEngine.Object>().ToArray(),
            };
        }
    }

    class TeleportLayerCheck : SceneCheck
    {
        public override string Name => "Teleport Layer";

        protected override CheckResult RunInScene(Scene scene)
        {
            var type = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.BaseTeleportationInteractable, Unity.XR.Interaction.Toolkit");
            if (type == null)
                return new CheckResult { Status = CheckStatus.Skipped, Summary = "XRI not installed" };

            var wrong = AllObjects(scene)
                .Where(go => go.GetComponent(type) != null && go.layer != 31)
                .ToList();

            if (wrong.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = "All teleportation areas on layer 31" };

            return new CheckResult
            {
                Status = CheckStatus.Error,
                Summary = $"{wrong.Count} teleportation area(s) not on layer 31 (Teleport)",
                Details = wrong.Select(go => $"[layer {go.layer}] {go.name}").ToList(),
                SelectAction = () => Selection.objects = wrong.Cast<UnityEngine.Object>().ToArray(),
            };
        }
    }

    class InteractionLayerCheck : SceneCheck
    {
        public override string Name => "Interaction Layers";

        protected override CheckResult RunInScene(Scene scene)
        {
            var type = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable, Unity.XR.Interaction.Toolkit");
            if (type == null)
                return new CheckResult { Status = CheckStatus.Skipped, Summary = "XRI not installed" };

            var issues = new List<string>();
            var issueObjects = new List<GameObject>();

            foreach (var go in AllObjects(scene))
            {
                var comp = go.GetComponent(type) as Component;
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                var bits = so.FindProperty("m_InteractionLayerMask.m_Bits");
                if (bits != null && ((uint)bits.longValue & 14u) != 0)
                {
                    issues.Add($"{go.name} — interaction layer bits: {bits.longValue} (bits 2/4/8 set)");
                    issueObjects.Add(go);
                }
            }

            if (issues.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = "No old interaction layer bits found" };

            return new CheckResult
            {
                Status = CheckStatus.Error,
                Summary = $"{issues.Count} interactable(s) have old interaction layer bits",
                Details = issues,
                SelectAction = () => Selection.objects = issueObjects.Cast<UnityEngine.Object>().ToArray(),
            };
        }
    }

    class TeleportableTagCheck : SceneCheck
    {
        public override string Name => "Teleportable Tag";

        protected override CheckResult RunInScene(Scene scene)
        {
            if (!UnityEditorInternal.InternalEditorUtility.tags.Contains("Teleportable"))
                return new CheckResult { Status = CheckStatus.OK, Summary = "Tag 'Teleportable' not defined" };

            var tagged = AllObjects(scene)
                .Where(go => { try { return go.CompareTag("Teleportable"); } catch { return false; } })
                .ToList();

            if (tagged.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = "No objects tagged 'Teleportable'" };

            return new CheckResult
            {
                Status = CheckStatus.Warning,
                Summary = $"{tagged.Count} object(s) have old 'Teleportable' tag — review these",
                Details = tagged.Select(go => go.name).ToList(),
                SelectAction = () => Selection.objects = tagged.Cast<UnityEngine.Object>().ToArray(),
            };
        }
    }

    // ── Utilities ────────────────────────────────────────────────────────────────

    static class PortingCheckerUtil
    {
        public static string HierarchyPath(GameObject go)
        {
            string path = go.name;
            var t = go.transform.parent;
            while (t != null) { path = t.name + "/" + path; t = t.parent; }
            return path;
        }
    }
}
