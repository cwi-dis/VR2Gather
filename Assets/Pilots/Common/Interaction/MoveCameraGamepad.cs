﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{
    public class MoveCameraGamepad : MoveCamera
    {
        public string leftRightAxisName = "Gamepad_Axis_1";
        public string upDownAxisName = "Gamepad_Axis_2";
        public string heightAxisName = "Gamepad_Axis_6";
        public new const bool allowHJKLforMouse = false;

        // Update is called once per frame
        void Update()
        {
            foreach (var inhibitKey in inhibitKeys)
            {
                if (inhibitKey != KeyCode.None && Input.GetKey(inhibitKey))
                {
                    return;
                }
            }
            if (heightAxisName != "")
            {
                float deltaHeight = 0;
                if (heightAxisName != "") deltaHeight = Input.GetAxis(heightAxisName);
                if (deltaHeight != 0)
                {
                    // Do Camera movement for up/down.
                    cameraTransformToControl.localPosition = new Vector3(
                        cameraTransformToControl.localPosition.x,
                        cameraTransformToControl.localPosition.y - deltaHeight * heightSensitivity,
                        cameraTransformToControl.localPosition.z);
                }
            }
            float x = 0;
            if (leftRightAxisName != "") x = Input.GetAxis(leftRightAxisName) * xySensitivity * Time.deltaTime;
            float y = 0;
            if (upDownAxisName != "") y = -1 * Input.GetAxis(upDownAxisName) * xySensitivity * Time.deltaTime;
            xRotation += y;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            cameraTransformToControl.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            adjustBodyHead(x, y);

        }
    }
}