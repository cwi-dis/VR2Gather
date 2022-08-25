using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using VRT.Teleporter;

public class KeyboardVRTeleporterController : MonoBehaviour
{
    

    public BaseTeleporter teleporter;
    public bool allowPaths = false;
    public string rightTriggerPath = "";
    public string leftTriggerPath = "";
    public string rightVerticalPath = "";
    public string rightHorizontalPath = "";
    public bool allowNVGBkeys = true;
    public bool allowMouse = true;

    private Vector3 dir;
    private float str = 7.0f;
    private float dirMul = 0.01f;
    private float strMul = 0.05f;


    float rightHorizontal = 0.0f;
    float rightVertical = 0.0f;

    private void Awake()
    {
    }

    void Update() {

        bool rightTrigger = false;
        bool leftTrigger = false;
        if (allowPaths)
        {
            InputControl c = InputSystem.FindControl(rightTriggerPath);
            if (c != null) rightTrigger = ((ButtonControl)c).wasPressedThisFrame;
            c = InputSystem.FindControl(leftTriggerPath);
            if (c != null) leftTrigger = ((ButtonControl)c).wasPressedThisFrame;
            c = InputSystem.FindControl(rightVerticalPath);
            if (c != null) rightVertical = ((AxisControl)c).ReadValue();
            c = InputSystem.FindControl(rightHorizontalPath);
            if (c != null) rightHorizontal = ((AxisControl)c).ReadValue();

        }
        /*if (IsDownRightTrigger || IsDownLeftTrigger)
        {
            Debug.Log("TRIGGER!");
        }*/


        if (teleporter.teleporterActive) {
            teleporter.CustomUpdatePath(null, dir, str);
        }


        // Start TeleportProcess
        if ((allowMouse && Mouse.current.leftButton.wasPressedThisFrame) || rightTrigger)
        {
            Debug.Log("Teleport: started");
            teleporter.SetActive(true);
            dir = transform.forward;
            str = 7.0f;
        }
        if ((allowMouse && Mouse.current.leftButton.wasReleasedThisFrame) || rightTrigger)
        {
            Debug.Log("Teleport: aborted");
            teleporter.SetActive(true);
            dir = transform.forward;
            str = 7.0f;
        }
        // Confirm TeleportProcess
        if ((allowMouse && Mouse.current.rightButton.wasPressedThisFrame) || leftTrigger) {
            Debug.Log("Teleport: teleport");
            if (teleporter.teleporterActive)
            {
                teleporter.Teleport();
            }
            teleporter.SetActive(false);
        }
        dir += transform.right * (dirMul * rightHorizontal);
       
        str += strMul * rightVertical;
        // Move the target location to teleport
       
        if (allowNVGBkeys && Keyboard.current.nKey.wasPressedThisFrame) dir += transform.right * dirMul;   // Right
        if (allowNVGBkeys && Keyboard.current.vKey.wasPressedThisFrame) dir -= transform.right * dirMul;   // Left
        if (allowNVGBkeys && Keyboard.current.gKey.wasPressedThisFrame) str += strMul;                     // Forward
        if (allowNVGBkeys && Keyboard.current.bKey.wasPressedThisFrame) str -= strMul;                     // Backward
        if (rightHorizontal != 0 || rightVertical != 0 || (allowNVGBkeys && Keyboard.current.anyKey.wasPressedThisFrame))
        {
            Debug.Log($"Teleport: now destination is {dir}, {str}");
        }
    }
}
