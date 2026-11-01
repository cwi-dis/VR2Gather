using System;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace VRT.Core
{
    /// <summary>
    /// What this application was built from, for the stats log, so analysis code can tell which
    /// log formats and workarounds apply to a run.
    ///
    /// Package versions are only available in the editor, so in a player they come from a
    /// resource that a build step writes.
    /// </summary>
    public static class VRTVersionInfo
    {
        private const string ResourceName = "VRTVersionInfo";
        private const string Unknown = "unknown";

        [Serializable]
        private class PackageVersions
        {
            public string vr2gather = Unknown;
            public string cwipc_unity = Unknown;
        }

        /// <summary>
        /// The name=value fields of the stats line that VRTConfig outputs at startup.
        /// </summary>
        public static string StatsFields()
        {
            PackageVersions packages = GetPackageVersions();
            return $"app={Application.productName}, app_version={Application.version}, " +
                $"vr2gather={packages.vr2gather}, cwipc_unity={packages.cwipc_unity}, " +
                $"unity={Application.unityVersion}, platform={Application.platform}";
        }

#if UNITY_EDITOR
        private static PackageVersions GetPackageVersions()
        {
            return new PackageVersions
            {
                vr2gather = PackageVersion("nl.cwi.dis.vr2gather"),
                cwipc_unity = PackageVersion("nl.cwi.dis.cwipc"),
            };
        }

        /// <summary>
        /// The version of a package, with what it was really made from, because the code can have
        /// changed since that version was released.
        /// </summary>
        private static string PackageVersion(string packageName)
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{packageName}/package.json");
            if (info == null) return Unknown;
            if (!string.IsNullOrEmpty(info.git?.hash))
            {
                return $"{info.version}+{info.git.hash.Substring(0, Math.Min(9, info.git.hash.Length))}";
            }
            bool isLocal = info.source == UnityEditor.PackageManager.PackageSource.Local
                || info.source == UnityEditor.PackageManager.PackageSource.Embedded;
            return isLocal ? $"{info.version}+local" : info.version;
        }

        /// <summary>
        /// Writes the package versions into the build as a resource, and removes that file again
        /// afterwards so it doesn't end up in the project. If a build fails the file stays behind,
        /// which is harmless: the next build overwrites it, and the editor doesn't read it.
        /// </summary>
        internal class BuildStep : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {
            private const string ResourcePath = "Assets/Resources/" + ResourceName + ".json";

            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ResourcePath));
                File.WriteAllText(ResourcePath, JsonUtility.ToJson(GetPackageVersions()));
                AssetDatabase.ImportAsset(ResourcePath);
            }

            public void OnPostprocessBuild(BuildReport report)
            {
                AssetDatabase.DeleteAsset(ResourcePath);
            }
        }
#else
        private static PackageVersions GetPackageVersions()
        {
            TextAsset resource = Resources.Load<TextAsset>(ResourceName);
            if (resource == null) return new PackageVersions();
            return JsonUtility.FromJson<PackageVersions>(resource.text);
        }
#endif
    }
}
