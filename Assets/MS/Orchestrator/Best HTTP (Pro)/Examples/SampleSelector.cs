using System;
using System.Collections.Generic;

using UnityEngine;
using BestHTTP;
using BestHTTP.Statistics;
using BestHTTP.Examples;

namespace BestHTTP.Examples
{
    /// <summary>
    /// A class to describe an Example and store it's metadata.
    /// </summary>
    public sealed class SampleDescriptor
    {
        public bool IsLabel { get; set; }
        public Type Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }

        public bool IsSelected { get; set; }
        public GameObject UnityObject { get; set; }
        public bool IsRunning { get { return UnityObject != null; } }

        public SampleDescriptor(Type type, string displayName, string description)
        {
            this.Type = type;
            this.DisplayName = displayName;
            this.Description = description;
        }

        public void CreateUnityObject()
        {
            if (UnityObject != null)
                return;

            UnityObject = new GameObject(DisplayName);
            UnityObject.AddComponent(Type);
        }

        public void DestroyUnityObject()
        {
            if (UnityObject != null)
            {
                UnityEngine.Object.Destroy(UnityObject);
                UnityObject = null;
            }
        }
    }

    public class SampleSelector : MonoBehaviour
    {
        public const int statisticsHeight = 160;

        List<SampleDescriptor> Samples = new List<SampleDescriptor>();
        public static SampleDescriptor SelectedSample;

        Vector2 scrollPos;

        void Awake()
        {
            Application.runInBackground = true;
            HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.All;

#if UNITY_SAMSUNGTV
        SamsungTV.touchPadMode = SamsungTV.TouchPadMode.Mouse;

        // Create a red 'cursor' to see where we are pointing to
        Texture2D tex = new Texture2D(8, 8, TextureFormat.RGB24, false);
        for (int i = 0; i < tex.width; ++i)
            for (int cv = 0; cv < tex.height; ++cv)
                tex.SetPixel(i, cv, Color.red);
        tex.Apply(false, true);
        Cursor.SetCursor(tex, Vector2.zero, CursorMode.Auto);
#endif


#if !BESTHTTP_DISABLE_SOCKETIO
            Samples.Add(new SampleDescriptor(null, "Socket.IO Samples", string.Empty) { IsLabel = true });
            Samples.Add(new SampleDescriptor(typeof(SocketIOChatSample), "Chat", "This example uses the Socket.IO implementation to connect to the official Chat demo server(http://chat.socket.io/).\n\nFeatures demoed in this example:\n-Instantiating and setting up a SocketManager to connect to a Socket.IO server\n-Changing SocketOptions property\n-Subscribing to Socket.IO events\n-Sending custom events to the server"));
            Samples.Add(new SampleDescriptor(typeof(SocketIOOrchestratorSample), "Orchestrator test", "For orchestrator tests"));
#endif

            SelectedSample = Samples[1];
        }

        void Update()
        {
            GUIHelper.ClientArea = new Rect(0, SampleSelector.statisticsHeight + 5, Screen.width, Screen.height - SampleSelector.statisticsHeight - 50);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (SelectedSample != null && SelectedSample.IsRunning)
                    SelectedSample.DestroyUnityObject();
                else
                    Application.Quit();
            }

            if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
            {
                if (SelectedSample != null && !SelectedSample.IsRunning)
                    SelectedSample.CreateUnityObject();
            }
        }

        void OnGUI()
        {
            var stats = HTTPManager.GetGeneralStatistics(StatisticsQueryFlags.All);

            // Connection statistics
            GUIHelper.DrawArea(new Rect(0, 0, Screen.width / 3, statisticsHeight), false, () =>
                {
                // Header
                GUIHelper.DrawCenteredText("Connections");

                    GUILayout.Space(5);

                    GUIHelper.DrawRow("Sum:", stats.Connections.ToString());
                    GUIHelper.DrawRow("Active:", stats.ActiveConnections.ToString());
                    GUIHelper.DrawRow("Free:", stats.FreeConnections.ToString());
                    GUIHelper.DrawRow("Recycled:", stats.RecycledConnections.ToString());
                    GUIHelper.DrawRow("Requests in queue:", stats.RequestsInQueue.ToString());
                });

            // Cache statistics
            GUIHelper.DrawArea(new Rect(Screen.width / 3, 0, Screen.width / 3, statisticsHeight), false, () =>
                {
                    GUIHelper.DrawCenteredText("Cache");

#if !BESTHTTP_DISABLE_CACHING
                if (!BestHTTP.Caching.HTTPCacheService.IsSupported)
                    {
#endif
                    GUI.color = Color.yellow;
                        GUIHelper.DrawCenteredText("Disabled in WebPlayer, WebGL & Samsung Smart TV Builds!");
                        GUI.color = Color.white;
#if !BESTHTTP_DISABLE_CACHING
                }
                    else
                    {
                        GUILayout.Space(5);

                        GUIHelper.DrawRow("Cached entities:", stats.CacheEntityCount.ToString());
                        GUIHelper.DrawRow("Sum Size (bytes): ", stats.CacheSize.ToString("N0"));

                        GUILayout.BeginVertical();

                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Clear Cache"))
                            BestHTTP.Caching.HTTPCacheService.BeginClear();

                        GUILayout.EndVertical();
                    }
#endif
            });

            // Cookie statistics
            GUIHelper.DrawArea(new Rect((Screen.width / 3) * 2, 0, Screen.width / 3, statisticsHeight), false, () =>
                {
                    GUIHelper.DrawCenteredText("Cookies");

#if !BESTHTTP_DISABLE_COOKIES
                if (!BestHTTP.Cookies.CookieJar.IsSavingSupported)
                {
#endif
                    GUI.color = Color.yellow;
                    GUIHelper.DrawCenteredText("Saving and loading from disk is disabled in WebPlayer, WebGL & Samsung Smart TV Builds!");
                    GUI.color = Color.white;
#if !BESTHTTP_DISABLE_COOKIES
                }
                else
                {
                    GUILayout.Space(5);

                    GUIHelper.DrawRow("Cookies:", stats.CookieCount.ToString());
                    GUIHelper.DrawRow("Estimated size (bytes):", stats.CookieJarSize.ToString("N0"));

                    GUILayout.BeginVertical();

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Clear Cookies"))
                        BestHTTP.Cookies.CookieJar.Clear();

                    GUILayout.EndVertical();
                }
#endif
            });

            if (SelectedSample == null || (SelectedSample != null && !SelectedSample.IsRunning))
            {
                // Draw the list of samples
                GUIHelper.DrawArea(new Rect(0, statisticsHeight + 5, SelectedSample == null ? Screen.width : Screen.width / 3, Screen.height - statisticsHeight - 5), false, () =>
                    {
                        scrollPos = GUILayout.BeginScrollView(scrollPos);
                        for (int i = 0; i < Samples.Count; ++i)
                            DrawSample(Samples[i]);
                        GUILayout.EndScrollView();
                    });

                if (SelectedSample != null)
                    DrawSampleDetails(SelectedSample);
            }
            else if (SelectedSample != null && SelectedSample.IsRunning)
            {
                GUILayout.BeginArea(new Rect(0, Screen.height - 50, Screen.width, 50), string.Empty);
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Back", GUILayout.MinWidth(100)))
                    SelectedSample.DestroyUnityObject();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        private void DrawSample(SampleDescriptor sample)
        {
            if (sample.IsLabel)
            {
                GUILayout.Space(15);
                GUIHelper.DrawCenteredText(sample.DisplayName);
                GUILayout.Space(5);
            }
            else if (GUILayout.Button(sample.DisplayName))
            {
                sample.IsSelected = true;

                if (SelectedSample != null)
                    SelectedSample.IsSelected = false;

                SelectedSample = sample;
            }
        }

        private void DrawSampleDetails(SampleDescriptor sample)
        {
            Rect area = new Rect(Screen.width / 3, statisticsHeight + 5, (Screen.width / 3) * 2, Screen.height - statisticsHeight - 5);
            GUI.Box(area, string.Empty);

            GUILayout.BeginArea(area);
            GUILayout.BeginVertical();
            GUIHelper.DrawCenteredText(sample.DisplayName);
            GUILayout.Space(5);
            GUILayout.Label(sample.Description);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Start Sample"))
                sample.CreateUnityObject();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}