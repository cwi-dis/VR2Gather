using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.PointCloud;
using VRT.UserRepresentation.TVM.DataProviders;
using VRT.Core;

public class OrchestratorCalibration : MonoBehaviour {

    private static OrchestratorCalibration instance;

    public static OrchestratorCalibration Instance { get { return instance; } }

    #region GUI components

    [SerializeField] private PlayerManager player = null;
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

        if (OrchestratorController.Instance.SelfUser.userData.userRepresentationType == UserRepresentationType.__TVM__) {
            NetworkDataProvider tvm = (NetworkDataProvider)player.tvm;
            tvm.connectionURI = OrchestratorController.Instance.SelfUser.userData.userMQurl;
            tvm.exchangeName = OrchestratorController.Instance.SelfUser.userData.userMQexchangeName;
            tvm.gameObject.SetActive(true);
        }
        else {
            player.pc.gameObject.SetActive(true);
            player.pc.AddComponent<PointCloudPipeline>().Init(OrchestratorController.Instance.SelfUser, Config.Instance.LocalUser, true);
        }
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
