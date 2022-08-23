using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CalibrationInteraction : MonoBehaviour
{
    [System.Serializable]
    public class KeyOrAxis
    {
        public string axis = "";
        public bool invertAxis = false;
        public KeyCode key = KeyCode.None;
        private bool prevAxisActive = false;
        public bool get()
        {
            if (key != KeyCode.None) return Input.GetKeyDown(key);
            if (axis == "") return false;
            bool axisActive = Input.GetAxisRaw(axis) >= 0.5f;
            bool rv = axisActive && !prevAxisActive;
            prevAxisActive = axisActive;
            if (invertAxis) rv = !rv;
            return rv;
        }
    }
    [System.Serializable]
    public class AxisOrTwoKeys
    {
        public string axis = "";
        public bool invertAxis = false;
        public KeyCode keyDecrease = KeyCode.None;
        public KeyCode keyIncrease = KeyCode.None;
        public float get()
        {
            if (axis != "")
            {
                var rv = Input.GetAxis(axis);
                if (invertAxis) rv = -rv;
                return rv;
            }
            if (Input.GetKeyDown(keyDecrease)) return -1;
            if (Input.GetKeyDown(keyIncrease)) return 1;
            return 0;
        }
    }
    public KeyOrAxis yes;
    public KeyOrAxis no;
    public KeyOrAxis done;
    public KeyOrAxis translate;
    public KeyOrAxis rotate;
    public KeyOrAxis reset;

    public AxisOrTwoKeys backwardForward;
    public AxisOrTwoKeys leftRight;
    public AxisOrTwoKeys downUp;

    public void OnYes()
    {
        Debug.Log($"CalibrationControls: OnYes");
    }
    public void OnNo()
    {
        Debug.Log($"CalibrationControls: OnNo");
    }
    public void OnTranslate()
    {
        Debug.Log($"CalibrationControls: OnTranslate");
    }
    public void OnRotate()
    {
        Debug.Log($"CalibrationControls: OnRotate");

    }
    public void OnDone()
    {
        Debug.Log($"CalibrationControls: OnDone");

    }
    public void OnReset()
    {
        Debug.Log($"CalibrationControls: OnReset");
    }
    public void OnBackwardForward(InputValue value)
    {
        var delta = value.Get<float>();
        Debug.Log($"CalibrationControls: OnBackwardForward: {delta}");

    }
    public void OnLeftRight(InputValue value)
    {
        var delta = value.Get<float>();
        Debug.Log($"CalibrationControls: OnLeftRight: {delta}");

    }
    public void OnUpDown(InputValue value)
    {
        var delta = value.Get<float>();
        Debug.Log($"CalibrationControls: OnUpDown: {delta}");

    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
