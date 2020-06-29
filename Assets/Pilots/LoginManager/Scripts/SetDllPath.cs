using UnityEngine;

public class SetDllPath {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoadRuntimeMethod() {
        string pathvar = System.Environment.GetEnvironmentVariable("PATH");
        //string dllsPath = $"{Application.dataPath}/../dlls/";
        string dllsPath = $"{System.IO.Directory.GetParent(Application.dataPath).ToString()}/dll/";
        if (System.IO.Directory.Exists(dllsPath)) { 
            System.Environment.SetEnvironmentVariable("PATH", $"{dllsPath};{pathvar}", System.EnvironmentVariableTarget.Process);
            System.Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", dllsPath);
        }
    }
}
