using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using VRT.Core;
using VRT.UserRepresentation.PointCloud;
using VRT.Orchestrator.Wrapping;
using VRT.Pilots.Common;

public class Calibration : MonoBehaviour {
    private enum State { CheckWithUser, SelectTranslationRotation, Translation, Rotation }
    private State       state = State.CheckWithUser;

    [Tooltip("The player to control, for preview")]
    public PlayerControllerBase player;
    [Tooltip("The camera to control, for preview")]
    public GameObject   cameraOffset;
    
    [Tooltip("How fast to rotate")]
    public float        _rotationSlightStep = 1f;
    [Tooltip("How fast to translate")]
    public float        _translationSlightStep = 0.01f;

    const string        prefix = "pcs";

    [Header("UI Panel references")]
    public GameObject   ComfortUI;
    public GameObject   CalibrationModeUI;
    public GameObject   TransalationUI;
    public GameObject   RotationUI;

    [Header("Input Actions")]
    private PlayerInput MyPlayerInput;
    const string YesActionName = "Yes";
    const string NoActionName = "No";
    const string DoneActionName = "Done";
    const string BackActionName = "Back";
    const string ResetRotationActionName = "ResetRotation";
    const string ResetTranslationActionName = "ResetTranslation";
    const string RotateActionName = "Rotate";
    const string TranslateActionName = "Translate";
    const string MoveActionName = "Move";
    const string HeightActionName = "Height";
    const string LookUpDownActionName = "LookUpDown";
  
    public static void ResetFactorySettings()
    {
        PlayerPrefs.SetFloat("pcs_pos_x", 0);
        PlayerPrefs.SetFloat("pcs_pos_y", 0);
        PlayerPrefs.SetFloat("pcs_pos_z", 0);
        PlayerPrefs.SetFloat("pcs_rot_x", 0);
        PlayerPrefs.SetFloat("pcs_rot_y", 0);
        PlayerPrefs.SetFloat("pcs_rot_z", 0);
    }

    public void OnDisable()
    {
        //
        // Workaround for a bug seen in October 2022:
        // https://forum.unity.com/threads/type-of-instance-in-array-does-not-match-expected-type.1320564/
        //
        MyPlayerInput.actions = null;
    }

    private void Start() {
        // Setup enough of the PFB_Player to allow viewing yourself as a pointcloud.
        var user = OrchestratorController.Instance.SelfUser;
        var userConfig = Config.Instance.LocalUser;
        player.SetUpPlayerController(true, user, null);
        player.SetRepresentation(user.userData.userRepresentationType, user, null);
#if xxxjack
        player.pc.gameObject.SetActive(true);
        player.pc.AddComponent<PointCloudPipeline>().Init(true, user, userConfig, true);
#endif
       
    }

