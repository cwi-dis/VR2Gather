using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrchestratorWrapping;
using UnityEngine.Video;
using System;

public enum Actions { VIDEO_1_START, VIDEO_1_PAUSE, VIDEO_2_START, VIDEO_2_PAUSE, WAIT }

abstract public class PilotController : MonoBehaviour {

    public static int my_ID = 0;
    public string masterID = null;

    public PlayerManager[] players;
    public VideoPlayer[] videos;

    [HideInInspector] public OrchestrationWindow orchestrator = null;
    [HideInInspector] public GameObject background = null;

    #region Sync
    public float timer = 0.0f;
    public float delay = 0.0f;
    public Actions todoAction = Actions.WAIT;
    #endregion

    #region Utils
    [HideInInspector] public Color playerCol = new Color(0.15f, 0.78f, 0.15f); // Green
    [HideInInspector] public Color otherCol = new Color(1.0f, 1.0f, 1.0f); // White
    [HideInInspector] public Color offlineCol = new Color(1.0f, 0.5f, 0.5f); // Red
    #endregion

    // Start is called before the first frame update
    public virtual void Start() {
        background = GameObject.Find("Background");
        orchestrator = GameObject.Find("ManagerTest").GetComponent<OrchestrationWindow>();
        var tmp = Config.Instance;
    }

    public virtual void Update() {
        //SyncTool.UpdateTimes();
    }

    public void SendPing() {
        string text = MessageType.PING + "_";
        text = text + my_ID + "_" + SyncTool.GetMyTimeString();
        orchestrator.SendPing(text, masterID);
        Debug.Log("PING: " + text + " // " + masterID);
    }

    public abstract void MessageActivation(string message);
}

