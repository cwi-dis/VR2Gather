using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VRT.Core;

public class EnablePanelBasedOnControllers : MonoBehaviour
{
    [Tooltip("Panels to enable when using Oculus control scheme")]
    public GameObject[] oculus;
    [Tooltip("Panels to enable when using OpenXR control scheme")]
    public GameObject[] openxr;
    [Tooltip("Panels to enable when using Gamepad/Joystick control scheme")]
    public GameObject[] gamepad;
    [Tooltip("Panels to enable when using KeyboardMouse control scheme")]
    public GameObject[] emulator;
    [Header("Introspection (for debugging)")]
    [Tooltip("Current control scheme")]
    public string currentControlScheme;

    // Start is called before the first frame update
    void Start()
    {
        PlayerInput pi = GetComponentInParent<PlayerInput>();
        OnControlsChanged(pi);
    }

    public void OnControlsChanged(PlayerInput pi)
    {
        Debug.Log($"EnablePanelBasedOnControllers({gameObject.name}): OnControlsChanged({pi.name}): enabled={pi.enabled}, inputIsActive={pi.inputIsActive}, actionMap={pi.currentActionMap.name}, controlScheme={pi.currentControlScheme}");
        currentControlScheme = pi.currentControlScheme;
        bool isOculus = pi.currentControlScheme == "Oculus";
        bool isOpenXR = pi.currentControlScheme == "OpenXR";
        bool isEmulation = pi.currentControlScheme == "KeyboardMouse";
        bool isGamepad = pi.currentControlScheme == "Gamepad" || pi.currentControlScheme == "Joystick";
        foreach (var c in oculus) c.SetActive(isOculus);
        foreach (var c in openxr) c.SetActive(isOpenXR);
        foreach (var c in emulator) c.SetActive(isEmulation);
        foreach (var c in gamepad) c.SetActive(isGamepad);
    }
}
