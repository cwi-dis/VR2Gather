using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicroRecorder : MonoBehaviour
{
    string device;
    int currentMinFreq, currentMaxFreq;
    AudioClip recorder;
    // Start is called before the first frame update
    void Start() {
        if (Microphone.devices.Length > 0) {
            device = Microphone.devices[0];
            Microphone.GetDeviceCaps(device, out currentMinFreq, out currentMaxFreq);
            recorder = Microphone.Start(device, true, 2, currentMaxFreq);
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = recorder;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log($"Using {device}:{currentMaxFreq}");
        }
        else
            Debug.LogError("No Micros detected.");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
