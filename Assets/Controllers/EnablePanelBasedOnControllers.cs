using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

public class EnablePanelBasedOnControllers : MonoBehaviour
{
    [Tooltip("Panels to enable when using Oculus controllers")]
    public GameObject[] oculus;
    [Tooltip("Panels to enable when using Vive controllers")]
    public GameObject[] vive;
    [Tooltip("Panels to enable when using gamepad controller")]
    public GameObject[] gamepad;
    [Tooltip("Panels to enable when using keyboard/mouse controller emulator")]
    public GameObject[] emulator;

    
    // Start is called before the first frame update
    void Start()
    {
        bool isOculus = false;
        bool isVive = false;
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
                isVive = true;
            }
        }
        foreach (var c in oculus) c.SetActive(isOculus);
        foreach (var c in vive) c.SetActive(isVive);
        foreach (var c in emulator) c.SetActive(isEmulation);
        foreach (var c in gamepad) c.SetActive(isGamepad);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
