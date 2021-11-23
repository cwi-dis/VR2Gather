using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public KeyOrAxis yes;
    public KeyOrAxis no;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
