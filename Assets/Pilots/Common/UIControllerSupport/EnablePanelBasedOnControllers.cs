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

#if xxxjack_not
    private void InitializeXRDevices()
    {
        bool isOculus = VRConfig.Instance.useControllerOculus();
        bool isOpenXR = VRConfig.Instance.useControllerOpenXR();
        bool isEmulation = VRConfig.Instance.useControllerEmulation();
        bool isGamepad = VRConfig.Instance.useControllerGamepad();

        if (!isEmulation && !isGamepad)
        {
            // xxxjack this code needs to move to VRConfig.
            var inputDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevices(inputDevices);
            foreach (var device in inputDevices)
            {
                if (device.manufacturer == "HTC")
                {
                    isOculus = false;
                }
            }
            // if not HTC then we assume Oculus xxxjack hack
            if (!isOculus)
            {
                isOpenXR = true;
            }
        }
        foreach (var c in oculus) c.SetActive(isOculus);
        foreach (var c in openxr) c.SetActive(isOpenXR);
        foreach (var c in emulator) c.SetActive(isEmulation);
        foreach (var c in gamepad) c.SetActive(isGamepad);
    }
#endif
}
