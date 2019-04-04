using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

public class CWIBuilder
{
    [MenuItem("VRTogether/CWI/Pointclouds/Windows")]
    public static void BuildGameWindowsPointClouds()
    {
        BuildGame(BuildTarget.StandaloneWindows);
    }

    [MenuItem("VRTogether/CWI/Pointclouds/OSx")]
    public static void BuildGameOSxPointClouds()
    {
        BuildGame(BuildTarget.StandaloneOSX);
    }

    public static void BuildGame(BuildTarget buildTarget) {
        // Get filename.
        string path = EditorUtility.SaveFolderPanel("Choose Location of Built PointClouds", "", "");
        if (string.IsNullOrEmpty(path)) return;

        string[] levels = new string[] { "Assets/CWI/Scenes/PointClouds.unity" };


        EditorUtility.DisplayProgressBar("Building Player", "Ironing shirts", 0);
        // Build player.
        BuildPipeline.BuildPlayer(levels, $"{path}/PointClouds.exe", buildTarget, BuildOptions.None);
        EditorUtility.ClearProgressBar();
        // Copy a file from the project folder to the build folder, alongside the built game.
        FileUtil.CopyFileOrDirectory("config.json", path + "/config.json");
        //        
    }
}
