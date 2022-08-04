using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CalibrationControls : MonoBehaviour
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
    //private Array values;

    // Start is called before the first frame update
    void Start()
    {
        //values = Enum.GetValues(typeof(KeyCode));
    }

    // Update is called once per frame
    void Update()
    {
        ////code to know hey pressed
        //foreach (KeyCode kcode in values)
        //{
        //    if (Input.GetKey(kcode))
        //        Debug.Log("KeyCode down: " + kcode);
        //}
    }
}
