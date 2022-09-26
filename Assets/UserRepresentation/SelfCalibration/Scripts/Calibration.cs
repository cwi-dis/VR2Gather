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
    public GameObject   cameraReference;
    
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
    public PlayerInput MyPlayerInput;
    public string YesActionName = "YesDone";
    public InputAction YesAction;
    public string NoActionName = "NoBack";
    public InputAction NoAction;
    public string DoneActionName = "YesDone";
    public InputAction DoneAction;
    public string ResetActionName = "NoBack";
    public InputAction ResetAction;
    public string RotateActionName = "Rotate";
    public InputAction RotateAction;
    public string TranslateActionName = "Translate";
    public InputAction TranslateAction;
    public string MoveActionName = "Move";
    public InputAction MoveAction;
    public string HeightActionName = "DownUp";
    public InputAction HeightAction;

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
            MyPlayerInput = GetComponent<PlayerInput>();
            YesAction = MyPlayerInput.actions[YesActionName];
            NoAction = MyPlayerInput.actions[NoActionName];
            DoneAction = MyPlayerInput.actions[DoneActionName];
            ResetAction = MyPlayerInput.actions[ResetActionName];
            RotateAction = MyPlayerInput.actions[RotateActionName];
            TranslateAction = MyPlayerInput.actions[TranslateActionName];
            MoveAction = MyPlayerInput.actions[MoveActionName];
            HeightAction = MyPlayerInput.actions[HeightActionName];
        }

        if (YesAction.triggered) Debug.Log("xxxjack YesAction triggered");
        if (NoAction.triggered) Debug.Log("xxxjack NoAction triggered");
        if (DoneAction.triggered) Debug.Log("xxxjack DoneAction triggered");
        if (ResetAction.triggered) Debug.Log("xxxjack ResetAction triggered");
        if (RotateAction.triggered) Debug.Log("xxxjack RotateAction triggered");
        if (TranslateAction.triggered) Debug.Log("xxxjack TranslateAction triggered");

        var curMove = MoveAction.ReadValue<Vector2>();
        if (curMove != Vector2.zero) Debug.Log($"xxxjack MoveAction {curMove}");
        var curHeight = HeightAction.ReadValue<float>();
        if (curHeight != 0) Debug.Log($"xxxjack HeightAction {curHeight}");

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
                    cameraReference.transform.localPosition = Vector3.zero;
                    cameraReference.transform.localRotation = Quaternion.Euler(Vector3.zero);
                }
                if (YesAction.triggered || NoAction.triggered || DoneAction.triggered)
                {
                    Debug.Log("Calibration: Mode: User is done");
                    state = State.CheckWithUser;
                }
                break;
            case State.Translation:
                // Movement
                float zAxis = curMove.y;
                float xAxis = curMove.x;
                float yAxis = curHeight;
                if (zAxis != 0) Debug.Log($"xxxjack translation z={zAxis}");
                if (xAxis != 0) Debug.Log($"xxxjack translation x={xAxis}");
                if (yAxis != 0) Debug.Log($"xxxjack translation y={yAxis}");
                if (ResetAction.triggered)
                {
                    cameraReference.transform.localPosition = new Vector3(0, 0, 0);
                    Debug.Log($"Calibration: Translation: reset to 0, 0, 0");
                }
                cameraReference.transform.localPosition += new Vector3(xAxis, yAxis, zAxis) * _translationSlightStep;
                // Save Translation
                if (YesAction.triggered)
                {
                    var pos = cameraReference.transform.localPosition;
                    PlayerPrefs.SetFloat(prefix + "_pos_x", pos.x);
                    PlayerPrefs.SetFloat(prefix + "_pos_y", pos.y);
                    PlayerPrefs.SetFloat(prefix + "_pos_z", pos.z);
                    Debug.Log($"Calibration: Translation: Saved: {pos.x}, {pos.y}, {pos.z}");
                    state = State.CheckWithUser;
                }
                // Back
                if (NoAction.triggered)
                {
                    cameraReference.transform.localPosition = new Vector3(
                        PlayerPrefs.GetFloat(prefix + "_pos_x", 0),
                        PlayerPrefs.GetFloat(prefix + "_pos_y", 0),
                        PlayerPrefs.GetFloat(prefix + "_pos_z", 0)
                    );
                    var pos = cameraReference.transform.localPosition;
                    Debug.Log($"Calibration: Translation: Reloaded to: {pos.x}, {pos.y}, {pos.z}");
                    state = State.CheckWithUser;
                }
                break;
            case State.Rotation:
                // Rotation
                float yAxisR = curMove.x;
                if (yAxisR != 0) Debug.Log($"xxxjack rotation y={yAxisR}");
                if (ResetAction.triggered)
                {
                    Debug.Log("Calibration: Rotation: Reset to 0,0,0");
                    cameraReference.transform.localEulerAngles = new Vector3(0, 0, 0);
                }
                cameraReference.transform.localRotation = Quaternion.Euler(cameraReference.transform.localRotation.eulerAngles + Vector3.up * -_rotationSlightStep * yAxisR);
                // Save Translation
                if (YesAction.triggered)
                {
                    var rot = cameraReference.transform.localRotation.eulerAngles;
                    PlayerPrefs.SetFloat(prefix + "_rot_x", rot.x);
                    PlayerPrefs.SetFloat(prefix + "_rot_y", rot.y);
                    PlayerPrefs.SetFloat(prefix + "_rot_z", rot.z);

                    Debug.Log($"Calibration: Rotation: Saved: {rot.x}, {rot.y}, {rot.z}");
                    state = State.CheckWithUser;
                }
                // Back
                if (NoAction.triggered)
                {
                    cameraReference.transform.localRotation = Quaternion.Euler(
                        PlayerPrefs.GetFloat(prefix + "_rot_x", 0),
                        PlayerPrefs.GetFloat(prefix + "_rot_y", 0),
                        PlayerPrefs.GetFloat(prefix + "_rot_z", 0)
                    );
                    var rot = cameraReference.transform.localRotation;
                    Debug.Log($"Calibration: Rotation: Reloaded to: {rot.x}, {rot.y}, {rot.z}");
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
        cameraReference.transform.localPosition = pos;
        cameraReference.transform.localRotation = Quaternion.Euler(rot);
    }

