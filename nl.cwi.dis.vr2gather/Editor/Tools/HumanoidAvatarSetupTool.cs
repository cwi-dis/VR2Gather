using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRT.Pilots.Common;

namespace VRT.Tools
{
    /// <summary>
    /// Menu item for HumanoidAvatarSetup: select the wrapper (an empty GameObject with the
    /// Humanoid model as its child) and run Tools > VR2Gather Setup Humanoid Avatar.
    /// </summary>
    public static class HumanoidAvatarSetupTool
    {
        const string MenuName = "Tools/VR2Gather Setup Humanoid Avatar";

        [MenuItem(MenuName)]
        static void SetupSelected()
        {
            var wrapper = Selection.activeGameObject;
            Undo.RegisterFullObjectHierarchyUndo(wrapper, "Setup Humanoid Avatar");
            string error = HumanoidAvatarSetup.Setup(wrapper);
            if (error != null)
            {
                Debug.LogError($"HumanoidAvatarSetup: {error}");
                EditorUtility.DisplayDialog("Setup Humanoid Avatar", error, "OK");
                return;
            }
            EditorSceneManager.MarkSceneDirty(wrapper.scene);
            Debug.Log($"HumanoidAvatarSetup: '{wrapper.name}' is set up. Save it as a prefab and assign it as altRepOne/altRepTwo of a player. " +
                      "In a self player, also set SizeAdjust's HMD tracking action and tick setHeightOnHMDTracking, as P_Self_Player does for P_Mannequin.");
        }

        [MenuItem(MenuName, validate = true)]
        static bool SetupSelectedValidate() => Selection.activeGameObject != null;
    }
}