    public void Update()
    {
        if (MyPlayerInput == null)
        {
            player.setupCamera();
            // Initialize camera position/orientation from saved preferences
            InitializePosition();

            // Initialize the UI screens
            ChangeModeUI();
            MyPlayerInput = GetComponent<PlayerInput>();

        }
        InputAction YesAction = MyPlayerInput.actions[YesActionName];
       
        InputAction NoAction = MyPlayerInput.actions[NoActionName];
        InputAction DoneAction = MyPlayerInput.actions[DoneActionName];
        InputAction BackAction = MyPlayerInput.actions[BackActionName];
        InputAction RotateAction = MyPlayerInput.actions[RotateActionName];
        InputAction TranslateAction = MyPlayerInput.actions[TranslateActionName];
        InputAction ResetRotationAction = MyPlayerInput.actions[ResetRotationActionName];
        InputAction ResetTranslationAction = MyPlayerInput.actions[ResetTranslationActionName];
        InputAction MoveAction = MyPlayerInput.actions[MoveActionName];
        InputAction HeightAction = MyPlayerInput.actions[HeightActionName];
        InputAction LookUpDownAction = MyPlayerInput.actions[LookUpDownActionName];

        // First tilt the camera, if needed
        var cameraTilt = LookUpDownAction.ReadValue<float>();
        if (cameraTilt != 0)
        {
            Quaternion rot = Quaternion.Euler(cameraTilt, 0, 0);
            //Debug.Log($"xxxjack cameraTilt {cameraTilt}, euler {rot}");
            cameraOffset.transform.localRotation = cameraOffset.transform.localRotation * rot;
        }
        var curMove = MoveAction.ReadValue<Vector2>();
        var curHeight = HeightAction.ReadValue<float>();

        ComfortUI.SetActive(state == State.CheckWithUser);
        CalibrationModeUI.SetActive(state == State.SelectTranslationRotation);
        TransalationUI.SetActive(state == State.Translation);
        RotationUI.SetActive(state == State.Rotation);

        switch (state)
        {
            case State.CheckWithUser:
                // I'm Comfortable
                if (YesAction.triggered)
                {
                    Debug.Log("Calibration: Comfort: User is happy, return to LoginManager");
                    //Application.Quit();
                    PilotController.LoadScene("LoginManager");
                }
                // I'm not comfortable
                if (NoAction.triggered)
                {
                    Debug.Log("Calibration: Comfort: Starting calibration process");
                    state = State.SelectTranslationRotation;
                }
                break;
            case State.SelectTranslationRotation:
                //Activate Translation
                if (TranslateAction.triggered)
                {
                    Debug.Log("Calibration: Mode: Selected Translation Mode");
                    state = State.Translation;
                }
                //Activate Rotation (UpAxis)
                if (RotateAction.triggered)
                {
                    Debug.Log("Calibration: Mode: Selected Rotation Mode");
                    state = State.Rotation;
                }
                
                if (BackAction.triggered)
                {
                    state = State.CheckWithUser;
                }
                break;
            case State.Translation:
                // Movement
                if (ResetTranslationAction.triggered)
                {
                    cameraOffset.transform.localPosition = new Vector3(0, 0, 0);
                    Debug.Log($"Calibration: Translation: reset to 0, 0, 0");
                }
                else
                {
                    float zAxis = curMove.y;
                    float xAxis = curMove.x;
                    float yAxis = curHeight;
                    cameraOffset.transform.localPosition += new Vector3(xAxis, yAxis, zAxis) * _translationSlightStep;
                }
                // Save Translation
                if (YesAction.triggered || DoneAction.triggered)
                {
                    var pos = cameraOffset.transform.localPosition;
                    PlayerPrefs.SetFloat(prefix + "_pos_x", pos.x);
                    PlayerPrefs.SetFloat(prefix + "_pos_y", pos.y);
                    PlayerPrefs.SetFloat(prefix + "_pos_z", pos.z);
                    Debug.Log($"Calibration: Translation: Saved: {pos.x}, {pos.y}, {pos.z}");
                    state = State.CheckWithUser;
                }
                if (BackAction.triggered)
                {
                    Debug.Log("Calibration: Back");
                    InitializePosition();
                    state = State.CheckWithUser;
                }
               
                break;
            case State.Rotation:
                // Rotation
                if (ResetRotationAction.triggered)
                {
                    Debug.Log("Calibration: Rotation: Reset to 0,0,0");
                    cameraOffset.transform.localEulerAngles = new Vector3(0, 0, 0);
                }
                else
                {
                    float yAxisR = curMove.x;
                    cameraOffset.transform.localRotation = Quaternion.Euler(cameraOffset.transform.localRotation.eulerAngles + Vector3.up * -_rotationSlightStep * yAxisR);
                }
                // Save Translation
                if (YesAction.triggered || DoneAction.triggered)
                {
                    var rot = cameraOffset.transform.localRotation.eulerAngles;
                    PlayerPrefs.SetFloat(prefix + "_rot_x", rot.x);
                    PlayerPrefs.SetFloat(prefix + "_rot_y", rot.y);
                    PlayerPrefs.SetFloat(prefix + "_rot_z", rot.z);

                    Debug.Log($"Calibration: Rotation: Saved: {rot.x}, {rot.y}, {rot.z}");
                    state = State.CheckWithUser;
                }
                if (BackAction.triggered)
                {
                    Debug.Log("Calibration: Back");
                    InitializePosition();
                    state = State.CheckWithUser;
                }

                break;
            default:
                Debug.LogError($"Calibration: unexpected state {state}");
                break;
        }
    }

    private void InitializePosition()
    {
        // Get initial position/orientation from the preferences
        Vector3 pos = new Vector3(PlayerPrefs.GetFloat(prefix + "_pos_x", 0), PlayerPrefs.GetFloat(prefix + "_pos_y", 0), PlayerPrefs.GetFloat(prefix + "_pos_z", 0));
        Vector3 rot = new Vector3(PlayerPrefs.GetFloat(prefix + "_rot_x", 0), PlayerPrefs.GetFloat(prefix + "_rot_y", 0), PlayerPrefs.GetFloat(prefix + "_rot_z", 0));
        Debug.Log($"Calibration: initial pos={pos}, rot={rot}");
        cameraOffset.transform.localPosition = pos;
        cameraOffset.transform.localRotation = Quaternion.Euler(rot);
    }

    void ChangeModeUI()
    {
        ComfortUI.SetActive(state == State.CheckWithUser);
        CalibrationModeUI.SetActive(state == State.SelectTranslationRotation);
        TransalationUI.SetActive(state == State.Translation);
        RotationUI.SetActive(state == State.Rotation);
    }
}
