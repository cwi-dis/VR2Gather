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

public class Calibration : MonoBehaviour {
    private enum State { CheckWithUser, SelectTranslationRotation, Translation, Rotation }
    private State       state = State.CheckWithUser;

    [Tooltip("The player to control, for preview")]
    public PlayerManager player;
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
    const string ResetActionName = "Reset";
    const string RotateActionName = "Rotate";
    const string TranslateActionName = "Translate";
    const string MoveActionName = "Move";
    const string HeightActionName = "Height";
  
    public static void ResetFactorySettings()
    {
        PlayerPrefs.SetFloat("pcs_pos_x", 0);
        PlayerPrefs.SetFloat("pcs_pos_y", 0);
        PlayerPrefs.SetFloat("pcs_pos_z", 0);
        PlayerPrefs.SetFloat("pcs_rot_x", 0);
        PlayerPrefs.SetFloat("pcs_rot_y", 0);
        PlayerPrefs.SetFloat("pcs_rot_z", 0);
    }

    private void Start() {
        // Setup enough of the PFB_Player to allow viewing yourself as a pointcloud.
        player.setupInputOutput(true, disableInput: true);
        player.pc.gameObject.SetActive(true);
        player.pc.AddComponent<PointCloudPipeline>().Init(OrchestratorController.Instance.SelfUser, Config.Instance.LocalUser, true);

        // Initialize camera position/orientation from saved preferences
        InitializePosition();

        // Initialize the UI screens
        ChangeModeUI();
    }

    public void Update()
    {
        if (MyPlayerInput == null)
        {
            if (!VRConfig.Instance.initialized)
            {
                Debug.LogError("Calibration: Update: VR config not yet initialized");
                return;
            }

            MyPlayerInput = GetComponent<PlayerInput>();
#if xxxjack_switch_control_scheme
            if (VRConfig.Instance.useControllerEmulation())
            {
                MyPlayerInput.SwitchCurrentControlScheme("KeyboardMouse");
            }
            else
            if (VRConfig.Instance.useControllerGamepad())
            {
                MyPlayerInput.SwitchCurrentControlScheme("XBox Gamepad");

            }
            else
            if (VRConfig.Instance.useControllerOculus())
            {
                MyPlayerInput.SwitchCurrentControlScheme("Oculus");
            }
            else
            if (VRConfig.Instance.useControllerOpenXR())
            {
                MyPlayerInput.SwitchCurrentControlScheme("OpenXR");
            }
            Debug.Log($"Calibration: control scheme {MyPlayerInput.currentControlScheme}");
#endif
        }
        InputAction YesAction = MyPlayerInput.actions[YesActionName];
       
        InputAction NoAction = MyPlayerInput.actions[NoActionName];
        InputAction DoneAction = MyPlayerInput.actions[DoneActionName];
        InputAction ResetAction = MyPlayerInput.actions[ResetActionName];
        InputAction RotateAction = MyPlayerInput.actions[RotateActionName];
        InputAction TranslateAction = MyPlayerInput.actions[TranslateActionName];
        InputAction MoveAction = MyPlayerInput.actions[MoveActionName];
        InputAction HeightAction = MyPlayerInput.actions[HeightActionName];

        if (YesAction.triggered) Debug.Log($"xxxjack YesAction triggered by {YesAction.activeControl.path}");
        if (NoAction.triggered) Debug.Log($"xxxjack NoAction triggered by {NoAction.activeControl.path}");
        if (DoneAction.triggered) Debug.Log($"xxxjack DoneAction triggered by {DoneAction.activeControl.path}");
        if (ResetAction.triggered) Debug.Log($"xxxjack ResetAction triggered by {ResetAction.activeControl.path}");
        if (RotateAction.triggered) Debug.Log($"xxxjack RotateAction triggered by {RotateAction.activeControl.path}");
        if (TranslateAction.triggered) Debug.Log($"xxxjack TranslateAction triggered by {TranslateAction.activeControl.path}");

        var curMove = MoveAction.ReadValue<Vector2>();
        if (curMove != Vector2.zero) Debug.Log($"xxxjack MoveAction {curMove} by {MoveAction.activeControl.path}");
        var curHeight = HeightAction.ReadValue<float>();
        if (curHeight != 0) Debug.Log($"xxxjack HeightAction {curHeight} by {HeightAction.activeControl.path}");

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
                    SceneManager.LoadScene("LoginManager");
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
                // Reset everything to factory settings
                if (ResetAction.triggered)
                {
                    Debug.Log("Calibration: Mode: Reset factory settings");
                    ResetFactorySettings();
                    cameraOffset.transform.localPosition = Vector3.zero;
                    cameraOffset.transform.localRotation = Quaternion.Euler(Vector3.zero);
                }
                if (DoneAction.triggered)
                {
                    state = State.CheckWithUser;
                }
                break;
            case State.Translation:
                // Movement
                if (ResetAction.triggered)
                {
                    cameraOffset.transform.localPosition = new Vector3(0, 0, 0);
                    Debug.Log($"Calibration: Translation: reset to 0, 0, 0");
                }
                else
                {
                    float zAxis = curMove.y;
                    float xAxis = curMove.x;
                    float yAxis = curHeight;
                    if (zAxis != 0) Debug.Log($"xxxjack translation z={zAxis}");
                    if (xAxis != 0) Debug.Log($"xxxjack translation x={xAxis}");
                    if (yAxis != 0) Debug.Log($"xxxjack translation y={yAxis}");
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
               
                break;
            case State.Rotation:
                // Rotation
                if (ResetAction.triggered)
                {
                    Debug.Log("Calibration: Rotation: Reset to 0,0,0");
                    cameraOffset.transform.localEulerAngles = new Vector3(0, 0, 0);
                }
                else
                {
                    float yAxisR = curMove.x;
                    if (yAxisR != 0) Debug.Log($"xxxjack rotation y={yAxisR}");
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
                
                break;
            default:
                Debug.LogError($"Calibration: unexpected state {state}");
                break;
        }
    }

    private void InitializePosition()
    {
        if (!VRConfig.Instance.initialized)
        {
            Debug.LogError("Calibration: VR config not yet initialized");
        }
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
