using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

public class GamepadMoveCamera : MoveCamera
{
    public string xAxisName = "Mouse X";
    public string yAxisName = "Mouse Y";
    public string heightAxisName = "Mouse ScrollWheel";
    
    // Update is called once per frame
    void Update()
    {
        // If axis names are specified we are (probably) using a gamepad
        float mouseX = Input.GetAxis(xAxisName) * xySensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis(yAxisName) * xySensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraToControl.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        adjustBodyHead(mouseX, -mouseY);
        
    }
}
