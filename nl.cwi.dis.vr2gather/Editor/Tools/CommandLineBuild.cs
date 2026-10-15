using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

namespace VRT.Tools
{
    /// <summary>
    /// Build methods for the command line (scripts/build.sh, through `unity build --execute-method`).
    ///
    /// OpenXR doesn't support Linux players, and the XR settings for Standalone are shared by
    /// Windows, macOS and Linux. With OpenXR active, OpenXR's build validation stops a Linux build
    /// ("The only standalone targets supported are Windows x64 and OSX with OpenXR"). In Play mode
    /// in the Linux Editor the same check only logs an error and the app runs in desktop mode, so
    /// BuildLinux builds the player without the XR loaders, which gives that same behaviour, and
    /// puts the loaders back afterwards (also when the build fails).
    /// </summary>
    public static class CommandLineBuild
    {
        public static void BuildLinux()
        {
            string output = GetArgument("-buildOutput");
            if (string.IsNullOrEmpty(output))
                throw new ArgumentException("CommandLineBuild.BuildLinux: no -buildOutput argument");

            var manager = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone)?.AssignedSettings;
            var removedLoaders = new List<XRLoader>();
            BuildReport report;
            try
            {
                if (manager != null)
                {
                    removedLoaders.AddRange(manager.activeLoaders);
                    foreach (var loader in removedLoaders)
                    {
                        manager.TryRemoveLoader(loader);
                        Debug.Log($"CommandLineBuild: XR loader {loader.name} disabled for the Linux build");
                    }
                }
                var options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = output,
                    target = BuildTarget.StandaloneLinux64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None,
                };
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                for (int i = 0; i < removedLoaders.Count; i++)
                {
                    manager.TryAddLoader(removedLoaders[i], i);
                    Debug.Log($"CommandLineBuild: XR loader {removedLoaders[i].name} enabled again");
                }
                // In case the build saved the settings without the loaders: save them as they are now
                if (manager != null && removedLoaders.Count > 0)
                {
                    EditorUtility.SetDirty(manager);
                    AssetDatabase.SaveAssetIfDirty(manager);
                }
            }
            Debug.Log($"CommandLineBuild: build result {report.summary.result}, {report.summary.totalErrors} errors");
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"CommandLineBuild.BuildLinux: build {report.summary.result}");
        }

        static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
