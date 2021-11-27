using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using VRT.Teleporter;

public class KeyboardVRTeleporterController : MonoBehaviour
{
    

    public VRTeleporter teleporter;
    public string rightTriggerName = "PrimaryTriggerRight";
    public string leftTriggerName = "PrimaryTriggerLeft";
    public string rightVerticalName = "Oculus_GearVR_RThumbstickY";
    public string rightHorizontalName = "Oculus_GearVR_RThumbstickX";
    public bool allowNVGBkeys = true;

    private Vector3 dir;
    private float str = 7.0f;
    private float dirMul = 0.01f;
    private float strMul = 0.05f;


    float rightTrigger = 0.0f;
    float leftTrigger = 0.0f;
    float rightHorizontal = 0.0f;
    float rightVertical = 0.0f;

    private void Awake()
    {
    }

    void Update() {

        rightTrigger = Input.GetAxisRaw(rightTriggerName);
        leftTrigger = Input.GetAxisRaw(leftTriggerName);
        rightVertical = Input.GetAxis(rightVerticalName);
        rightHorizontal = Input.GetAxis(rightHorizontalName);

        /*if (IsDownRightTrigger || IsDownLeftTrigger)
        {
            Debug.Log("TRIGGER!");
        }*/


        if (teleporter.displayActive) {
            teleporter.CustomUpdatePath(dir, str);
        }
        

        // Start TeleportProcess
        if (Input.GetMouseButtonDown(0) || rightTrigger >= 0.8f)
        {
            teleporter.ToggleDisplay(true);
            dir = transform.forward;
            str = 7.0f;
        }
        // Confirm TeleportProcess
        if (Input.GetMouseButtonDown(1) || leftTrigger >= 0.8f) {
            if (teleporter.displayActive) teleporter.Teleport();
        }
        // Cancel TeleportProcess
        //if (Input.GetMouseButtonUp(0)) {
        //    teleporter.ToggleDisplay(false);
        //}
        dir += transform.right * (dirMul * rightHorizontal);
       
        str += strMul * rightVertical;
        // Move the target location to teleport
       
        if (allowNVGBkeys && Input.GetKey(KeyCode.N)) dir += transform.right * dirMul;   // Right
        if (allowNVGBkeys && Input.GetKey(KeyCode.V)) dir -= transform.right * dirMul;   // Left
        if (allowNVGBkeys && Input.GetKey(KeyCode.G)) str += strMul;                     // Forward
        if (allowNVGBkeys && Input.GetKey(KeyCode.B)) str -= strMul;                     // Backward
    }
}
