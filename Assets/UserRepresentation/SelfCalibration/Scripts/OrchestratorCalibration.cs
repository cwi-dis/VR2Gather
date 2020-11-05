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

        if (OrchestratorController.Instance.SelfUser.userData.userRepresentationType == UserData.eUserRepresentationType.__TVM__) {
            player.tvm.transform.localPosition = new Vector3(PlayerPrefs.GetFloat("tvm_pos_x", 0), PlayerPrefs.GetFloat("tvm_pos_y", 0), PlayerPrefs.GetFloat("tvm_pos_z", 0));
            player.tvm.transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat("tvm_rot_x", 0), PlayerPrefs.GetFloat("tvm_rot_y", 0), PlayerPrefs.GetFloat("tvm_rot_z", 0));
            player.tvm.connectionURI = OrchestratorController.Instance.SelfUser.userData.userMQurl;
            player.tvm.exchangeName = OrchestratorController.Instance.SelfUser.userData.userMQexchangeName;
            player.tvm.gameObject.SetActive(true);
        }
        else if (OrchestratorController.Instance.SelfUser.userData.userRepresentationType == UserData.eUserRepresentationType.__PCC_CWI_)
        {
            player.pc.gameObject.SetActive(true);
        }
        else if (OrchestratorController.Instance.SelfUser.userData.userRepresentationType == UserData.eUserRepresentationType.__PCC_CWIK4A_)
        {
            player.pc.gameObject.SetActive(true);
        }
        else if (OrchestratorController.Instance.SelfUser.userData.userRepresentationType == UserData.eUserRepresentationType.__PCC_PROXY__)
        {
            player.pc.gameObject.SetActive(true);
        }
        else if (OrchestratorController.Instance.SelfUser.userData.userRepresentationType == UserData.eUserRepresentationType.__PCC_SYNTH__)
        {
            player.pc.gameObject.SetActive(true);
        }
        else if (OrchestratorController.Instance.SelfUser.userData.userRepresentationType == UserData.eUserRepresentationType.__PCC_PRERECORDED__)
        {
            player.pc.gameObject.SetActive(true);
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
