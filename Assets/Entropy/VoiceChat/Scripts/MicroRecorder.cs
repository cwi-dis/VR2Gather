using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicroRecorder : MonoBehaviour
{
    public int fps = 30;
    string device;
    int currentFrequency;
    int bufferLength;
    AudioClip recorder;
    // Start is called before the first frame update
    void Start() {
        if (Microphone.devices.Length > 0) {
            device = Microphone.devices[0];
            int currentMinFreq;
            Microphone.GetDeviceCaps(device, out currentMinFreq, out currentFrequency);
            recorder = Microphone.Start(device, true, 1, currentFrequency);
            currentFrequency = recorder.frequency;
            bufferLength = (int)(currentFrequency / fps);
            Debug.Log($"Using {device}  Frequency {currentFrequency} bufferLength {bufferLength}");
        }
        else
            Debug.LogError("No Micros detected.");
    }

    int lastPosition=0;
    // Update is called once per frame
    void Update() {
        int currentPostion = Microphone.GetPosition(device);
        int available;
        if (currentPostion<lastPosition ) {
            // Loop!
            available = currentPostion + (currentFrequency - lastPosition);
        }else
            available =  currentPostion- lastPosition;


        if (available> bufferLength)
        {
            Debug.Log($"Buffer !!!! (rest: {(available-bufferLength)})");
            lastPosition = (lastPosition + bufferLength) % currentFrequency;
        }

    }
}
