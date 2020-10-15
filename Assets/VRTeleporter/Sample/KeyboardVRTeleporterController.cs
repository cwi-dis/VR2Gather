using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;


public class KeyboardVRTeleporterController : MonoBehaviour
{
    

    public VRTeleporter teleporter;
    private Vector3 dir;
    private float str = 7.0f;
    private float dirMul = 0.01f;
    private float strMul = 0.05f;


    float rightTrigger = 0.0f;
    float leftTrigger = 0.0f;
    float rightHorizontal = 0.0f;
    float rightVertical = 0.0f;

    void Update() {

        rightTrigger = Input.GetAxisRaw("PrimaryTriggerRight");
        leftTrigger = Input.GetAxisRaw("PrimaryTriggerLeft");
        rightVertical = Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical");
        rightHorizontal = Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");

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
       
        if (Input.GetKey(KeyCode.N)) dir += transform.right * dirMul;   // Right
        if (Input.GetKey(KeyCode.V)) dir -= transform.right * dirMul;   // Left
        if (Input.GetKey(KeyCode.G)) str += strMul;                     // Forward
        if (Input.GetKey(KeyCode.B)) str -= strMul;                     // Backward
    }
}
