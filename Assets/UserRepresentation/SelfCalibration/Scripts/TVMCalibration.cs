using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TVMCalibration : MonoBehaviour {

    private enum State { Comfort, Mode, Translation, Rotation}

    private bool axisLInUse = false;
    private bool axisRInUse = false;
    private float _rotationSlightStep = 1f;
    private float _translationSlightStep = 0.01f;
    private State state = State.Comfort;
    public GameObject ComfortUI;
    public GameObject CalibrationModeUI;
    public GameObject TransalationUI;
    public GameObject RotationUI;
    private Config cfg;
    private Config._TVMs tvm;

    // Use this for initialization
    void Start() {
        cfg = Config.Instance;
        tvm = cfg.TVMs;
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case State.Comfort:
                #region UI
                ComfortUI.SetActive(true);
                CalibrationModeUI.SetActive(false);
                TransalationUI.SetActive(false);
                RotationUI.SetActive(false);
                #endregion
                #region INPUT
                // I'm Comfortabler
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetAxisRaw("PrimaryTriggerRight") >= 0.9) {
                    if (!axisRInUse) {
                        Debug.Log("Comfortable!");
                        //Application.Quit();
                        SceneManager.LoadScene("LoginManager");
                        axisRInUse = true;
                    }
                }
                // I'm not comfortable
                if (Input.GetKeyDown(KeyCode.Keypad0) || Input.GetAxisRaw("PrimaryTriggerLeft") >= 0.9) {
                    if (!axisLInUse) {
                        Debug.Log("Calibration ON!");
                        state = State.Mode;
                        axisLInUse = true;
                    }
                }
                // ResetAxisTrigger
                if (Input.GetAxisRaw("PrimaryTriggerLeft") == 0) axisLInUse = false;
                if (Input.GetAxisRaw("PrimaryTriggerRight") == 0) axisRInUse = false;
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
                if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.JoystickButton0)) {
                    Debug.Log("Translation Mode");
                    state = State.Translation;
                }
                //Activate Rotation (UpAxis)
                if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.JoystickButton2)) {
                    Debug.Log("Rotation Mode");
                    state = State.Rotation;
                }
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetAxis("PrimaryTriggerRight") >= 0.9 ||
                    Input.GetAxis("PrimaryTriggerLeft") >= 0.9) {                    
                    if (!axisLInUse && !axisRInUse) {
                        Debug.Log("Calibration OFF!");
                        cfg.WriteConfig(cfg);
                        state = State.Comfort;
                        axisLInUse = true;
                        axisRInUse = true;
                    }
                }
                // ResetAxisTrigger
                if (Input.GetAxisRaw("PrimaryTriggerLeft") == 0) axisLInUse = false;
                if (Input.GetAxisRaw("PrimaryTriggerRight") == 0) axisRInUse = false;
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
                if (Input.GetKeyDown(KeyCode.Keypad8) || Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical") <= -0.9) { //Up Z (Forward Movement) 
                    this.transform.Translate(new Vector3(0, 0, _translationSlightStep), Space.World);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical") >= 0.9) { //Down Z (Backward Movement) 
                    this.transform.Translate(new Vector3(0, 0, -_translationSlightStep), Space.World);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickHorizontal") <= -0.9) { //Up X (Left Movement) 
                    this.transform.Translate(new Vector3(_translationSlightStep, 0, 0), Space.World);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad6) || Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickHorizontal") >= 0.9) { //Down X (Right Movement) 
                    this.transform.Translate(new Vector3(-_translationSlightStep, 0, 0), Space.World);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad9) || Input.GetAxis("Oculus_CrossPlatform_PrimaryThumbstickVertical") >= 0.9) { //Up Y (Up Movement) 
                    this.transform.Translate(new Vector3(0, _translationSlightStep, 0), Space.World);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad7) || Input.GetAxis("Oculus_CrossPlatform_PrimaryThumbstickVertical") <= -0.9) { //Down Y (Down Movement) 
                    this.transform.Translate(new Vector3(0, -_translationSlightStep, 0), Space.World);
                }
                // Save Translation
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetAxis("PrimaryTriggerRight") >= 0.9) {
                    if (!axisRInUse) {
                        var pos = this.transform.localPosition;
                        PlayerPrefs.SetFloat("x_pos", pos.x);
                        PlayerPrefs.SetFloat("y_pos", pos.y);
                        PlayerPrefs.SetFloat("z_pos", pos.z);

                        tvm.offsetPosition = pos;

                        Debug.Log("Translation Saved!");
                        state = State.Mode;
                        axisRInUse = true;
                    }
                }
                // Back
                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetAxis("PrimaryTriggerLeft") >= 0.9) {
                    if (!axisLInUse) {
                        this.transform.localPosition = tvm.offsetPosition;

                        Debug.Log("Translation Reset!");
                        state = State.Mode;
                        axisLInUse = true;
                    }
                }
                // ResetAxisTrigger
                if (Input.GetAxisRaw("PrimaryTriggerLeft") == 0) axisLInUse = false;
                if (Input.GetAxisRaw("PrimaryTriggerRight") == 0) axisRInUse = false;
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
                if (Input.GetKeyDown(KeyCode.Keypad7) || Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickHorizontal") <= -0.9) { //Rotate Left
                    this.transform.Rotate(Vector3.up, -_rotationSlightStep, Space.Self);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad9) || Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickHorizontal") >= 0.9) { //Rotate Right
                    this.transform.Rotate(Vector3.up, _rotationSlightStep, Space.Self);
                }
                // Save Translation
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetAxis("PrimaryTriggerRight") >= 0.9) {
                    if (!axisRInUse) {                      
                        var rot = this.transform.localRotation.eulerAngles;
                        PlayerPrefs.SetFloat("x", rot.x);
                        PlayerPrefs.SetFloat("y", rot.y);
                        PlayerPrefs.SetFloat("z", rot.z);

                        tvm.offsetRotation = rot;

                        Debug.Log("Rotation Saved!");
                        state = State.Mode;
                        axisRInUse = true;
                    }
                }
                // Back
                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetAxis("PrimaryTriggerLeft") >= 0.9) {
                    if (!axisLInUse) {
                        this.transform.localRotation = Quaternion.Euler(tvm.offsetRotation);

                        Debug.Log("Rotation Reset!");
                        state = State.Mode;
                        axisLInUse = true;
                    }
                }
                // ResetAxisTrigger
                if (Input.GetAxisRaw("PrimaryTriggerLeft") == 0) axisLInUse = false;
                if (Input.GetAxisRaw("PrimaryTriggerRight") == 0) axisRInUse = false;
                #endregion
                break;
            default:
                break;
        }
    }
}