#if xxxjack_outdated
    public void OnYesDone(InputValue value)
    {
        if (value.Get<float>() < 0.5) return;
        Debug.Log($"Calibration: OnYesDone");
        if (state == State.CheckWithUser)
        {
            Debug.Log("Calibration: CheckWithUser: User is happy, return to LoginManager");
            SceneManager.LoadScene("LoginManager");
        } else
        if (state == State.SelectTranslationRotation)
        {
            Debug.Log("Calibration: Mode: User is done");
            state = State.CheckWithUser;
            ChangeModeUI();
        }
        else
        if (state == State.Translation)
        {
            var pos = cameraReference.transform.localPosition;
            PlayerPrefs.SetFloat(prefix + "_pos_x", pos.x);
            PlayerPrefs.SetFloat(prefix + "_pos_y", pos.y);
            PlayerPrefs.SetFloat(prefix + "_pos_z", pos.z);
            Debug.Log($"Calibration: Translation: Saved: {pos.x}, {pos.y}, {pos.z}");
            state = State.SelectTranslationRotation;
            ChangeModeUI();
        }
        else
        if (state == State.Rotation)
        {
            var rot = cameraReference.transform.localRotation.eulerAngles;
            PlayerPrefs.SetFloat(prefix + "_rot_x", rot.x);
            PlayerPrefs.SetFloat(prefix + "_rot_y", rot.y);
            PlayerPrefs.SetFloat(prefix + "_rot_z", rot.z);

            Debug.Log($"Calibration: Rotation: Saved: {rot.x}, {rot.y}, {rot.z}");
            state = State.SelectTranslationRotation;
            ChangeModeUI();
        }
    }

    public void OnNoBack(InputValue value)
    {
        if (value.Get<float>() < 0.5) return;
        Debug.Log($"Calibration: OnNoBack");
        if (state == State.CheckWithUser)
        {
            Debug.Log("Calibration: Comfort: Starting calibration process");
            state = State.SelectTranslationRotation;
            ChangeModeUI();
        }
        else
        if (state == State.SelectTranslationRotation)
        {
            Debug.Log("Calibration: Mode: User is done");
            state = State.CheckWithUser;
            ChangeModeUI();
        }
        else
        if (state == State.Translation)
        {
            cameraReference.transform.localPosition = new Vector3(
                        PlayerPrefs.GetFloat(prefix + "_pos_x", 0),
                        PlayerPrefs.GetFloat(prefix + "_pos_y", 0),
                        PlayerPrefs.GetFloat(prefix + "_pos_z", 0)
                    );
            var pos = cameraReference.transform.localPosition;
            Debug.Log($"Calibration: Translation: Reloaded to: {pos.x}, {pos.y}, {pos.z}");
            state = State.SelectTranslationRotation;
            ChangeModeUI();
        }
        else
        if (state == State.Rotation)
        {
            cameraReference.transform.localRotation = Quaternion.Euler(
                 PlayerPrefs.GetFloat(prefix + "_rot_x", 0),
                 PlayerPrefs.GetFloat(prefix + "_rot_y", 0),
                 PlayerPrefs.GetFloat(prefix + "_rot_z", 0)
             );
            var rot = cameraReference.transform.localRotation;
            Debug.Log($"Calibration: Rotation: Reloaded to: {rot.x}, {rot.y}, {rot.z}");
            state = State.SelectTranslationRotation;
            ChangeModeUI();
        }
    }

    public void OnTranslate(InputValue value)
    {
        if (value.Get<float>() < 0.5) return;
        Debug.Log($"Calibration: OnTranslate");
        if (state == State.SelectTranslationRotation)
        {
            Debug.Log("Calibration: Mode: Selected Translation Mode");
            state = State.Translation;
            ChangeModeUI();
        }
    }

    public void OnRotate(InputValue value)
    {
        if (value.Get<float>() < 0.5) return;
        Debug.Log($"Calibration: OnRotate");
        if (state == State.SelectTranslationRotation)
        {
            Debug.Log("Calibration: Mode: Selected Rotation Mode");
            state = State.Rotation;
            ChangeModeUI();
        }

    }

    public void OnReset(InputValue value)
    {
        if (value.Get<float>() < 0.5) return;
        Debug.Log($"Calibration: OnReset");
        if (state == State.SelectTranslationRotation)
        {
            Debug.Log("Calibration: Mode: Reset factory settings");
            ResetFactorySettings();
            cameraReference.transform.localPosition = Vector3.zero;
            cameraReference.transform.localRotation = Quaternion.Euler(Vector3.zero);

        } else
        if (state == State.Translation)
        {
            cameraReference.transform.localPosition = new Vector3(0, 0, 0);
            Debug.Log($"Calibration: Translation: reset to 0, 0, 0");
        } else
        if (state == State.Rotation)
        {
            Debug.Log("Calibration: Rotation: Reset to 0,0,0");
            cameraReference.transform.localEulerAngles = new Vector3(0, 0, 0);
        }
    }

    public void OnMove(InputValue value)
    {
        var delta = value.Get<Vector2>();
        if (delta.x == 0 && delta.y == 0) return;
        Debug.Log($"Calibration: OnBackwardForward: {delta}");

        if (state == State.Translation)
        {
            cameraReference.transform.localPosition += new Vector3(delta.x, 0, delta.y) * _translationSlightStep;
        }
        else
        if (state == State.Rotation)
        {
            cameraReference.transform.localRotation = Quaternion.Euler(cameraReference.transform.localRotation.eulerAngles + Vector3.up * -_rotationSlightStep * delta.x);
        }
    }

    public void OnDownUp(InputValue value)
    {
        var delta = value.Get<float>();
        if (delta == 0) return;
        Debug.Log($"Calibration: OnDownUp: {delta}");
        if (state == State.Translation)
        {
            cameraReference.transform.localPosition += new Vector3(0, delta, 0) * _translationSlightStep;
        } 
    }
#endif

    void ChangeModeUI()
    {
        ComfortUI.SetActive(state == State.CheckWithUser);
        CalibrationModeUI.SetActive(state == State.SelectTranslationRotation);
        TransalationUI.SetActive(state == State.Translation);
        RotationUI.SetActive(state == State.Rotation);
    }
}
