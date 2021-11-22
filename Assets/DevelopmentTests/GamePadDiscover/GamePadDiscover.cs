using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;

public class GamePadDiscover : MonoBehaviour
{
    public string axisName = "";
    public float axisValue = 0;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"XRSettings.enabled={XRSettings.enabled}");
        Debug.Log($"XRSettings.isDeviceActive={XRSettings.isDeviceActive}");
        Debug.Log($"XRSettings.loadedDeviceName={XRSettings.loadedDeviceName}");
        var names = Input.GetJoystickNames();
        foreach(var name in names)
        {
            Debug.Log($"Joystick or gamepad name: {name}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (axisName != "")
        {
            float newValue = Input.GetAxis(axisName);
            if (newValue != axisValue)
            {
                Debug.Log($"Axis {axisName} = {newValue}");
                axisValue = newValue;
            }
        }
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
                Debug.Log("KeyCode down: " + kcode);
        }
    }
}
