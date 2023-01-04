using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEditor;

// This InputProcessor should negate values, i.e. a range 0..1 will be mapped to 1..0.
// This is different from inverting (which maps -1..1 to 1..-1).
// But it doesn't work...
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class NegateProcessor : InputProcessor<float>
{

#if UNITY_EDITOR
    static NegateProcessor()
    {
        Initialize();
    }
#endif

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        InputSystem.RegisterProcessor<NegateProcessor>();
    }

    public override float Process(float value, InputControl control)
    {
        string name = "no control";
        if (control != null) name = control.name;
        return 1-value;
    }
}
