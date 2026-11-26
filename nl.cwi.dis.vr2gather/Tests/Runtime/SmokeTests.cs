using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRT.Core;
using VRT.Login;

namespace VRT.Tests.Runtime
{
    // Smoke test (#416): from the login scene, AutoStart creates a standalone session with Pilot 0,
    // autoLeaveAfter leaves it after a few seconds, and the app is back in the login scene. Fails
    // on any error, on any warning not in KnownWarnings, and when an expected component wrote no
    // stats record. Once per user representation.
    //
    // The configuration is the project's config.json with the test's settings on top (no VR,
    // AutoStart, stats to the log every second), written to a temporary folder together with a
    // config-user.json, so the developer's own config-user.json isn't used. The point cloud is
    // cwipc's synthetic one, so this needs cwipc installed. The microphone is "Muted" until there
    // is a synthetic microphone.
    public class SmokeTests
    {
        const string LoginScene = "VRTLoginManager";
        const string ScenarioName = "Pilot 0";
        const string ScenarioScene = "Pilot0";
        const float SessionSeconds = 5;
        const float Timeout = 60;

        // Warnings that are accepted: a part of the message, and the reason.
        static readonly Dictionary<string, string> KnownWarnings = new Dictionary<string, string>
        {
        };

        // Components that must write at least one stats record in every session: the start of the
        // component name (before any "#"). VRTConfig and VRTInitializer write theirs once per run,
        // so they aren't here. A standalone session has nobody to send to: no encoders or writers.
        static readonly string[] SessionStats = { "OrchestratorController", "PilotController", "SessionPlayerManager" };

        // And per representation: the self player's pipeline.
        static readonly Dictionary<UserRepresentationType, string[]> ExpectedStats = new Dictionary<UserRepresentationType, string[]>
        {
            { UserRepresentationType.PointCloud, new[] { "PointCloudPipelineSelf", "AsyncSyntheticReader", "AsyncPointCloudPreparer", "PointCloudRenderer" } },
            { UserRepresentationType.SimpleAvatar, new string[0] },
        };

        static IEnumerable<UserRepresentationType> Representations() => ExpectedStats.Keys;

        readonly object _lock = new object();
        List<string> _problems;
        List<string> _stats;

        [SetUp]
        public void StartCollecting()
        {
            _problems = new List<string>();
            _stats = new List<string>();
            // Errors are collected and reported below, together with the warnings.
            LogAssert.ignoreFailingMessages = true;
            Application.logMessageReceivedThreaded += OnLog;
        }

        [TearDown]
        public void StopCollecting()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            LogAssert.ignoreFailingMessages = false;
        }

        [OneTimeTearDown]
        public void RestoreConfig()
        {
            VRTConfig.SetConfigFileForTests(null);
        }

        void OnLog(string message, string stackTrace, LogType type)
        {
            lock (_lock)
            {
                if (message.StartsWith("stats: ")) _stats.Add(message);
                if (type == LogType.Warning)
                {
                    if (!KnownWarnings.Keys.Any(k => message.Contains(k))) _problems.Add("Warning: " + message);
                }
                else if (type != LogType.Log)
                {
                    _problems.Add($"{type}: {message}");
                }
            }
        }

        [UnityTest]
        public IEnumerator LoginSessionLeave([ValueSource(nameof(Representations))] UserRepresentationType representation)
        {
            Configure(representation);
            SceneManager.LoadScene(LoginScene);
            yield return WaitForScene(ScenarioScene, "the session to start (is Caps Lock on? It cancels AutoStart)");
            yield return WaitForScene(LoginScene, "the session to end");
            // Let leaving the session finish.
            yield return new WaitForSeconds(1);
            // A Statistics object whose component never called Final() warns from its finalizer
            // (cwi-dis/cwipc_unity#89): make those run now, so the warning is counted below (#451).
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            List<string> problems, stats;
            lock (_lock)
            {
                problems = _problems.ToList();
                stats = _stats.ToList();
            }

            var components = stats
                .Select(s => Regex.Match(s, @"component=([^,#]+)"))
                .Where(m => m.Success)
                .GroupBy(m => m.Groups[1].Value)
                .ToDictionary(g => g.Key, g => g.Count());
            TestContext.WriteLine($"Stats records per component ({stats.Count} in all):");
            foreach (var kv in components.OrderBy(kv => kv.Key))
                TestContext.WriteLine($"    {kv.Key}: {kv.Value}");

            foreach (string expected in SessionStats.Concat(ExpectedStats[representation]))
                if (!components.Keys.Any(c => c.StartsWith(expected)))
                    problems.Add($"No stats record from {expected}");

            if (problems.Count > 0)
                Assert.Fail($"{problems.Count} problem(s):\n" + string.Join("\n", problems));
        }

        IEnumerator WaitForScene(string sceneName, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (SceneManager.GetActiveScene().name != sceneName)
            {
                if (Time.realtimeSinceStartup - start > Timeout)
                    Assert.Fail($"Timed out after {Timeout} s waiting for {what}: the active scene is {SceneManager.GetActiveScene().name}");
                yield return null;
            }
        }

        // The first time: the config files for VRTConfig to read. After that VRTConfig already
        // exists (it lives across scenes), so the representation is set on it directly.
        static void Configure(UserRepresentationType representation)
        {
            if (VRTConfig.InstanceExists())
            {
                SetRepresentation(VRTConfig.Instance.RepresentationConfig, representation);
            }
            else
            {
                string projectConfig = Path.Combine(Path.GetDirectoryName(Application.dataPath), "config.json");
                if (!File.Exists(projectConfig)) Assert.Ignore($"No config.json in the project ({projectConfig})");

                // Inactive, so its Awake() doesn't run: this one only converts JSON.
                var go = new GameObject("SmokeTests config");
                go.SetActive(false);
                try
                {
                    var config = go.AddComponent<VRTConfig>();
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(projectConfig), config);
                    config.disableVR = true;
                    config.autoInventUsername = false;
                    config.userConfigFilename = "config-user.json";
                    config.StatisticsConfig.interval = 1;
                    config.StatisticsConfig.outputFile = "";
                    config.AutoStartConfig = new VRTConfig.AutoStartConfigType
                    {
                        autoCreateStandalone = true,
                        sessionName = "smoketest",
                        sessionScenario = ScenarioName,
                        autoLeaveAfter = SessionSeconds,
                    };
                    SetRepresentation(config.RepresentationConfig, representation);

                    string folder = Path.Combine(Application.temporaryCachePath, "SmokeTests");
                    Directory.CreateDirectory(folder);
                    string configPath = Path.Combine(folder, "config.json");
                    File.WriteAllText(configPath, JsonUtility.ToJson(config, true));
                    File.WriteAllText(Path.Combine(folder, "config-user.json"), JsonUtility.ToJson(config.RepresentationConfig, true));
                    VRTConfig.SetConfigFileForTests(configPath);
                }
                finally
                {
                    Object.Destroy(go);
                }
            }
            OrchestratorLogin.ResetAutoStartStatics();
        }

        static void SetRepresentation(VRTConfig.RepresentationConfigType rc, UserRepresentationType representation)
        {
            rc.userName = "smoketest";
            rc.representation = representation;
            rc.representation_str = representation.ToString();
            rc.microphoneName = "Muted";
            rc.webcamName = "";
            rc.RepresentationPointcloudConfig.variant = RepresentationPointcloudVariant.synthetic;
            rc.RepresentationPointcloudConfig.variant_str = RepresentationPointcloudVariant.synthetic.ToString();
        }
    }
}
