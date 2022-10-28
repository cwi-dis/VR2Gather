using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEditor;

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
    public bool modeTouchingActive = false;
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
        if (modeTouchingActive)
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

    public void OnTouchingTouch()
    {
        if (!modeTouchingActive) return;
        Debug.Log($"Touched: {gropePosition}");
    }


    public void OnModeMoving(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeMovingActive = onOff;
        Debug.Log($"ModeMoving({onOff})");
        if (modeMovingActive)
        {
            modeTurningActive = modeTouchingActive = modeTeleportingActive = false;
        }
    }

    public void OnModeTurning(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeTurningActive = onOff;
        Debug.Log($"ModeTurning({onOff})");
        if (modeTurningActive)
        {
            modeMovingActive = modeTouchingActive = modeTeleportingActive = false;
        }
    }

    public void OnModeTouching(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeTouchingActive = onOff;
        Debug.Log($"ModeTouching({onOff})");
        gropePosition = virtualPosition;
        if (modeTouchingActive)
        {
            modeTurningActive = modeMovingActive = modeTeleportingActive = false;
        }
    }

    public void OnModeTeleporting(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeTeleportingActive = onOff;
        Debug.Log($"ModeTeleporting({onOff})");
        teleportPosition = virtualPosition;
        if (modeTeleportingActive)
        {
            modeTurningActive = modeMovingActive = modeTouchingActive = false;
        }
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
