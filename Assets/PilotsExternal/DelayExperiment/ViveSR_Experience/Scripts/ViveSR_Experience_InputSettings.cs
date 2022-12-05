#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Valve.VR;

namespace Vive.Plugin.SR
{
    [InitializeOnLoad]
    public class ViveSR_Experience_InputSettings : EditorWindow
    {
        static ViveSR_Experience_InputSettings window;
        static List<Page> SettingPages = new List<Page>();
        static int CountPage;
        static int CurrentPage;

        static ViveSR_Experience_InputSettings()
        {
            EditorApplication.update += Init;
        }

        [MenuItem("SRWorks/Controller Input Settings")]
        static void Window()
        {
            EditorApplication.update += Init;
        }

        static void Init()
        {
            CurrentPage = CountPage = 0;
            SettingPages.Clear();
            Page[] pages = new Page[]
            {
                (ViveSR_Experience_Page_VRInput)CreateInstance(typeof(ViveSR_Experience_Page_VRInput)),
                (ViveSR_Experience_Page_Finish)CreateInstance(typeof(ViveSR_Experience_Page_Finish)),
            };
            
            foreach(Page page in pages)
            {
                if (page.IsNeedShow())
                    SettingPages.Add(page);
            }
            CountPage = SettingPages.Count;
            
            if (CountPage > 1)
            {
                window = GetWindow<ViveSR_Experience_InputSettings>(true, "Input Settings", true); // Show window
                window.minSize = new Vector2(300, 400);
            }
            EditorApplication.update -= Init;
        }

        public void OnGUI()
        {
            GUI.skin.label.wordWrap = true;

            GUILayout.Space(5);

            for (int p = CurrentPage; p < CountPage; p++)
            {
                if (SettingPages[p].IsNeedShow())
                {
                    SettingPages[p].RenderGUI();
                    break;
                }
                else
                    ++CurrentPage;
            }
            if (CurrentPage == CountPage) Close();
        }

        public abstract class Page : EditorWindow
        {
            public abstract string Name { get; }
            public abstract bool IsNeedShow();
            public abstract void RenderGUI();
        }
    }

    public class ViveSR_Experience_Page_VRInput : ViveSR_Experience_InputSettings.Page
    {
        public override string Name { get { return "SteamVR_Input"; } }
        const string HelpboxText_RemindEnableViveSRInput = "Enable SteamVR_Input of ViveSR Experience?";
        string steamVRStreamingFileDirectoryPath = @"Assets\StreamingAssets\SteamVR\";

        string[] FileNames = { "actions.json", "bindings_vive_controller.json", "bindings_vive_type2_controller.json" };

        bool declined = false;

        public override bool IsNeedShow()
        {
            if (declined) return false;
            return IsNeedSteamVRInputSupport();
        }

        public override void RenderGUI()
        {
            EditorGUILayout.HelpBox(HelpboxText_RemindEnableViveSRInput, MessageType.Warning);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Accept"))
            {
                EnableSteamVRInputSupport();
            }
            if (GUILayout.Button("Decline")) declined = true;
            GUILayout.EndHorizontal();
        }

        private bool IsNeedSteamVRInputSupport()
        {
            foreach (string str in FileNames)
            {
                if (File.Exists(steamVRStreamingFileDirectoryPath + str)) return false;
            }

            return true;
        }

        private void EnableSteamVRInputSupport()
        {
            if (IsNeedSteamVRInputSupport())
            {
                string inputFileDirectoryPath = @"Assets\ViveSR_Experience\Input\";

                if (!Directory.Exists(steamVRStreamingFileDirectoryPath)) Directory.CreateDirectory(steamVRStreamingFileDirectoryPath);

                foreach (string str in FileNames)
                {
                    if (File.Exists(inputFileDirectoryPath + str))
                    {
                        if (File.Exists(steamVRStreamingFileDirectoryPath + str)) FileUtil.DeleteFileOrDirectory(steamVRStreamingFileDirectoryPath + str);
                        FileUtil.CopyFileOrDirectory(inputFileDirectoryPath + str, steamVRStreamingFileDirectoryPath + str);
                    }
                    else Debug.Log("'" + inputFileDirectoryPath + str + "' not found.");
                }

                SteamVR_Input_Generator.BeginGeneration();
            }
        }
    }

    public class ViveSR_Experience_Page_Finish : ViveSR_Experience_InputSettings.Page
    {
        public override string Name { get { return "Finish"; } }
        private bool click = false;

        public override bool IsNeedShow()
        {
            return !click;
        }

        public override void RenderGUI()
        {
            GUILayout.Label("Done!");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close")) click = true;
        }

    }
}
#endif