using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

public class EnablePanelBasedOnControllers : MonoBehaviour
{
    [Tooltip("Panels to enable when using Oculus controllers")]
    public GameObject[] oculus;
    [Tooltip("Panels to enable when using OpenVR controllers")]
    public GameObject[] openvr;
    [Tooltip("Panels to enable when using gamepad controller")]
    public GameObject[] gamepad;
    [Tooltip("Panels to enable when using keyboard/mouse controller emulator")]
    public GameObject[] emulator;


    // Start is called before the first frame update
    void Start()
    {
        InitializeXRDevices();
    }

    private void InitializeXRDevices()
    {
        bool isOculus = VRConfig.Instance.useControllerOculus();
        bool isOpenVR = VRConfig.Instance.useControllerOpenVR();
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
                isOpenVR = true;
            }
        }
        foreach (var c in oculus) c.SetActive(isOculus);
        foreach (var c in openvr) c.SetActive(isOpenVR);
        foreach (var c in emulator) c.SetActive(isEmulation);
        foreach (var c in gamepad) c.SetActive(isGamepad);
    }

}
