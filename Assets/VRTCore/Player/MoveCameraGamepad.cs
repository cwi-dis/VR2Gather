using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

public class MoveCameraGamepad : MoveCamera
{
    public string leftRightAxisName = "Gamepad_Axis_1";
    public string upDownAxisName = "Gamepad_Axis_2";
    public string heightAxisName = "Gamepad_Axis_6";
    public new const bool allowHJKLforMouse = false;

    // Update is called once per frame
    void Update()
    {
        if (heightAxisName != "")
        {
            float deltaHeight = Input.GetAxis(heightAxisName);
            if (deltaHeight != 0)
            {
                // Do Camera movement for up/down.
                cameraToControl.transform.localPosition = new Vector3(
                    cameraToControl.transform.localPosition.x,
                    cameraToControl.transform.localPosition.y - deltaHeight * heightSensitivity,
                    cameraToControl.transform.localPosition.z);
            }
        }
        float x = Input.GetAxis(leftRightAxisName) * xySensitivity * Time.deltaTime;
        float y = -1 * Input.GetAxis(upDownAxisName) * xySensitivity * Time.deltaTime;
        xRotation += y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraToControl.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        adjustBodyHead(x, y);
        
    }
}
