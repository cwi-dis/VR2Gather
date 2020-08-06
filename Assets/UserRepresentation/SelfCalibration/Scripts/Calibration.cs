using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Workers;

public class Calibration : MonoBehaviour {
    private enum State { Comfort, Mode, Translation, Rotation }
    private State       state = State.Comfort;

    public float        _rotationSlightStep = 1f;
    public float        _translationSlightStep = 0.01f;
    public string        prefix = "pcs";
    public GameObject   ComfortUI;
    public GameObject   CalibrationModeUI;
    public GameObject   TransalationUI;
    public GameObject   RotationUI;

    EntityPipeline p0;

    // Start is called before the first frame update
    void Start() {
        if (OrchestratorController.Instance.SelfUser.userData.userRepresentationType == OrchestratorWrapping.UserData.eUserRepresentationType.__PCC_CWI_)
            p0 = gameObject.AddComponent<EntityPipeline>().Init(new OrchestratorWrapping.User(), Config.Instance.LocalUser, "", "", true);
    }

    bool rightTrigger = false;
    bool oldRightTrigger = false;
    bool leftTrigger = false;
    bool oldLeftTrigger = false;

    bool IsDownRightTrigger { get { return rightTrigger && !oldRightTrigger; } }
    bool IsDownLeftTrigger { get { return leftTrigger && !oldLeftTrigger; } }

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
                if (Input.GetKeyDown(KeyCode.Space) || IsDownRightTrigger) {
                    Debug.Log("Comfortable!");
                    //Application.Quit();
                    SceneManager.LoadScene("LoginManager");
                }
                // I'm not comfortable
                if (Input.GetKeyDown(KeyCode.Keypad0) || IsDownLeftTrigger ) {
                    Debug.Log("Calibration ON!");
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
                if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.JoystickButton0)) {
                    Debug.Log("Translation Mode");
                    state = State.Translation;
                }
                //Activate Rotation (UpAxis)
                if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.JoystickButton2)) {
                    Debug.Log("Rotation Mode");
                    state = State.Rotation;
                }
                if (Input.GetKeyDown(KeyCode.Space) || IsDownRightTrigger || IsDownLeftTrigger) {
                    Debug.Log("Calibration OFF!");
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
                if (Input.GetKeyDown(KeyCode.Keypad8)) zAxis = 1;
                if (Input.GetKeyDown(KeyCode.Keypad2)) zAxis = -1;
                float xAxis = -Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                if (Input.GetKeyDown(KeyCode.Keypad4)) xAxis = 1;
                if (Input.GetKeyDown(KeyCode.Keypad6)) xAxis = -1;
                float yAxis = -Input.GetAxis("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                if (Input.GetKeyDown(KeyCode.Keypad9)) yAxis = 1;
                if (Input.GetKeyDown(KeyCode.Keypad7)) yAxis = -1;
                this.transform.localPosition += new Vector3(xAxis, yAxis, zAxis) * _translationSlightStep;
                // Save Translation
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetAxis("PrimaryTriggerRight") >= 0.9) {
                    var pos = transform.localPosition;
                    PlayerPrefs.SetFloat(prefix + "_pos_x", pos.x);
                    PlayerPrefs.SetFloat(prefix + "_pos_y", pos.y);
                    PlayerPrefs.SetFloat(prefix + "_pos_z", pos.z);
                    Debug.Log("Translation Saved!");
                    state = State.Mode;
                }
                // Back
                if (Input.GetKeyDown(KeyCode.Backspace) || IsDownLeftTrigger) {
                    transform.localPosition = new Vector3 ( 
                        PlayerPrefs.GetFloat(prefix+"_pos_x", 0), 
                        PlayerPrefs.GetFloat(prefix+"_pos_y", 0), 
                        PlayerPrefs.GetFloat(prefix+"_pos_z", 0)
                    );
                    Debug.Log("Translation Reset!");
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
                if (Input.GetKeyDown(KeyCode.Keypad7)) yAxisR = -1;
                if (Input.GetKeyDown(KeyCode.Keypad9)) yAxisR =  1;
                transform.localRotation = Quaternion.Euler( transform.localRotation.eulerAngles + Vector3.up * -_rotationSlightStep* yAxisR);
                // Save Translation
                if (Input.GetKeyDown(KeyCode.Space) || IsDownRightTrigger ) {
                    var rot = transform.localRotation.eulerAngles;
                    PlayerPrefs.SetFloat(prefix + "_rot_x", rot.x);
                    PlayerPrefs.SetFloat(prefix + "_rot_y", rot.y);
                    PlayerPrefs.SetFloat(prefix + "_rot_z", rot.z);

                    Debug.Log("Rotation Saved!");
                    state = State.Mode;
                }
                // Back
                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetAxis("PrimaryTriggerLeft") >= 0.9) {
                    transform.localRotation = Quaternion.Euler( 
                        PlayerPrefs.GetFloat(prefix + "_rot_x", 0),
                        PlayerPrefs.GetFloat(prefix + "_rot_y", 0),
                        PlayerPrefs.GetFloat(prefix + "_rot_z", 0)
                    );
                    Debug.Log("Rotation Reset!");
                    state = State.Mode;
                }
                #endregion
                break;
            default:
                break;
        }
    }
}
