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

public class InputSystemHandling : MonoBehaviour
{
    [Tooltip("The character controller for the thing to be moved")]
    public CharacterController controller;
    [Tooltip("How fast moves go")]
    public float moveSpeed = 1f;


    Vector2 oldMousePosition;

    [Tooltip("xxxjack Turning destination")]
    public Vector2 turnPosition = new Vector2(0, 0);
    [Tooltip("xxxjack Teleport destination")]
    public Vector2 teleportPosition = new Vector2(0, 0);
    [Tooltip("xxxjack Grope destination")]
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
    
            Vector3 move = transform.right * delta.x + transform.forward * delta.y;
            move = move * moveSpeed * Time.deltaTime;
            Debug.Log($"InputSystemHandling: move {move}");
            controller.Move(move);
        }
        if (modeTurningActive)
        {
            turnPosition += delta;
            Debug.Log($"OnDelta: Turn({delta}) to {turnPosition}");
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
        Debug.Log($"InputSystemHandling: Teleported to {teleportPosition}");
    }

    public void OnTeleportHome()
    {
        if (!modeTeleportingActive) return;
        Debug.Log($"InputSystemHandling: Teleported to home");
    }

    public void OnGropingTouch()
    {
        if (!modeGropingActive) return;
        Debug.Log($"InputSystemHandling: Touched: {gropePosition}");
    }


    public void OnModeMoving(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeMovingActive = onOff;
        Debug.Log($"InputSystemHandling: ModeMoving({onOff})");
        if (modeMovingActive)
        {
            modeTurningActive = modeGropingActive = modeTeleportingActive = false;
        }
    }

    public void OnModeTurning(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeTurningActive = onOff;
        Debug.Log($"InputSystemHandling: ModeTurning({onOff})");
        if (modeTurningActive)
        {
            modeMovingActive = modeGropingActive = modeTeleportingActive = false;
        }
    }

    public void OnModeGroping(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeGropingActive = onOff;
        Debug.Log($"InputSystemHandling: ModeGroping({onOff})");
        gropePosition = new Vector2(0,0);
        if (modeGropingActive)
        {
            modeTurningActive = modeMovingActive = modeTeleportingActive = false;
        }
    }

    public void OnModeTeleporting(InputValue value)
    {
        bool onOff = value.Get<float>() != 0;
        modeTeleportingActive = onOff;
        Debug.Log($"ModeTeleporting({onOff})");
        teleportPosition = new Vector2(0,0);
        if (modeTeleportingActive)
        {
            modeTurningActive = modeMovingActive = modeGropingActive = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
