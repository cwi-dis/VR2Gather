using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

//
// Note to self (or others):
//
// There is an input debugger (for the new InputSystem) under
// Window->Analysis->Input Debugger. Double-click a device and you will
// see all the events it outputs, axis values, etc.
//
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
    [Tooltip("This will enable (many) messages on control of the wrong type")]
    public bool messageOnBadControl = false;
    void Start()
    {
        Debug.Log($"XRSettings.enabled={XRSettings.enabled}");
        Debug.Log($"XRSettings.isDeviceActive={XRSettings.isDeviceActive}");
        Debug.Log($"XRSettings.loadedDeviceName={XRSettings.loadedDeviceName}");
        foreach(var d in InputSystem.devices)
        {
            Debug.Log($"InputSystem device: {d.path}");
        }
        if (Keyboard.current != null) Debug.Log($"Keyboard: {Keyboard.current.path}");
        if (Mouse.current != null) Debug.Log($"Mouse: {Mouse.current.path}");
        if (Gamepad.current != null) Debug.Log($"Gamepad: {Gamepad.current.path}");
        if (Joystick.current != null) Debug.Log($"Joystick: {Joystick.current.path}");
    }

    // Update is called once per frame
    void Update()
    {
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
            using (var buttons = InputSystem.FindControls(buttonPath))
                foreach (var _button in buttons)
                {
                    ButtonControl button = _button as ButtonControl;
                    if (button == null)
                    {
                        if (messageOnBadControl) Debug.Log($"Not a ButtonControl: {_button.path}");
                        continue;
                    }
                    if (button != null && button.wasPressedThisFrame)
                    {
                        Debug.Log($"Button down: {buttonPath}: {button.path}");
                    }
                }
        }
        foreach (var a in axes)
        {
            using (var axes = InputSystem.FindControls(a.axisPath))
                foreach (var _axis in axes)
                {
                    AxisControl axis = _axis as AxisControl;
                    if (axis == null)
                    {
                        if (messageOnBadControl) Debug.Log($"Not an AxisControl: {_axis.path}");
                        continue;

                    }
                    if (axis != null)
                    {
                        float newValue = axis.ReadValue();
                        if (newValue != a.axisValue)
                        {
                            Debug.Log($"Axis {a.axisPath}: {axis.path} = {newValue}");
                            a.axisValue = newValue;
                        }
                    }
                }
        }
    }
}
