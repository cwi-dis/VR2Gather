using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OrchestratorWrapping;

public class OrchestratorCalibration : MonoBehaviour {

    private static OrchestratorCalibration instance;

    public static OrchestratorCalibration Instance { get { return instance; } }

    #region GUI components

    [SerializeField] private Button exitButton = null;

    #endregion

    #region Unity

    // Start is called before the first frame update
    void Start() {
        if (instance == null) {
            instance = this;
        }
        // Buttons listeners
        exitButton.onClick.AddListener(delegate { LeaveButton(); });

        InitialiseControllerEvents();
    }

    private void OnDestroy() {
        TerminateControllerEvents();
    }

    #endregion

    #region Buttons

    public void LeaveButton() {
        SceneManager.LoadScene("LoginManager");
    }

    #endregion

    #region Events listeners

    // Subscribe to Orchestrator Wrapper Events
    private void InitialiseControllerEvents() {
    }

    // Un-Subscribe to Orchestrator Wrapper Events
    private void TerminateControllerEvents() {
    }

    #endregion

    #region Commands



    #endregion

#if UNITY_STANDALONE_WIN
    void OnGUI() {
        if (GUI.Button(new Rect(Screen.width / 2, 5, 70, 20), "Open Log")) {
            var log_path = System.IO.Path.Combine(System.IO.Directory.GetParent(Environment.GetEnvironmentVariable("AppData")).ToString(), "LocalLow", Application.companyName, Application.productName, "Player.log");
            Debug.Log(log_path);
            Application.OpenURL(log_path);
        }
    }
#endif
}
