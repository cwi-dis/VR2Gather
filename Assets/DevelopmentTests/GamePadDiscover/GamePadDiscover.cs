using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;

public class GamePadDiscover : MonoBehaviour
{
    [System.Serializable]
    public class axisInfo
    {
        [Tooltip("Name of axis")]
        public string axisName;
        [Tooltip("Current axis value")]
        public float axisValue;
    };
    [Tooltip("Axes to monitor")]
    public axisInfo[] axes;
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
        foreach(var a in axes)
        {
            if (a.axisName != "")
            {
                float newValue = Input.GetAxis(a.axisName);
                if (newValue != a.axisValue)
                {
                    Debug.Log($"Axis {a.axisName} = {newValue}");
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
    }
}
