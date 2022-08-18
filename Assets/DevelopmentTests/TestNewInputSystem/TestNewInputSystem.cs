using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEditor;

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
        return 1-value;
    }
}

public class TestNewInputSystem : MonoBehaviour
{
    Vector2 oldMousePosition;

    [Tooltip("Show key presses and mouse moves")]
    public bool showKeyboardMouse = false;
    [Tooltip("Virtual position after move/teleport")]
    public Vector2 virtualPosition = new Vector2(0, 0);

    public bool moveModeActive = false;
    public bool foobarModeActive = false;

    void Start()
    {
 
    }

  
    public void OnDelta(InputValue value)
    {
        Vector2 delta = value.Get<Vector2>();
        if (moveModeActive)
        {
            virtualPosition += delta;
            Debug.Log($"OnDelta: Move({delta}) to {virtualPosition}");
        }
        if (foobarModeActive)
        {
            Debug.Log($"OnDelta: Foobar({delta})");
        }
    }

    public void OnTeleport()
    {
        virtualPosition = new Vector2(0, 0);
        Debug.Log($"Teleport to {virtualPosition}");
    }

    public void OnFoobarMode(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        foobarModeActive = onOff;
        Debug.Log($"FoobarMode({onOff})");
    }

    public void OnMoveMode(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        moveModeActive = onOff;
        Debug.Log($"MoveMode({onOff})");
    }


    // Update is called once per frame
    void Update()
    {
        if(showKeyboardMouse)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            if (mousePosition != oldMousePosition)
            {
                Debug.Log($"Mouse was moved from {oldMousePosition} to {mousePosition}");
                oldMousePosition = mousePosition;
            }
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                Debug.Log("A key was pressed");
            }
        }
    }
}
