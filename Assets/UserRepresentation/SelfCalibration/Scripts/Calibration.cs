using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using VRT.Core;

public class Calibration : MonoBehaviour {
    private enum State { CheckWithUser, SelectTranslationRotation, Translation, Rotation }
    private State       state = State.CheckWithUser;

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
        InitializePosition();
        ChangeModeUI();
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

    public void OnYes()
    {
        Debug.Log($"CalibrationControls: OnYes");
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

    public void OnNo()
    {
        Debug.Log($"CalibrationControls: OnNo");
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

    public void OnTranslate()
    {
        Debug.Log($"CalibrationControls: OnTranslate");
        if (state == State.SelectTranslationRotation)
        {
            Debug.Log("Calibration: Mode: Selected Translation Mode");
            state = State.Translation;
            ChangeModeUI();
        }
    }

    public void OnRotate()
    {
        Debug.Log($"CalibrationControls: OnRotate");
        if (state == State.SelectTranslationRotation)
        {
            Debug.Log("Calibration: Mode: Selected Rotation Mode");
            state = State.Rotation;
            ChangeModeUI();
        }

    }

    public void OnDone()
    {
        Debug.Log($"CalibrationControls: OnDone");

        if (state == State.SelectTranslationRotation)
        {
            Debug.Log("Calibration: Mode: User is done");
            state = State.CheckWithUser;
            ChangeModeUI();
        }
    }

    public void OnReset()
    {
        Debug.Log($"CalibrationControls: OnReset");
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

    public void OnBackwardForward(InputValue value)
    {
        var delta = value.Get<float>();
        Debug.Log($"CalibrationControls: OnBackwardForward: {delta}");

        if (state == State.Translation)
        {
            cameraReference.transform.localPosition += new Vector3(0, 0, delta) * _translationSlightStep;
        }
    }

    public void OnLeftRight(InputValue value)
    {
        var delta = value.Get<float>();
        Debug.Log($"CalibrationControls: OnLeftRight: {delta}");
        if (state == State.Translation)
        {
            cameraReference.transform.localPosition += new Vector3(delta, 0, 0) * _translationSlightStep;

        } else
        if (state == State.Rotation)
        {
            cameraReference.transform.localRotation = Quaternion.Euler(cameraReference.transform.localRotation.eulerAngles + Vector3.up * -_rotationSlightStep * delta);
        }
    }

    public void OnUpDown(InputValue value)
    {
        var delta = value.Get<float>();
        Debug.Log($"CalibrationControls: OnUpDown: {delta}");
        if (state == State.Translation)
        {
            cameraReference.transform.localPosition += new Vector3(0, delta, 0) * _translationSlightStep;
        } 
    }

    // Update is called once per frame
    void Update() {
        InitializePosition();
    }

    void ChangeModeUI()
    {
        ComfortUI.SetActive(state == State.CheckWithUser);
        CalibrationModeUI.SetActive(state == State.SelectTranslationRotation);
        TransalationUI.SetActive(state == State.Translation);
        RotationUI.SetActive(state == State.Rotation);
    }
}
