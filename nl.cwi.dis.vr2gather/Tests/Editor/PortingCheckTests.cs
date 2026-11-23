using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VRT.Tools;

namespace VRT.Tests.Editor
{
    // The Porting Check (Tools → VR2Gather Porting Check) must be clean on the project's own setup
    // and scenes (#416): every check OK or skipped, or a warning listed in KnownWarnings. One test
    // per global or script check, and one per enabled scene in Build Settings for the scene checks.
    // In the development projects those are the package's sample scenes.
    //
    // The tests open scenes; the scenes that were open before are opened again afterwards. They
    // refuse to run while an open scene has unsaved changes.
    public class PortingCheckTests
    {
        // Warnings that are accepted, with the reason: "<check name>" for a global or script check,
        // "<scene name>: <check name>" for a scene check.
        static readonly Dictionary<string, string> KnownWarnings = new Dictionary<string, string>
        {
        };

        const string LoginScene = "VRTLoginManager";

        SceneSetup[] _savedSetup;

        [OneTimeSetUp]
        public void SaveOpenScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    Assert.Ignore($"Scene '{SceneManager.GetSceneAt(i).name}' has unsaved changes: save or discard them, then run these tests again");
            _savedSetup = EditorSceneManager.GetSceneManagerSetup();
        }

        [OneTimeTearDown]
        public void RestoreOpenScenes()
        {
            if (_savedSetup != null && _savedSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(_savedSetup);
        }

        static IEnumerable<string> ProjectCheckNames() =>
            PortingChecks.CreateAll().Where(c => c.Category != CheckCategory.Scene).Select(c => c.Name);

        static IEnumerable<string> EnabledScenePaths() =>
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path);

        [TestCaseSource(nameof(ProjectCheckNames))]
        public void ProjectCheck(string checkName)
        {
            var check = PortingChecks.CreateAll().Single(c => c.Name == checkName);
            // The Scenario Registry check needs the login scene open.
            if (check is ScenarioRegistryCheck)
                OpenScene(FindLoginScene());
            check.RunAndStore();
            AssertClean(new[] { check }, "");
        }

        [TestCaseSource(nameof(EnabledScenePaths))]
        public void SceneChecks(string scenePath)
        {
            OpenScene(scenePath);
            var checks = PortingChecks.CreateAll().OfType<SceneCheck>().ToList();
            foreach (var check in checks)
            {
                check.TargetScenePath = scenePath;
                check.RunAndStore();
            }
            AssertClean(checks, Path.GetFileNameWithoutExtension(scenePath) + ": ");
        }

        static void OpenScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Assert.IsTrue(scene.isLoaded, $"Could not open {path}");
        }

        static string FindLoginScene()
        {
            var guids = AssetDatabase.FindAssets($"t:Scene {LoginScene}");
            Assert.IsNotEmpty(guids, $"{LoginScene} scene not found");
            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        // Fails with every problem listed; reports skipped checks and accepted warnings in the test output.
        static void AssertClean(IEnumerable<PortingCheck> checks, string prefix)
        {
            var problems = new List<string>();
            foreach (var check in checks)
            {
                var result = check.Result;
                string key = prefix + check.Name;
                string line = $"{key}: {result.Status}: {result.Summary}";
                if (result.Details != null)
                    line += string.Concat(result.Details.Select(d => "\n    " + d));
                switch (result.Status)
                {
                    case CheckStatus.OK:
                        break;
                    case CheckStatus.Skipped:
                        TestContext.WriteLine(line);
                        break;
                    case CheckStatus.Warning when KnownWarnings.TryGetValue(key, out var reason):
                        TestContext.WriteLine($"{line}\n    (known warning: {reason})");
                        break;
                    default:
                        problems.Add(line);
                        break;
                }
            }
            if (problems.Count > 0)
                Assert.Fail(string.Join("\n", problems));
        }
    }
}
