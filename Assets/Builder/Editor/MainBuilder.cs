using UnityEditor;

public class MainBuilder {
    public static void Build(string menuItem, string[] levels ) {
        string[] options = menuItem.Split('/');
        BuildTarget buildTarget = BuildTarget.StandaloneWindows;
        switch (options[3].ToUpper()) {
            case "WINDOWS":
                buildTarget = BuildTarget.StandaloneWindows;
                break;
            case "OSX":
                buildTarget = BuildTarget.StandaloneOSX;
                break;
        }
        // Get filename.
        string path = EditorUtility.SaveFolderPanel($"Choose Location of {options[2]} - {options[3]} player", "", "");
        if (string.IsNullOrEmpty(path)) return;

        EditorUtility.DisplayProgressBar($"Building {options[2]} - {options[3]} Player", "Ironing shirts", 0);
        // Build player.
        BuildPipeline.BuildPlayer(levels, $"{path}/{options[2]}.exe", buildTarget, BuildOptions.None);
        EditorUtility.ClearProgressBar();
        // Copy a file from the project folder to the build folder, alongside the built game.
        FileUtil.DeleteFileOrDirectory(path + "/config.json");
        FileUtil.CopyFileOrDirectory("config.json", path + "/config.json");
        //        
    }
}
