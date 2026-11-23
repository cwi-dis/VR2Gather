using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VRT.Tools;

namespace VRT.Tests.Editor
{
    // The package's shaders and materials work in this project's render pipeline (#416). Player
    // builds don't catch either problem: a compile error in a SubShader for the other pipeline
    // (#365, cwi-dis/cwipc_unity#55), or a material that renders pink. Run in both development
    // projects, BIRP and URP, to cover both pipelines.
    public class ShaderTests
    {
        const string PackageRoot = "Packages/nl.cwi.dis.vr2gather";
        // The development projects have the package's Samples~/VRTAssets as Assets/VRTAssets (a symlink).
        const string SamplesRoot = "Assets/VRTAssets";

        static string[] SearchRoots() =>
            AssetDatabase.IsValidFolder(SamplesRoot) ? new[] { PackageRoot, SamplesRoot } : new[] { PackageRoot };

        static IEnumerable<string> AssetPaths(string filter) =>
            AssetDatabase.FindAssets(filter, SearchRoots()).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p);

        static string PipelineName =>
            GraphicsSettings.currentRenderPipeline == null ? "Built-in Render Pipeline" : GraphicsSettings.currentRenderPipeline.GetType().Name;

        [Test]
        public void PackageShadersHaveNoErrors()
        {
            var problems = new List<string>();
            int count = 0;
            foreach (string path in AssetPaths("t:Shader"))
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null) continue;
                count++;
                if (!ShaderUtil.ShaderHasError(shader)) continue;
                var errors = ShaderUtil.GetShaderMessages(shader)
                    .Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    .Select(m => $"    {m.message} (line {m.line}, {m.platform})");
                problems.Add($"{path} ({shader.name}):\n" + string.Join("\n", errors));
            }
            Assert.Greater(count, 0, "No shaders found in the package");
            TestContext.WriteLine($"{count} shader(s) checked with {PipelineName}");
            if (problems.Count > 0)
                Assert.Fail($"{problems.Count} shader(s) with errors with {PipelineName}:\n" + string.Join("\n", problems));
        }

        [Test]
        public void PackageMaterialsRenderInThisPipeline()
        {
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            var problems = new List<string>();
            int count = 0;
            foreach (string path in AssetPaths("t:Material"))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                count++;
                var shader = mat.shader;
                if (shader == null || shader.name == "Hidden/InternalErrorShader")
                    problems.Add($"{path}: shader missing (from a package this project doesn't have?)");
                else if (!shader.isSupported)
                    problems.Add($"{path}: shader {shader.name} not supported");
                else if (srp && RenderPipelineMaterialsCheck.IsBirpOnly(shader))
                    problems.Add($"{path}: shader {shader.name} is BIRP-only, renders pink");
            }
            Assert.Greater(count, 0, "No materials found in the package");
            TestContext.WriteLine($"{count} material(s) checked with {PipelineName}");
            if (problems.Count > 0)
                Assert.Fail($"{problems.Count} material(s) that don't render with {PipelineName}:\n" + string.Join("\n", problems));
        }
    }
}
