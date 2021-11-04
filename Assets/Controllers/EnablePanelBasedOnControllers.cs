using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnablePanelBasedOnControllers : MonoBehaviour
{
    public GameObject LControllerOculus;
    public GameObject RControllerOculus;
    public GameObject LControllerVive;
    public GameObject RControllerVive;
    bool isOculus = true; // we take oculus as default device

    // Start is called before the first frame update
    void Start()
    {
        var inputDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(inputDevices);
        foreach (var device in inputDevices)
        {
            if (device.manufacturer == "HTC")
            {
                isOculus = false;
            }
        }// if not HTC then we assume Oculus

        LControllerOculus.SetActive(isOculus);
        RControllerOculus.SetActive(isOculus);
        LControllerVive.SetActive(!isOculus);
        RControllerVive.SetActive(!isOculus);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
