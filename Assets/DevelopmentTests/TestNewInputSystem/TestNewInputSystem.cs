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
    [Tooltip("Virtual position")]
    public Vector2 virtualPosition = new Vector2(0, 0);
    [Tooltip("Turning destination")]
    public Vector2 turnPosition = new Vector2(0, 0);
    [Tooltip("Teleport destination")]
    public Vector2 teleportPosition = new Vector2(0, 0);
    [Tooltip("Grope destination")]
    public Vector2 gropePosition = new Vector2(0, 0);

    public bool modeMovingActive = false;
    public bool modeTurningActive = false;
    public bool modeGropingActive = false;
    public bool modeTeleportingActive = false;

    void Start()
    {
 
    }

  
    public void OnDelta(InputValue value)
    {
        Vector2 delta = value.Get<Vector2>();
        if (modeMovingActive)
        {
            virtualPosition += delta;
            Debug.Log($"OnDelta: Move({delta}) to {virtualPosition}");
        }
        if (modeTurningActive)
        {
            turnPosition += delta;
            Debug.Log($"OnDelta: Turn({delta}) to {virtualPosition}");
        }
        if (modeGropingActive)
        {
            gropePosition += delta;
        }
        if (modeTeleportingActive)
        {
            teleportPosition += delta;
        }
    }

    public void OnTeleportGo()
    {
        if (!modeTeleportingActive) return;
        virtualPosition = teleportPosition;
        Debug.Log($"Teleported to {virtualPosition}");
    }

    public void OnTeleportHome()
    {
        if (!modeTeleportingActive) return;
        virtualPosition = new Vector2(0, 0);
        Debug.Log($"Teleported to {virtualPosition}");
    }

    public void OnGropingTouch()
    {
        if (!modeGropingActive) return;
        Debug.Log($"Touched: {gropePosition}");
    }


    public void OnModeMoving(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeMovingActive = onOff;
        Debug.Log($"ModeMoving({onOff})");
    }

    public void OnModeTurning(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeTurningActive = onOff;
        Debug.Log($"ModeTurning({onOff})");
    }

    public void OnModeGroping(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeGropingActive = onOff;
        Debug.Log($"ModeGroping({onOff})");
        gropePosition = virtualPosition;
    }

    public void OnModeTeleporting(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeTeleportingActive = onOff;
        Debug.Log($"ModeTeleporting({onOff})");
        teleportPosition = virtualPosition;
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
