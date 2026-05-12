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
                new PilotControllerCheck(),
                new SceneSetupCheck(),
                new PhysicsLayerCheck(),
                new TeleportLayerCheck(),
                new InteractionLayerCheck(),
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

    class PilotControllerCheck : SceneCheck
    {
        public override string Name => "Pilot Controller";

        protected override CheckResult RunInScene(Scene scene)
        {
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
                return new CheckResult { Status = CheckStatus.OK, Summary = $"{controllers.Count} PilotController(s) have required prefabs set" };

            return new CheckResult
            {
                Status = CheckStatus.Error,
                Summary = $"{issues.Count} PilotController(s) missing required prefab references",
                Details = issues,
                SelectAction = () => Selection.objects = issueObjects.Cast<UnityEngine.Object>().ToArray(),
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
                return new CheckResult { Status = CheckStatus.Warning, Summary = "Scene uses Tool_scenesetup_solo — solo-only, no networking" };

            return new CheckResult
            {
                Status = CheckStatus.Error,
                Summary = "No Tool_scenesetup (or variant) found in scene",
                Details = new List<string> { "Every VR2Gather scene requires the Tool_scenesetup prefab — add it from the VR2Gather package" },
            };
        }
    }

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
            var interactableType = Type.GetType(
                "UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.BaseTeleportationInteractable, Unity.XR.Interaction.Toolkit");
            if (interactableType == null)
                return new CheckResult { Status = CheckStatus.Skipped, Summary = "XRI not installed" };

            const uint teleportBit = 1u << 31;
            var issues = new List<string>();
            var issueObjects = new List<GameObject>();

            foreach (var go in AllObjects(scene))
            {
                var comp = go.GetComponent(interactableType) as Component;
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                var bits = so.FindProperty("m_InteractionLayers.m_Bits");
                if (bits != null && ((uint)bits.longValue & teleportBit) == 0)
                {
                    issues.Add($"{go.name} — XRI interaction layer 31 (Teleport) not set (mask: {bits.longValue})");
                    issueObjects.Add(go);
                }
            }

            if (issues.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = "All teleportation areas have interaction layer 31 (Teleport)" };

            return new CheckResult
            {
                Status = CheckStatus.Error,
                Summary = $"{issues.Count} teleportation area(s) missing interaction layer 31 (Teleport)",
                Details = issues,
                SelectAction = () => Selection.objects = issueObjects.Cast<UnityEngine.Object>().ToArray(),
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
                var bits = so.FindProperty("m_InteractionLayers.m_Bits");
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
            int errorCount    = 0;

            foreach (var go in AllObjects(scene))
            {
                if (go.GetComponent(baseType) == null) continue;
                if (teleportType != null && go.GetComponent(teleportType) != null) continue;
                if (PortingCheckerUtil.IsDerivedFromAny(go, "PFB_Grabbable", "PFB_Trigger")) continue;

                allBadObjects.Add(go);
                var captured = go;
                if (PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    details.Add($"{go.name} — interactable is part of a non-VR2Gather prefab");
                }
                else
                {
                    details.Add($"{go.name} — interactable is not part of any prefab (will block future automated porting)");
                    errorCount++;
                }
                detailSelects.Add(() => Selection.activeGameObject = captured);
            }

            if (details.Count == 0)
                return new CheckResult { Status = CheckStatus.OK, Summary = "All interactables follow VR2Gather conventions" };

            int warnCount = details.Count - errorCount;
            return new CheckResult
            {
                Status = errorCount > 0 ? CheckStatus.Error : CheckStatus.Warning,
                Summary = errorCount > 0
                    ? $"{errorCount} bare interactable(s) (not in any prefab); {warnCount} in non-VR2Gather prefab(s)"
                    : $"{warnCount} interactable(s) in non-VR2Gather prefab(s)",
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
    }
}
