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

    [Tooltip("The camera attached to the head that turns (Usually found automatically)")]
    public Transform cameraTransformToControl = null;
    [Tooltip("The player body that turns horizontally")]
    public Transform playerBody;
    [Tooltip("The player head that tilts")]
    public Transform avatarHead;
    [Tooltip("How fast the viewpoint turns")]
    public float xySensitivity = 1;
    [Tooltip("How fast the viewpoint moves up/down")]
    public float heightSensitivity = 1; // 5 Centimeters

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

    private void Awake()
    {
        if (cameraTransformToControl != null) return;
        PlayerManager player = GetComponentInParent<PlayerManager>();
        cameraTransformToControl = player.getCameraTransform();
    }

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
            float xRotation = delta.x * xySensitivity * Time.deltaTime;
            float yRotation = delta.y * xySensitivity * Time.deltaTime;

            Debug.Log($"OnDelta: Turn({delta}) to {turnPosition}");
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            cameraTransformToControl.localRotation = cameraTransformToControl.localRotation*Quaternion.Euler(xRotation, 0f, 0f);
            adjustBodyHead(xRotation, -yRotation);
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

    public void OnHeightDelta(InputValue value)
    {
        float deltaHeight = value.Get<float>();

        // Note by Jack: spectators and no-representation users should be able to move their viewpoint up and down.
        // with the current implementation all users have this ability, which may or may not be a good idea.
        if (deltaHeight != 0)
        {
            // Do Camera movement for up/down.
            cameraTransformToControl.localPosition = new Vector3(
                cameraTransformToControl.localPosition.x,
                cameraTransformToControl.localPosition.y + deltaHeight * heightSensitivity,
                cameraTransformToControl.localPosition.z);
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

    protected void adjustBodyHead(float hAngle, float vAngle)
    {
        playerBody.Rotate(Vector3.up, hAngle);
        avatarHead.Rotate(Vector3.right, vAngle);
    }

}
