using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRT.Login;

namespace VRT.Tools
{
    public class VRTPortingCheckerWindow : EditorWindow
    {
        [MenuItem("Tools/VR2Gather Porting Check")]
        static void Open() => GetWindow<VRTPortingCheckerWindow>("VR2Gather Porting Check");

        List<PortingCheck> _checks;
        Vector2 _scroll;

        void OnEnable()
        {
            _checks = new List<PortingCheck>
            {
                new RequiredSamplesCheck(),
                new InputActionsCheck(),
                new ScenarioRegistryCheck(),
                new OrchestratorRefCheck(),
                new DeferredCheck("Physics Layers",     CheckCategory.Scene),
                new DeferredCheck("Teleport Layer",     CheckCategory.Scene),
                new DeferredCheck("Interaction Layers", CheckCategory.Scene),
                new DeferredCheck("Teleportable Tag",   CheckCategory.Scene),
            };
        }

        void OnGUI()
        {
            if (GUILayout.Button("Run All Checks"))
                foreach (var c in _checks)
                    if (!(c is DeferredCheck))
                        c.RunAndStore();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            CheckCategory? current = null;
            foreach (var check in _checks)
            {
                if (check.Category != current)
                {
                    current = check.Category;
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(check.Category.ToString().ToUpper(), EditorStyles.boldLabel);
                }
                DrawRow(check);
            }

            EditorGUILayout.EndScrollView();
        }

        static readonly Color ColorOK      = new Color(0.3f, 0.9f, 0.3f);
        static readonly Color ColorWarning = new Color(1.0f, 0.8f, 0.0f);
        static readonly Color ColorError   = new Color(1.0f, 0.3f, 0.3f);
        static readonly Color ColorGray    = new Color(0.6f, 0.6f, 0.6f);

        void DrawRow(PortingCheck check)
        {
            bool deferred = check is DeferredCheck;
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
            GUI.color = deferred ? ColorGray : prev;
            GUILayout.Label(check.Name, GUILayout.Width(160));
            GUI.color = prev;

            if (deferred)
            {
                GUI.color = ColorGray;
                GUILayout.Label("(future)", GUILayout.ExpandWidth(true));
                GUI.color = prev;
            }
            else
            {
                GUILayout.Label(check.Result.Summary ?? "", GUILayout.ExpandWidth(true));
                if (check.Result.FixAction != null && GUILayout.Button("Fix", GUILayout.Width(36)))
                    check.Result.FixAction();
                if (check.Result.OpenAction != null && GUILayout.Button(check.Result.OpenLabel ?? "Open", GUILayout.Width(80)))
                    check.Result.OpenAction();
                if (GUILayout.Button("↺", GUILayout.Width(22)))
                    check.RunAndStore();
            }

            EditorGUILayout.EndHorizontal();

            if (!deferred && check.Result.Details != null && check.Result.Details.Count > 0)
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
        public System.Action FixAction;
        public System.Action OpenAction;
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

    class DeferredCheck : PortingCheck
    {
        readonly string _name;
        readonly CheckCategory _cat;
        public DeferredCheck(string name, CheckCategory cat) { _name = name; _cat = cat; }
        public override string Name => _name;
        public override CheckCategory Category => _cat;
        protected override CheckResult Run() => new CheckResult { Status = CheckStatus.Skipped };
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
            System.Action openSettings = () => SettingsService.OpenProjectSettings("Project/Input System Package");

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

            var registry = Object.FindObjectsByType<ScenarioRegistry>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                               .FirstOrDefault();
            if (registry == null)
                return new CheckResult { Status = CheckStatus.Error, Summary = "No ScenarioRegistry found in VRTLoginManager scene" };

            var errors = new List<string>();
            var warnings = new List<string>();

            // a) User prefab variant check
            var source = PrefabUtility.GetCorrespondingObjectFromSource(registry.gameObject);
            if (source == null)
                warnings.Add("ScenarioRegistry is not a prefab instance — save it as a prefab variant in your own Assets folder");
            else if (AssetDatabase.GetAssetPath(source).Contains("/Samples/"))
                warnings.Add("ScenarioRegistry still uses the Samples original — create a prefab variant in your own Assets folder");

            // b) All registered scenes present in Build Settings
            var buildSceneNames = EditorBuildSettings.scenes
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .ToHashSet();
            foreach (var s in registry.Scenarios)
                if (!buildSceneNames.Contains(s.scenarioSceneName))
                    errors.Add($"Scene '{s.scenarioSceneName}' (scenario '{s.scenarioName}') is missing from Build Settings");

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
}
