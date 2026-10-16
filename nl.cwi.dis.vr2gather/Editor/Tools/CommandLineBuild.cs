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
    /// BuildWithoutXR builds a desktop player without XR (desktop mode only), for the build target
    /// the Editor was started with (`unity build --target`). The XR settings for Standalone are
    /// shared by Windows, macOS and Linux, so it disables the Standalone XR loaders during the
    /// build and puts them back afterwards, also when the build fails.
    ///
    /// Linux needs this: OpenXR doesn't support Linux players, and with OpenXR active its build
    /// validation refuses a Linux build ("The only standalone targets supported are Windows x64
    /// and OSX with OpenXR"). See issue #366.
    /// </summary>
    public static class CommandLineBuild
    {
        public static void BuildWithoutXR()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (group != BuildTargetGroup.Standalone)
                throw new ArgumentException($"CommandLineBuild.BuildWithoutXR: {target} is not a desktop (Standalone) target");
            string output = GetArgument("-buildOutput");
            if (string.IsNullOrEmpty(output))
                throw new ArgumentException("CommandLineBuild.BuildWithoutXR: no -buildOutput argument");

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
                        Debug.Log($"CommandLineBuild: XR loader {loader.name} disabled for this {target} build");
                    }
                }
                var options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = output,
                    target = target,
                    targetGroup = group,
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
                throw new Exception($"CommandLineBuild.BuildWithoutXR: build {report.summary.result}");
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
