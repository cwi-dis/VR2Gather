using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

public class GamepadMoveCamera : MoveCamera
{
    public string xAxisName = "Gamepad_Axis_1";
    public string yAxisName = "Gamepad_Axis_2";
    public string heightAxisName = "Mouse ScrollWheel";
    
    // Update is called once per frame
    void Update()
    {
        // If axis names are specified we are (probably) using a gamepad
        float x = Input.GetAxis(xAxisName) * xySensitivity * Time.deltaTime;
        float y = -1 * Input.GetAxis(yAxisName) * xySensitivity * Time.deltaTime;
        xRotation += y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraToControl.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        adjustBodyHead(x, y);
        
    }
}
