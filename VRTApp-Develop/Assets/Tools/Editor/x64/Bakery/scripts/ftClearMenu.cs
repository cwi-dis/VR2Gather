using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class ftClearMenu
{
    [MenuItem("Bakery/Utilities/Clear baked data", false, 44)]
    private static void ClearBakedData()
    {
        if (EditorUtility.DisplayDialog("Bakery", "Clear all Bakery data for currently loaded scenes?", "OK", "Cancel"))
        {
            var sceneCount = SceneManager.sceneCount;
            for(int i=0; i<sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                var go = ftLightmaps.FindInScene("!ftraceLightmaps", scene);
                if (go == null) continue;
                Undo.DestroyObjectImmediate(go);
            }
            LightmapSettings.lightmaps = new LightmapData[0];
        }
    }
}

