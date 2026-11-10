using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.Login;
using VRT.Pilots.Common;

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
                new VR2GatherSamplesVersionCheck(),
                new RequiredSamplesCheck(),
                new InputActionsCheck(),
                new TeleportInteractionLayerCheck(),
                new ScenarioRegistryCheck(),
                new OrchestratorRefCheck(),
                new SceneLoadCheck(),
                new SceneSetupCheck(),
                new SoloPlayerCheck(),
                new PlayerStartFloorCheck(),
                new PilotControllerCheck(),
                new PhysicsLayerCheck(),
                new TeleportLayerCheck(),
                new InteractionLayerCheck(),
                new TriggerInteractablesCheck(),
                new ActivateOnlyTriggersCheck(),
                new GrabbableEventsCheck(),
                new TeleportableTagCheck(),
                new InteractableConventionCheck(),
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
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run All Checks"))
            {
                UpdateSceneCheckTargets();
                foreach (var c in _checks)
                    c.RunAndStore();
                LogReport();
            }
            if (GUILayout.Button("Log Report", GUILayout.Width(90)))
                LogReport();
            EditorGUILayout.EndHorizontal();

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

        void LogReport()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== VR2Gather Porting Check Report ===");
            CheckCategory? current = null;
            foreach (var check in _checks)
            {
                if (check.Category != current)
                {
                    current = check.Category;
                    sb.AppendLine($"[{check.Category.ToString().ToUpper()}]");
                }
                string icon = check.Result.Status switch
                {
                    CheckStatus.OK      => "OK     ",
                    CheckStatus.Warning => "WARN   ",
                    CheckStatus.Error   => "ERROR  ",
                    CheckStatus.Skipped => "SKIP   ",
                    _                   => "NOTRUN ",
                };
                sb.AppendLine($"  {icon} {check.Name}: {check.Result.Summary ?? "(no result)"}");
                if (check.Result.Details != null)
                    foreach (var d in check.Result.Details)
                        sb.AppendLine($"           {d}");
            }
            sb.Append("=== End Report ===");
            string report = sb.ToString();
            bool hasError   = _checks.Any(c => c.Result.Status == CheckStatus.Error);
            bool hasWarning = _checks.Any(c => c.Result.Status == CheckStatus.Warning);
            if (hasError)
                Debug.LogError(report);
            else if (hasWarning)
                Debug.LogWarning(report);
            else
                Debug.Log(report);
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
                for (int i = 0; i < check.Result.Details.Count; i++)
                {
                    var selectAction = check.Result.DetailSelectActions != null && i < check.Result.DetailSelectActions.Count
                        ? check.Result.DetailSelectActions[i] : null;
                    if (selectAction != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(check.Result.Details[i], EditorStyles.wordWrappedMiniLabel);
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                            selectAction();
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.LabelField(check.Result.Details[i], EditorStyles.wordWrappedMiniLabel);
                    }
                }
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
        public List<Action> DetailSelectActions; // parallel to Details; null entry = no button for that line
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

        // A Fix button that runs a fixer from VRTPortingFixes on the scene (#383): one Undo step,
        // the fixer's report goes to the console, and the check runs again. Runs after the current
        // GUI event, so the window's layout doesn't change in the middle of it.
        protected Action FixActionFor(Scene scene, Func<Scene, bool, VRTPortingFixes.Report> fixer)
        {
            return () => EditorApplication.delayCall += () =>
            {
                if (!scene.isLoaded) return;
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName($"Porting Check: fix {Name}");
                var report = fixer(scene, false);
                Undo.CollapseUndoOperations(group);
                Debug.Log(report.Text);
                RunAndStore();
                if (EditorWindow.HasOpenInstances<VRTPortingCheckerWindow>())
                    EditorWindow.GetWindow<VRTPortingCheckerWindow>(false, null, false).Repaint();
            };
        }
    }

    // A scene check that is the dry run of a fixer: every object the fixer would change, or that
    // it found but can't fix (an unknown pattern), is listed; Fix runs the fixer for real.
    abstract class FixerCheck : SceneCheck
    {
        protected abstract VRTPortingFixes.Report Fixer(Scene scene, bool dryRun);
        protected abstract string OkSummary { get; }
        protected abstract string ProblemSummary { get; }  // after "N object(s): "
        protected virtual CheckStatus ProblemStatus => CheckStatus.Error;

        protected override CheckResult RunInScene(Scene scene)
        {
            var report = Fixer(scene, true);
            if (report.SkipReason != null)
                return new CheckResult { Status = CheckStatus.Skipped, Summary = report.SkipReason };
            if (report.Items.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = OkSummary };

            var objects = report.Items.Where(i => i.Object != null).Select(i => i.Object).Distinct().ToList();
            string summary = $"{report.Items.Count} {ProblemSummary}";
            if (report.NotFixed > 0)
                summary += report.Changed > 0 ? $" ({report.NotFixed} need manual work)" : " (need manual work)";
            return new CheckResult
            {
                Status = ProblemStatus,
                Summary = summary,
                Details = report.Items.Select(i => (i.Fixable ? "" : "manual: ") + i.Line).ToList(),
                DetailSelectActions = report.Items.Select(i =>
                {
                    var go = i.Object;
                    return go != null ? (Action)(() => Selection.activeGameObject = go) : null;
                }).ToList(),
                SelectAction = () => Selection.objects = objects.Cast<UnityEngine.Object>().ToArray(),
                FixAction = report.Changed > 0 ? FixActionFor(scene, Fixer) : null,
            };
        }
    }

    // ── Global checks ────────────────────────────────────────────────────────────

    class VR2GatherSamplesVersionCheck : PortingCheck
    {
        public override string Name => "VR2Gather Samples";
        public override CheckCategory Category => CheckCategory.Global;

        protected override CheckResult Run()
        {
            // Use a runtime type with an explicit asmdef (VRT.Login) rather than the
            // Tools editor scripts which have no asmdef and resolve to the default assembly.
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ScenarioRegistry).Assembly);
            if (info == null)
                return new CheckResult { Status = CheckStatus.Skipped, Summary = "Could not determine VR2Gather package version" };

            string packageVersion = info.version;

            // In VRTApp-Develop the package assets live directly at Assets/VRTAssets rather
            // than being imported as samples — treat that as equivalent to samples being present.
            if (Directory.Exists(Path.Combine(Application.dataPath, "VRTAssets")))
                return new CheckResult { Status = CheckStatus.OK, Summary = $"Development project: VRTAssets present in Assets (package v{packageVersion})" };

            string samplesRoot = Path.Combine(Application.dataPath, "Samples", "VR2Gather");

            if (!Directory.Exists(samplesRoot) || Directory.GetDirectories(samplesRoot).Length == 0)
                return new CheckResult
                {
                    Status = CheckStatus.Error,
                    Summary = $"VR2Gather samples not imported (package is v{packageVersion})",
                    Details = new List<string> { "Import via Package Manager → VR2Gather → Samples → VRTAssets → Import" },
                };

            bool currentVersionPresent = Directory.GetDirectories(samplesRoot)
                .Any(d => Path.GetFileName(d) == packageVersion);

            if (currentVersionPresent)
                return new CheckResult { Status = CheckStatus.OK, Summary = $"VR2Gather samples v{packageVersion} present" };

            string installed = string.Join(", ", Directory.GetDirectories(samplesRoot).Select(d => Path.GetFileName(d)));
            return new CheckResult
            {
                Status = CheckStatus.Error,
                Summary = $"VR2Gather samples version mismatch — installed: {installed}, package: v{packageVersion}",
                Details = new List<string> { "Re-import VR2Gather samples: Package Manager → VR2Gather → Samples → VRTAssets → Import" },
            };
        }
    }

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
                foreach (var sample in new[] { "Starter Assets", "Hands Interaction Demo" })
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

    class TeleportInteractionLayerCheck : PortingCheck
    {
        public override string Name => "Teleport XRI Layer";
        public override CheckCategory Category => CheckCategory.Global;

        protected override CheckResult Run()
        {
            var maskType = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask, Unity.XR.Interaction.Toolkit");
            if (maskType == null)
                return new CheckResult { Status = CheckStatus.Skipped, Summary = "XRI not installed" };

            var layerToName = maskType.GetMethod("LayerToName",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            string name = layerToName != null ? (string)layerToName.Invoke(null, new object[] { 31 }) : null;

            Action openSettings = () => SettingsService.OpenProjectSettings("Project/XR Interaction Toolkit");

            if (string.IsNullOrEmpty(name) || name != "Teleport")
                return new CheckResult
                {
                    Status = CheckStatus.Error,
                    Summary = $"XRI interaction layer 31 is '{name ?? "(empty)"}', expected 'Teleport'",
                    Details = new List<string> { "Set interaction layer 31 to 'Teleport' in Edit → Project Settings → XR Interaction Toolkit" },
                    OpenAction = openSettings,
                    OpenLabel = "Open Settings",
                };

            return new CheckResult { Status = CheckStatus.OK, Summary = "XRI interaction layer 31 is 'Teleport'" };
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

            // a) User prefab variant check — prefab must live in Assets/, not in Packages/ or Assets/Samples/.
            //    Skip in the VR2Gather development project (Assets/VRTAssets present), where using
            //    the package prefab directly is correct.
            bool isDevProject = Directory.Exists(Path.Combine(Application.dataPath, "VRTAssets"));
            if (!isDevProject)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(registry.gameObject);
                if (source == null)
                    warnings.Add("ScenarioRegistry is not a prefab instance — save it as a prefab variant in your own Assets folder");
                else if (!AssetDatabase.GetAssetPath(source).StartsWith("Assets/") ||
                         AssetDatabase.GetAssetPath(source).StartsWith("Assets/Samples/"))
                    warnings.Add($"ScenarioRegistry prefab is from '{AssetDatabase.GetAssetPath(source)}' — create a user prefab variant in your own Assets folder");
            }

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

    // Direct SceneManager.LoadScene() calls change the scene only for the participant
    // that makes the call. Scene changes should go through PilotController.LoadNewScene(),
    // triggered for everyone via a NetworkTrigger.
    class SceneLoadCheck : PortingCheck
    {
        public override string Name => "Coordinated scene changes";
        public override CheckCategory Category => CheckCategory.Scripts;

        protected override CheckResult Run()
        {
            var details = new List<string>();
            var detailSelects = new List<Action>();
            foreach (var file in Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
            {
                string rel = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("//") || !line.Contains("SceneManager.LoadScene")) continue;
                    int lineNumber = i + 1;
                    details.Add($"{rel}:{lineNumber} → use PilotController.Instance.LoadNewScene(), triggered via a NetworkTrigger");
                    detailSelects.Add(() => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(rel), lineNumber));
                    Debug.LogWarning($"VRTPortingCheck: direct SceneManager.LoadScene in {rel}:{lineNumber}");
                }
            }

            if (details.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = "No direct SceneManager.LoadScene calls" };

            return new CheckResult
            {
                Status = CheckStatus.Warning,
                Summary = $"{details.Count} direct SceneManager.LoadScene call(s), not coordinated across participants",
                Details = details,
                DetailSelectActions = detailSelects,
            };
        }
    }

    // ── Scene checks ─────────────────────────────────────────────────────────────

    class PilotControllerCheck : SceneCheck
    {
        public override string Name => "Pilot Controller";

        protected override CheckResult RunInScene(Scene scene)
        {
            if (AllObjects(scene).Any(go => PortingCheckerUtil.IsDerivedFromAny(go, "Tool_scenesetup_solo")))
                return new CheckResult { Status = CheckStatus.Skipped, Summary = "Solo scene — prefab refs not required (see Solo Player check)" };

            var controllers = AllObjects(scene)
                .Select(go => go.GetComponent<PilotController>())
                .Where(c => c != null)
                .ToList();

            if (controllers.Count == 0)
                return new CheckResult { Status = CheckStatus.Warning, Summary = "No PilotController found in scene" };

            if (controllers.Count > 1)
                return new CheckResult
                {
                    Status = CheckStatus.Error,
                    Summary = $"{controllers.Count} PilotControllers found — exactly one expected",
                    Details = controllers.Select(c => $"{c.gameObject.name} ({c.GetType().Name})").ToList(),
                    SelectAction = () => Selection.objects = controllers.Select(c => (UnityEngine.Object)c.gameObject).ToArray(),
                };

            var issues = new List<string>();
            var issueObjects = new List<GameObject>();

            foreach (var controller in controllers)
            {
                var so = new SerializedObject(controller);
                // The prefabs are only used to play the scene on its own in the editor; a developer who
                // turned playableInEditor off (default on) doesn't need them, e.g. the login scene.
                if (so.FindProperty("playableInEditor")?.boolValue == false) continue;
                bool configMissing = so.FindProperty("configurationPrefab")?.objectReferenceValue == null;
                bool orchMissing   = so.FindProperty("orchestratorPrefab")?.objectReferenceValue == null;

                if (configMissing || orchMissing)
                {
                    var missing = new List<string>();
                    if (configMissing) missing.Add("configurationPrefab");
                    if (orchMissing)   missing.Add("orchestratorPrefab");
                    issues.Add($"{controller.gameObject.name} ({controller.GetType().Name}) — {string.Join(", ", missing)} not set");
                    issueObjects.Add(controller.gameObject);
                }
            }

            if (issues.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = $"{controllers.Count} PilotController(s) have required prefabs set, or are not playable in the editor" };

            return new CheckResult
            {
                Status = CheckStatus.Error,
                Summary = $"{issues.Count} PilotController(s) missing required prefab references",
                Details = issues,
                SelectAction = () => Selection.objects = issueObjects.Cast<UnityEngine.Object>().ToArray(),
                FixAction = FixActionFor(scene, VRTPortingFixes.FixPilotControllerPrefabs),
            };
        }
    }

    class SceneSetupCheck : SceneCheck
    {
        public override string Name => "Scene Setup";

        protected override CheckResult RunInScene(Scene scene)
        {
            bool hasFull = AllObjects(scene).Any(go => PortingCheckerUtil.IsDerivedFromAny(go, "Tool_scenesetup"));
            if (hasFull)
                return new CheckResult { Status = CheckStatus.OK, Summary = "Tool_scenesetup present" };

            bool hasSolo = AllObjects(scene).Any(go => PortingCheckerUtil.IsDerivedFromAny(go, "Tool_scenesetup_solo"));
            if (hasSolo)
                return new CheckResult { Status = CheckStatus.OK, Summary = "Scene uses Tool_scenesetup_solo — solo-only, no networking" };

            return new CheckResult
            {
                Status = CheckStatus.Error,
                Summary = "No Tool_scenesetup (or variant) found in scene",
                Details = new List<string> { "Every VR2Gather scene requires the Tool_scenesetup prefab — add it from the VR2Gather package" },
            };
        }
    }

    class SoloPlayerCheck : SceneCheck
    {
        public override string Name => "Solo Player";

        protected override CheckResult RunInScene(Scene scene)
        {
            bool isSolo = AllObjects(scene).Any(go => PortingCheckerUtil.IsDerivedFromAny(go, "Tool_scenesetup_solo"));
            if (!isSolo)
                return new CheckResult { Status = CheckStatus.Skipped, Summary = "Not a solo scene" };

            var players = AllObjects(scene)
                .Where(go => PortingCheckerUtil.IsDerivedFromAny(go, "P_Self_Player"))
                .ToList();

            if (players.Count == 0)
                return new CheckResult { Status = CheckStatus.Error, Summary = "Solo scene has no P_Self_Player instance" };

            var inactive = players.Where(go => !go.activeInHierarchy).ToList();
            if (inactive.Count > 0)
                return new CheckResult
                {
                    Status = CheckStatus.Warning,
                    Summary = $"{inactive.Count} P_Self_Player instance(s) present but inactive",
                    Details = inactive.Select(go => PortingCheckerUtil.HierarchyPath(go)).ToList(),
                    SelectAction = () => Selection.objects = inactive.Cast<UnityEngine.Object>().ToArray(),
                };

            return new CheckResult { Status = CheckStatus.OK, Summary = $"{players.Count} P_Self_Player instance(s) present and active" };
        }
    }

    // New players are copies of SessionPlayersManager's SelfPlayerPrefab/PlayerPrefab (in Tool_scenesetup:
    // disabled scene objects) and start at the template's position until they are moved to their location.
    // If there is no floor under that position they fall when physics runs first (#404). Developers may
    // move the templates (for a scene that isn't around the origin), so this checks for a floor, not for (0,0,0).
    class PlayerStartFloorCheck : SceneCheck
    {
        public override string Name => "Player Start Floor";

        const float RayStartAbove = 0.5f;  // start the ray a bit above the template, so a floor at exactly its height is hit
        const float MaxDropBelow = 2.0f;   // a floor more than this far below the template doesn't count

        protected override CheckResult RunInScene(Scene scene)
        {
            var managers = AllObjects(scene)
                .Select(go => go.GetComponent<SessionPlayersManager>())
                .Where(m => m != null)
                .ToList();
            if (managers.Count == 0)
                return new CheckResult { Status = CheckStatus.Skipped, Summary = "No SessionPlayersManager in scene" };

            Physics.SyncTransforms();
            var physics = scene.GetPhysicsScene();
            var details = new List<string>();
            var selects = new List<Action>();
            var found = new List<string>();
            var foundSelects = new List<Action>();
            var bad = new List<GameObject>();
            int nChecked = 0;

            foreach (var m in managers)
            {
                foreach (var (label, template) in new[] { ("SelfPlayerPrefab", m.SelfPlayerPrefab), ("PlayerPrefab", m.PlayerPrefab) })
                {
                    if (template == null) continue;
                    nChecked++;
                    // For a scene object this is its world position; for a prefab asset, its root position:
                    // either way, where Instantiate() without a position puts the copy.
                    Vector3 start = template.transform.position;
                    bool hit = physics.Raycast(start + Vector3.up * RayStartAbove, Vector3.down, out RaycastHit info,
                        RayStartAbove + MaxDropBelow, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    var go = template;
                    if (hit)
                    {
                        var floor = info.collider.gameObject;
                        found.Add($"{PortingCheckerUtil.HierarchyPath(m.gameObject)}.{label}: new players start at {start}, above '{PortingCheckerUtil.HierarchyPath(floor)}' ({info.distance - RayStartAbove:F2} m below)");
                        foundSelects.Add(() => Selection.activeObject = floor);
                        continue;
                    }
                    bad.Add(go);
                    details.Add($"{PortingCheckerUtil.HierarchyPath(m.gameObject)}.{label}: new players start at {start}, no floor collider within {MaxDropBelow} m below");
                    selects.Add(() => Selection.activeObject = go);
                }
            }

            if (nChecked == 0)
                return new CheckResult { Status = CheckStatus.Warning, Summary = "SessionPlayersManager has no player templates set" };
            if (bad.Count == 0)
                return new CheckResult
                {
                    Status = CheckStatus.OK,
                    Summary = $"New players start above a floor ({nChecked} template(s))",
                    Details = found,
                    DetailSelectActions = foundSelects,
                };

            return new CheckResult
            {
                Status = CheckStatus.Warning,
                Summary = $"{bad.Count} player template(s) not above a floor: new players fall before they are placed",
                Details = details,
                DetailSelectActions = selects,
                SelectAction = () => Selection.objects = bad.Cast<UnityEngine.Object>().ToArray(),
            };
        }
    }

    class PhysicsLayerCheck : FixerCheck
    {
        public override string Name => "Physics Layers";
        protected override string OkSummary => "No objects on old VRT layers 28/29";
        protected override string ProblemSummary => "object(s) on layer 28 or 29, should be Default (0)";
        protected override VRTPortingFixes.Report Fixer(Scene scene, bool dryRun) =>
            VRTPortingFixes.FixPhysicsLayers(scene, dryRun);
    }

    class TeleportLayerCheck : FixerCheck
    {
        public override string Name => "Teleport Layer";
        protected override string OkSummary => "All teleportation areas have only interaction layer 31 (Teleport)";
        protected override string ProblemSummary => "teleportation area(s) with old interaction layers, should be 31 (Teleport)";
        protected override VRTPortingFixes.Report Fixer(Scene scene, bool dryRun) =>
            VRTPortingFixes.FixInteractionLayers(scene, dryRun, VRTPortingFixes.InteractionLayerScope.Teleportation);
    }

    class InteractionLayerCheck : FixerCheck
    {
        public override string Name => "Interaction Layers";
        protected override string OkSummary => "No old interaction layer bits found";
        protected override string ProblemSummary => "interactable(s) with old interaction layer bits 2/4/8, should be Default";
        protected override VRTPortingFixes.Report Fixer(Scene scene, bool dryRun) =>
            VRTPortingFixes.FixInteractionLayers(scene, dryRun, VRTPortingFixes.InteractionLayerScope.NonTeleportation);
    }

    // VR controller casters ignore trigger colliders, the keyboard/mouse ray doesn't (#383, #385).
    class TriggerInteractablesCheck : FixerCheck
    {
        public override string Name => "Trigger-only Interactables";
        protected override string OkSummary => "All interactables have a solid collider";
        protected override string ProblemSummary => "interactable(s) with only trigger colliders: invisible to the VR controllers";
        protected override VRTPortingFixes.Report Fixer(Scene scene, bool dryRun) =>
            VRTPortingFixes.FixTriggerInteractables(scene, dryRun);
    }

    // Old buttons call NetworkTrigger.Trigger only on Activated, so poking them does nothing (#383).
    class ActivateOnlyTriggersCheck : FixerCheck
    {
        public override string Name => "Activate-only Triggers";
        protected override string OkSummary => "All NetworkTrigger buttons react to Select Entered";
        protected override string ProblemSummary => "button(s) that call NetworkTrigger only on Activated: poke does nothing";
        protected override CheckStatus ProblemStatus => CheckStatus.Warning;
        protected override VRTPortingFixes.Report Fixer(Scene scene, bool dryRun) =>
            VRTPortingFixes.FixActivateOnlyTriggers(scene, dryRun);
    }

    // Events wired to VRTGrabbableController methods that were renamed: Unity silently skips them (#383).
    class GrabbableEventsCheck : FixerCheck
    {
        public override string Name => "Grabbable Events";
        protected override string OkSummary => "No calls to old VRTGrabbableController methods";
        protected override string ProblemSummary => "event call(s) to old VRTGrabbableController methods: grabs are not synchronized";
        protected override VRTPortingFixes.Report Fixer(Scene scene, bool dryRun) =>
            VRTPortingFixes.FixGrabbableEvents(scene, dryRun);
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

    class InteractableConventionCheck : SceneCheck
    {
        public override string Name => "Interactable Conventions";

        protected override CheckResult RunInScene(Scene scene)
        {
            var baseType = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable, Unity.XR.Interaction.Toolkit");
            if (baseType == null)
                return new CheckResult { Status = CheckStatus.Skipped, Summary = "XRI not installed" };

            var teleportType = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.BaseTeleportationInteractable, Unity.XR.Interaction.Toolkit");

            var details       = new List<string>();
            var detailSelects = new List<Action>();
            var allBadObjects = new List<GameObject>();
            int bareCount = 0, autoCount = 0, manualCount = 0;

            foreach (var go in AllObjects(scene))
            {
                if (go.GetComponent(baseType) == null) continue;
                if (teleportType != null && go.GetComponent(teleportType) != null) continue;
                if (PortingCheckerUtil.IsDerivedFromAny(go, "PFB_Grabbable", "PFB_Trigger")) continue;

                allBadObjects.Add(go);
                var captured = go;
                string label;

                if (!PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    label = $"{go.name} — bare scene object (not a prefab); works fine, easy to auto-convert — don't use as template for new work";
                    bareCount++;
                }
                else if (PortingCheckerUtil.HasMixedRootComponents(go))
                {
                    label = $"{go.name} — non-VR2Gather prefab, mixed root (visuals + interactable on same GO) — needs manual pre-cleaning before auto-conversion";
                    manualCount++;
                }
                else
                {
                    label = $"{go.name} — non-VR2Gather prefab, clean root — auto-convertible";
                    autoCount++;
                }

                details.Add(label);
                detailSelects.Add(() => Selection.activeGameObject = captured);
            }

            if (details.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = "All interactables follow VR2Gather conventions" };

            var parts = new List<string>();
            if (bareCount  > 0) parts.Add($"{bareCount} bare scene object(s)");
            if (autoCount  > 0) parts.Add($"{autoCount} auto-convertible prefab(s)");
            if (manualCount > 0) parts.Add($"{manualCount} prefab(s) needing manual work");

            return new CheckResult
            {
                Status = CheckStatus.Warning,
                Summary = string.Join("; ", parts),
                Details = details,
                DetailSelectActions = detailSelects,
                SelectAction = () => Selection.objects = allBadObjects.Cast<UnityEngine.Object>().ToArray(),
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

        public static bool IsDerivedFromAny(GameObject go, params string[] baseNames)
        {
            var nameSet = new HashSet<string>(baseNames);
            var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            while (source != null)
            {
                if (nameSet.Contains(source.name)) return true;
                source = PrefabUtility.GetCorrespondingObjectFromSource(source);
            }
            return false;
        }

        // Returns true if the GO has components beyond Transform, Rigidbody, Colliders,
        // XR interaction components, and VRT components — i.e. it has "content" (mesh, audio,
        // custom logic) mixed onto the same root as its interactable, which makes auto-conversion
        // harder because those components would need to be moved to children first.
        public static bool HasMixedRootComponents(GameObject go)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var t = comp.GetType();
                if (t == typeof(Transform)) continue;
                if (t == typeof(Rigidbody)) continue;
                if (typeof(Collider).IsAssignableFrom(t)) continue;
                if (t.Namespace != null && t.Namespace.StartsWith("UnityEngine.XR")) continue;
                if (t.Namespace != null && t.Namespace.StartsWith("VRT")) continue;
                return true;
            }
            return false;
        }
    }
}
