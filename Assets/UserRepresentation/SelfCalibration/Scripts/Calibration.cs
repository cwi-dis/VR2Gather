using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRT.Core;

public class Calibration : MonoBehaviour {
    private enum State { Comfort, Mode, Translation, Rotation }
    private State       state = State.Comfort;

    public GameObject   cameraReference;
    public float        _rotationSlightStep = 1f;
    public float        _translationSlightStep = 0.01f;
    public string        prefix = "pcs";

    [Header("Input controller support")]
    public CalibrationControls emulation;
    public CalibrationControls gamepad;
    public CalibrationControls oculus;
    public CalibrationControls openvr;
    private CalibrationControls controls = null;
    [Header("UI Panel references")]
    public GameObject   ComfortUI;
    public GameObject   CalibrationModeUI;
    public GameObject   TransalationUI;
    public GameObject   RotationUI;

    private void Start() {
        // Enable the correct set of controls (and only the correct set)
        if (VRConfig.Instance.useControllerEmulation()) controls = emulation;
        if (VRConfig.Instance.useControllerGamepad()) controls = gamepad;
        if (VRConfig.Instance.useControllerOculus()) controls = oculus;
        if (VRConfig.Instance.useControllerOpenVR()) controls = openvr;
        emulation.enabled = emulation == controls;
        gamepad.enabled = gamepad == controls;
        oculus.enabled = oculus == controls;
        openvr.enabled = openvr == controls;
        // Get initial position/orientation from the preferences
        cameraReference.transform.localPosition = new Vector3(PlayerPrefs.GetFloat(prefix + "_pos_x", 0), PlayerPrefs.GetFloat(prefix + "_pos_y", 0), PlayerPrefs.GetFloat(prefix + "_pos_z", 0));
        cameraReference.transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat(prefix + "_rot_x", 0), PlayerPrefs.GetFloat(prefix + "_rot_y", 0), PlayerPrefs.GetFloat(prefix + "_rot_z", 0));
    }

    // Update is called once per frame
    void Update() {
        //
        // Enable current UI
        //
        ComfortUI.SetActive(state == State.Comfort);
        CalibrationModeUI.SetActive(state == State.Mode);
        TransalationUI.SetActive(state == State.Translation);
        RotationUI.SetActive(state == State.Rotation);

        switch (state) {
            case State.Comfort:
                // I'm Comfortable
                if (controls.yes.get()) {
                    Debug.Log("Calibration: User is happy, return to LoginManager");
                    //Application.Quit();
                    SceneManager.LoadScene("LoginManager");
                }
                // I'm not comfortable
                if (controls.no.get()) {
                    Debug.Log("Calibration: Starting calibration process");
                    state = State.Mode;
                }
                break;
            case State.Mode:
                //Activate Translation
                if (controls.translate.get()) {
                    Debug.Log("Calibration: Translation Mode");
                    state = State.Translation;
                }
                //Activate Rotation (UpAxis)
                if (controls.rotate.get()) {
                    Debug.Log("Calibration: Rotation Mode");
                    state = State.Rotation;
                }
                if (controls.yes.get()) {
                    Debug.Log("Calibration: User is done");
                    state = State.Comfort;
                }
                break;
            case State.Translation:
                // Movement
                float zAxis = controls.backwardForward.get();
                float xAxis = controls.leftRight.get();
                float yAxis = controls.downUp.get();
                if (zAxis != 0) Debug.Log($"xxxjack rotation {zAxis}");
                if (xAxis != 0) Debug.Log($"xxxjack rotation {xAxis}");
                if (yAxis != 0) Debug.Log($"xxxjack rotation {yAxis}");
                if (controls.reset.get())
                {
                    cameraReference.transform.localPosition = new Vector3(0, 0, 0);
                    Debug.Log("Calibration: Try translation 0,0,0");
                }
                cameraReference.transform.localPosition += new Vector3(xAxis, yAxis, zAxis) * _translationSlightStep;
                // Save Translation
                if (controls.yes.get()) {
                    var pos = cameraReference.transform.localPosition;
                    PlayerPrefs.SetFloat(prefix + "_pos_x", pos.x);
                    PlayerPrefs.SetFloat(prefix + "_pos_y", pos.y);
                    PlayerPrefs.SetFloat(prefix + "_pos_z", pos.z);
                    Debug.Log($"Calibration: Translation Saved: {pos.x},{pos.y},{pos.z}");
                    state = State.Mode;
                }
                // Back
                if (controls.no.get()) {
                    cameraReference.transform.localPosition = new Vector3 ( 
                        PlayerPrefs.GetFloat(prefix+"_pos_x", 0), 
                        PlayerPrefs.GetFloat(prefix+"_pos_y", 0), 
                        PlayerPrefs.GetFloat(prefix+"_pos_z", 0)
                    );
                    var pos = cameraReference.transform.localPosition;
                    Debug.Log($"Calibration: Translation Reset to: {pos.x},{pos.y},{pos.z}");
                    state = State.Mode;
                }
                break;
            case State.Rotation:
                // Rotation
                float yAxisR = controls.leftRight.get();
                if (yAxisR != 0) Debug.Log($"xxxjack rotation {yAxisR}");
                if (controls.reset.get())
                {
                    Debug.Log("Calibration: Try rotation 0,0,0");
                    cameraReference.transform.localEulerAngles = new Vector3(0, 0, 0);
                }
                cameraReference.transform.localRotation = Quaternion.Euler(cameraReference.transform.localRotation.eulerAngles + Vector3.up * -_rotationSlightStep* yAxisR);
                // Save Translation
                if (controls.yes.get()) {
                    var rot = cameraReference.transform.localRotation.eulerAngles;
                    PlayerPrefs.SetFloat(prefix + "_rot_x", rot.x);
                    PlayerPrefs.SetFloat(prefix + "_rot_y", rot.y);
                    PlayerPrefs.SetFloat(prefix + "_rot_z", rot.z);

                    Debug.Log($"Calibration: Rotation Saved: {rot.x},{rot.y},{rot.z}");
                    state = State.Mode;
                }
                // Back
                if (controls.no.get()) {
                    cameraReference.transform.localRotation = Quaternion.Euler( 
                        PlayerPrefs.GetFloat(prefix + "_rot_x", 0),
                        PlayerPrefs.GetFloat(prefix + "_rot_y", 0),
                        PlayerPrefs.GetFloat(prefix + "_rot_z", 0)
                    );
                    var rot = cameraReference.transform.localRotation;
                    Debug.Log($"Calibration: Rotation Reset to: {rot.x},{rot.y},{rot.z}");
                    state = State.Mode;
                }
                break;
            default:
                break;
        }
    }
}
