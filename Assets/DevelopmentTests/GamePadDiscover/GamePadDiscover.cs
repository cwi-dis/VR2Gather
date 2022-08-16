using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public class GamePadDiscover : MonoBehaviour
{
    [System.Serializable]
    public class axisInfo
    {
        [Tooltip("InputSystem path of axis")]
        public string axisPath;
        [Tooltip("Current axis value")]
        public float axisValue;
    };
    [Tooltip("Axes to monitor")]
    public axisInfo[] axes;
    // Start is called before the first frame update
    [Tooltip("InputSystem Paths of buttons to monitor")]
    public string[] buttons;
    void Start()
    {
        Debug.Log($"XRSettings.enabled={XRSettings.enabled}");
        Debug.Log($"XRSettings.isDeviceActive={XRSettings.isDeviceActive}");
        Debug.Log($"XRSettings.loadedDeviceName={XRSettings.loadedDeviceName}");
#if ENABLE_INPUT_SYSTEM
        foreach(var d in InputSystem.devices)
        {
            Debug.Log($"InputSystem device: {d.path}");
        }
#else
        var names = Input.GetJoystickNames();
        foreach(var name in names)
        {
            Debug.Log($"Joystick or gamepad name: {name}");
        }
#endif
    }

    // Update is called once per frame
    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        foreach(Key keyCode in Enum.GetValues(typeof(Key)))
        {
            try
            {
                var key = (ButtonControl)Keyboard.current[keyCode];
                if (key.wasPressedThisFrame)
                {
                    Debug.Log($"Key down: '{key.name}' code {keyCode}");
                }
            }
            catch (ArgumentOutOfRangeException)
            {

            }
        }
        foreach(var buttonPath in buttons)
        {
            ButtonControl button = InputSystem.FindControl(buttonPath) as ButtonControl;
            if (button != null && button.wasPressedThisFrame)
            {
                Debug.Log($"Button down: {buttonPath}");
            }
        }
        foreach (var a in axes)
        {
            AxisControl axis = InputSystem.FindControl(a.axisPath) as AxisControl;
            if (axis != null)
            {
                float newValue = axis.ReadValue();
                if (newValue != a.axisValue)
                {
                    Debug.Log($"Axis {a.axisPath} = {newValue}");
                    a.axisValue = newValue;
                }
            }
        }
#else
        foreach(var a in axes)
        {
            if (a.axisName != "")
            {
                float newValue = Input.GetAxis(a.axisName);
                if (newValue != a.axisValue)
                {
                    //Debug.Log($"Axis {a.axisName} = {newValue}");
                    a.axisValue = newValue;
                }
            }
        }
     
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
            {
                Debug.Log("KeyCode down: " + kcode);
            }
        }
#endif
    }
}
