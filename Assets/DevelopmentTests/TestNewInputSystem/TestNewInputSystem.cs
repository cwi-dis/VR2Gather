using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TestNewInputSystem : MonoBehaviour
{
    Vector2 oldMousePosition;

     void Start()
    {
 
    }

    public void OnMove()
    {
        
    }

    public void OnTeleport()
    {

    }

    public void OnFoobar()
    {

    }

    // Update is called once per frame
    void Update()
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
