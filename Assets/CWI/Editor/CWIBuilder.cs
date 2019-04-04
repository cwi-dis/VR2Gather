using UnityEditor;
using System.Reflection;

public class CWIBuilder {

    static string[] levels = new string[] { "Assets/CWI/Scenes/PointClouds.unity" };

    [MenuItem("VRTogether/CWI/Point Clouds/Windows")]
    public static void BuildGameWindowsPointClouds() {
        MainBuilder.Build(((MenuItem)MethodBase.GetCurrentMethod().GetCustomAttributes(typeof(MenuItem), true)[0]).menuItem, levels);
    }

    [MenuItem("VRTogether/CWI/Point Clouds/OSX")]
    public static void BuildGameOSxPointClouds() {
        MainBuilder.Build( ((MenuItem)MethodBase.GetCurrentMethod().GetCustomAttributes(typeof(MenuItem), true)[0]).menuItem, levels);
    }
}
