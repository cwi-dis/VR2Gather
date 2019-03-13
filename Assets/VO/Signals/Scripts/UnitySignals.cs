using System.Collections;
using UnityEngine;
using System;

public class UnitySignals : MonoBehaviour {
    IntPtr  handle;
    bool    isPlaying = false;

    private void Awake() {
        Application.logMessageReceived += HandleLog;
    }

    // Use this for initialization
    void Start () {
        Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", Application.dataPath + (Application.isEditor ? "/VO/Signals/Plugins" : "/Plugins"));
        Environment.SetEnvironmentVariable("PATH", Application.dataPath + (Application.isEditor ? "/VO/Signals/Plugins" : "/Plugins"));
        handle = signals_unity_bridge_pinvoke.sub_create("MyMediaPipeline");
        if (handle != IntPtr.Zero)
        {
            Debug.Log(">>> sub_create " + handle);
            isPlaying = signals_unity_bridge_pinvoke.sub_play(handle, "http://livesim.dashif.org/livesim/testpic_2s/Manifest.mpd");
            Debug.Log(">>> sub_play " + isPlaying);
            if (isPlaying) {
                int count = signals_unity_bridge_pinvoke.sub_get_stream_count(handle);
                Debug.Log(">>> sub_get_stream_count " + count);
            }
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();

    }

    void OnDestroy() {
        signals_unity_bridge_pinvoke.sub_destroy(handle);
        Application.logMessageReceived -= HandleLog;
    }

    string myLog;
    Queue myLogQueue = new Queue();
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        myLog = logString;
        string newString = "\n [" + type + "] : " + myLog;
        myLogQueue.Enqueue(newString);
        if (type == LogType.Exception)
        {
            newString = "\n" + stackTrace;
            myLogQueue.Enqueue(newString);
        }
        myLog = string.Empty;
        foreach (string mylog in myLogQueue) {
            myLog += mylog;
        }
    }

    void OnGUI() {
        GUILayout.Label(myLog);
    }
}
