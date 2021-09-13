using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using VRT.Teleporter;

public class LeftHandVRTeleporterController : MonoBehaviour
{
    

    public VRTeleporter teleporter;
    private Vector3 dir;
    private float str = 7.0f;
    private float dirMul = 0.01f;
    private float strMul = 0.05f;


    float rightTrigger = 0.0f;

    bool rightTriggerTwo = false;
    bool previousRightTriggerTwo = false;

    float leftTrigger = 0.0f;
    float rightHorizontal = 0.0f;
    float rightVertical = 0.0f;

    private float previousRightTrigger = 0.0f;

    private void Awake()
    {
        //if (SceneManager.GetActiveScene().name != "Museum") gameObject.SetActive(false);
    }

    void Update() {

        rightTrigger = Input.GetAxisRaw("PrimaryTriggerRight");
        
        leftTrigger = Input.GetAxisRaw("PrimaryTriggerLeft");
        rightVertical = Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical");
        rightHorizontal = Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");

        // try to enable both 'A' button and joystick for teleporation and to see which one is better
        rightTriggerTwo = Input.GetKey(KeyCode.JoystickButton3);

        /*if (IsDownRightTrigger || IsDownLeftTrigger)
        {
            Debug.Log("TRIGGER!");
        }*/


        if (teleporter.displayActive) {
            teleporter.CustomUpdatePath(dir, str);
        }


        // Start TeleportProcess

        //if (Input.GetMouseButtonDown(0) || rightTrigger >= 0.8f)
        //if (rightTrigger >= 0.8f)

        if (rightTriggerTwo)
        {
            teleporter.ToggleDisplay(true);
            dir = transform.forward;
            str = 7.0f;
            //previousRightTrigger = rightTrigger;
            previousRightTriggerTwo = rightTriggerTwo;
        }





        // Confirm TeleportProcess

        //if (Input.GetMouseButtonDown(1) || leftTrigger >= 0.8f)
        //if (previousRightTrigger>0.0f && rightTrigger<=0.0f) {

        if (previousRightTriggerTwo && !rightTriggerTwo) { 
            if (teleporter.displayActive) teleporter.Teleport();
        }

        //previousRightTrigger = 0.0f;




        // Cancel TeleportProcess
        //if (leftTrigger >= 0.3f)
        //{
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
