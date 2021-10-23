#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class CopyConfigOnBuild : IPostprocessBuildWithReport
{
    public int callbackOrder {  get { return 0; } }
    public void OnPostprocessBuild(BuildReport report)
    {
        string srcDir = Path.GetDirectoryName(Application.dataPath) + "/";
        string dstDir = Path.GetDirectoryName(report.summary.outputPath) + "/";
        Debug.Log($"CopyConfigOnBuild.OnPostProcessBuild src {srcDir} dst {dstDir}");
        if (File.Exists(srcDir + "config.json"))
        {
            File.Copy(srcDir + "config.json", dstDir + "config.json");
            Debug.Log($"CopyConfigOnBuild.OnPostProcessBuild copied config.json");
        }
        if (File.Exists(srcDir + "cameraconfig.xml"))
        {
            File.Copy(srcDir + "cameraconfig.xml", dstDir + "cameraconfig.xml");
            Debug.Log($"CopyConfigOnBuild.OnPostProcessBuild copied cameraconfig.xml");
        }


    }
}
#endif