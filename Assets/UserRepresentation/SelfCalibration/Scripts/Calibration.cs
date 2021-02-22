using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Calibration : MonoBehaviour {
    private enum State { Comfort, Mode, Translation, Rotation }
    private State       state = State.Comfort;

    public GameObject   cameraReference;
    public float        _rotationSlightStep = 1f;
    public float        _translationSlightStep = 0.01f;
    public string        prefix = "pcs";
    public GameObject   ComfortUI;
    public GameObject   CalibrationModeUI;
    public GameObject   TransalationUI;
    public GameObject   RotationUI;

    bool rightTrigger = false;
    bool oldRightTrigger = false;
    bool leftTrigger = false;
    bool oldLeftTrigger = false;

    bool IsDownRightTrigger { get { return rightTrigger && !oldRightTrigger; } }
    bool IsDownLeftTrigger { get { return leftTrigger && !oldLeftTrigger; } }

    private void Start() {
        cameraReference.transform.localPosition = new Vector3(PlayerPrefs.GetFloat(prefix + "_pos_x", 0), PlayerPrefs.GetFloat(prefix + "_pos_y", 0), PlayerPrefs.GetFloat(prefix + "_pos_z", 0));
        cameraReference.transform.localRotation = Quaternion.Euler(PlayerPrefs.GetFloat(prefix + "_rot_x", 0), PlayerPrefs.GetFloat(prefix + "_rot_y", 0), PlayerPrefs.GetFloat(prefix + "_rot_z", 0));
    }

    // Update is called once per frame
    void Update() {
        oldRightTrigger = rightTrigger;
        oldLeftTrigger = leftTrigger;
        rightTrigger = Input.GetAxisRaw("PrimaryTriggerRight") >= 0.9;
        leftTrigger = Input.GetAxisRaw("PrimaryTriggerLeft") >= 0.9;
        
        switch (state) {
            case State.Comfort:
                #region UI
                ComfortUI.SetActive(true);
                CalibrationModeUI.SetActive(false);
                TransalationUI.SetActive(false);
                RotationUI.SetActive(false);
                #endregion
                #region INPUT
                // I'm Comfortabler
                if (Input.GetKeyDown(KeyCode.Space) || IsDownRightTrigger || Input.GetKeyDown(KeyCode.Y)) {
                    Debug.Log("Calibration: User is happy, return to LoginManager");
                    //Application.Quit();
                    SceneManager.LoadScene("LoginManager");
                }
                // I'm not comfortable
                if (Input.GetKeyDown(KeyCode.Keypad0) || IsDownLeftTrigger || Input.GetKeyDown(KeyCode.N)) {
                    Debug.Log("Calibration: Starting calibration process");
                    state = State.Mode;
                }
                // ResetAxisTrigger
                #endregion
                break;
            case State.Mode:
                #region UI
                ComfortUI.SetActive(false);
                CalibrationModeUI.SetActive(true);
                TransalationUI.SetActive(false);
                RotationUI.SetActive(false);
                #endregion
                #region INPUT
                //Activate Translation
                if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.T)) {
                    Debug.Log("Calibration: Translation Mode");
                    state = State.Translation;
                }
                //Activate Rotation (UpAxis)
                if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.R)) {
                    Debug.Log("Calibration: Rotation Mode");
                    state = State.Rotation;
                }
                if (Input.GetKeyDown(KeyCode.Space) || IsDownRightTrigger || IsDownLeftTrigger) {
                    Debug.Log("Calibration: User is done");
                    state = State.Comfort;
                }
                #endregion
                break;
            case State.Translation:
                #region UI
                ComfortUI.SetActive(false);
                CalibrationModeUI.SetActive(false);
                TransalationUI.SetActive(true);
                RotationUI.SetActive(false);
                #endregion
                #region INPUT
                // Movement
                float zAxis = Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                if (Input.GetKeyDown(KeyCode.Keypad8) || Input.GetKeyDown(KeyCode.F)) zAxis = 1;
                if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.B)) zAxis = -1;
                float xAxis = -Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.R)) xAxis = 1;
                if (Input.GetKeyDown(KeyCode.Keypad6) || Input.GetKeyDown(KeyCode.L)) xAxis = -1;
                float yAxis = -Input.GetAxis("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                if (Input.GetKeyDown(KeyCode.Keypad9) || Input.GetKeyDown(KeyCode.U)) yAxis = 1;
                if (Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.D)) yAxis = -1;
                // Code added by Jack to allow resetting of position (mainly for non-HMD users)
                if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.JoystickButton2))
                {
                    cameraReference.transform.localPosition = new Vector3(0, 0, 0);
                    Debug.Log("Calibration: Try translation 0,0,0");
                }
                cameraReference.transform.localPosition += new Vector3(xAxis, yAxis, zAxis) * _translationSlightStep;
                // Save Translation
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetAxis("PrimaryTriggerRight") >= 0.9) {
                    var pos = cameraReference.transform.localPosition;
                    PlayerPrefs.SetFloat(prefix + "_pos_x", pos.x);
                    PlayerPrefs.SetFloat(prefix + "_pos_y", pos.y);
                    PlayerPrefs.SetFloat(prefix + "_pos_z", pos.z);
                    Debug.Log($"Calibration: Translation Saved: {pos.x},{pos.y},{pos.z}");
                    state = State.Mode;
                }
                // Back
                if (Input.GetKeyDown(KeyCode.Backspace) || IsDownLeftTrigger) {
                    cameraReference.transform.localPosition = new Vector3 ( 
                        PlayerPrefs.GetFloat(prefix+"_pos_x", 0), 
                        PlayerPrefs.GetFloat(prefix+"_pos_y", 0), 
                        PlayerPrefs.GetFloat(prefix+"_pos_z", 0)
                    );
                    var pos = cameraReference.transform.localPosition;
                    Debug.Log($"Calibration: Translation Reset to: {pos.x},{pos.y},{pos.z}");
                    state = State.Mode;
                }
                #endregion
                break;
            case State.Rotation:
                #region UI
                ComfortUI.SetActive(false);
                CalibrationModeUI.SetActive(false);
                TransalationUI.SetActive(false);
                RotationUI.SetActive(true);
                #endregion
                #region INPUT
                // Rotation
                float yAxisR = Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                if (Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.L)) yAxisR = -1;
                if (Input.GetKeyDown(KeyCode.Keypad9) || Input.GetKeyDown(KeyCode.R)) yAxisR =  1;
                // Code added by Jack to allow resetting of rotation (mainly for non-HMD users)
                if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.JoystickButton0))
                {
                    Debug.Log("Calibration: Try rotation 0,0,0");
                    cameraReference.transform.localEulerAngles = new Vector3(0, 0, 0);
                }
                cameraReference.transform.localRotation = Quaternion.Euler(cameraReference.transform.localRotation.eulerAngles + Vector3.up * -_rotationSlightStep* yAxisR);
                // Save Translation
                if (Input.GetKeyDown(KeyCode.Space) || IsDownRightTrigger ) {
                    var rot = cameraReference.transform.localRotation.eulerAngles;
                    PlayerPrefs.SetFloat(prefix + "_rot_x", rot.x);
                    PlayerPrefs.SetFloat(prefix + "_rot_y", rot.y);
                    PlayerPrefs.SetFloat(prefix + "_rot_z", rot.z);

                    Debug.Log($"Calibration: Rotation Saved: {rot.x},{rot.y},{rot.z}");
                    state = State.Mode;
                }
                // Back
                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetAxis("PrimaryTriggerLeft") >= 0.9) {
                    cameraReference.transform.localRotation = Quaternion.Euler( 
                        PlayerPrefs.GetFloat(prefix + "_rot_x", 0),
                        PlayerPrefs.GetFloat(prefix + "_rot_y", 0),
                        PlayerPrefs.GetFloat(prefix + "_rot_z", 0)
                    );
                    var rot = cameraReference.transform.localRotation;
                    Debug.Log($"Calibration: Rotation Reset to: {rot.x},{rot.y},{rot.z}");
                    state = State.Mode;
                }
                #endregion
                break;
            default:
                break;
        }
    }
}
