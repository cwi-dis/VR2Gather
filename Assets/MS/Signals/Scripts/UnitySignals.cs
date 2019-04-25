using System.Collections;
using UnityEngine;
using System;

public class UnitySignals : MonoBehaviour {

    public string StreamName = "MyMediaPipeline";
    public string URL = "http://livesim.dashif.org/livesim/testpic_2s/Manifest.mpd";

    IntPtr handle;
    bool    isPlaying = false;


    private void Awake() {
        Application.logMessageReceived += HandleLog;
    }

    // Use this for initialization
    void Start () {
        signals_unity_bridge_pinvoke.SetPaths();

        handle = signals_unity_bridge_pinvoke.sub_create(StreamName);
        if (handle != IntPtr.Zero) {
            Debug.Log(">>> sub_create " + handle);
            isPlaying = signals_unity_bridge_pinvoke.sub_play(handle, URL);
            Debug.Log(">>> sub_play " + isPlaying);
            if (isPlaying) {
                int count = signals_unity_bridge_pinvoke.sub_get_stream_count(handle);
                Debug.Log(">>> sub_get_stream_count " + count);
            }else
                Debug.LogError("SUBD_ERROR: can't open URL " + URL );

        }else
            Debug.LogError("SUBD_ERROR: can't create streaming: " + StreamName);
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
