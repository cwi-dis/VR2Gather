using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Wacki;
using System;
using Vive.Plugin.SR.Experience;

public class PlayerHandUILaserPointer : IUILaserPointer {

    public EVRButtonId button = EVRButtonId.k_EButton_SteamVR_Trigger;

    Hand hand;

    private bool _connected = false;

    public static MeshRenderer pointer_rnd, hitPoint_rnd;

    public static Color Color_Hit_Default, Color_Normal_Default, Color_Hit, Color_Normal;

    public static PlayerHandUILaserPointer LaserPointer;

    float soundPlayedTime;

    bool isTriggerDown;

    public static void CreateLaserPointer()
    {
        LaserPointer = new GameObject("LaserPointer").AddComponent<PlayerHandUILaserPointer>();
        LaserPointer.transform.SetParent(ViveSR_Experience.instance.AttachPoint.transform);
        LaserPointer.transform.localPosition = Vector3.zero;
        LaserPointer.transform.localEulerAngles = new Vector3(-60f, 0f, 0f);
        LaserPointer.color = Color.white;
    }

    public static void EnableLaserPointer(bool isOn)
    {
        if (LaserPointer == null) CreateLaserPointer();
        LaserPointer.gameObject.SetActive(isOn);
    }

    protected override void Initialize()
    {
        base.Initialize();

        Color_Hit_Default = Color_Hit = new Color(0.75f, 0.917f, 1f, 1f);
        Color_Normal_Default = Color_Normal = Color.white;

        hand = ViveSR_Experience.instance.targetHand;
        _connected = true;
    }

    public static void ResetColors()
    {
        Color_Hit = Color_Hit_Default;
        Color_Normal = Color_Normal_Default;
    }

    public static void SetColors(Color hit, Color normal)
    {
        Color_Hit = hit;
        Color_Normal = normal;
    }

    public override bool ButtonDown()
    {     
        if(!_connected)
            return false;

        if (hand.isPoseValid && ViveSR_Experience_ControllerDelegate.ViveControlInputs[ViveControlType.Trigger].actionClick.GetStateDown(hand.handType))
        {
            isTriggerDown = true;
            return true;
        }

        return false;
    }

    public override bool ButtonUp()
    {
        if(!_connected)
            return false;

        if (hand.isPoseValid && ViveSR_Experience_ControllerDelegate.ViveControlInputs[ViveControlType.Trigger].actionClick.GetStateUp(hand.handType))
        {
            isTriggerDown = false;
            return true;
        }

        return false;
    }

    public override void OnEnterControl(GameObject control)
    {
        if (!_connected)
            return;

        if (control.name.Contains("Panel")) return;

        if (pointer_rnd == null) pointer_rnd = pointer.GetComponent<MeshRenderer>();
        if (hitPoint_rnd == null) hitPoint_rnd = hitPoint.GetComponent<MeshRenderer>();
        pointer_rnd.material.SetColor("_Color", Color_Hit);
        hitPoint_rnd.material.SetColor("_Color", Color_Hit);

        if (Time.timeSinceLevelLoad - soundPlayedTime > 0.1f && !isTriggerDown)
        {
            soundPlayedTime = Time.timeSinceLevelLoad;
        }

    }

    public override void OnExitControl(GameObject control)
    {
        if (!_connected)
            return;

        if (control.name.Contains("Panel")) return;

        pointer_rnd.material.SetColor("_Color", Color_Normal);
        hitPoint_rnd.material.SetColor("_Color", Color_Normal);
    }

    int controllerIndex
    {
        get {
            if (!_connected) return 0;
            return Array.IndexOf(Player.instance.hands, hand) + 2;
        }
    }
}