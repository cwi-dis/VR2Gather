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
    float[] buffer;
    VoiceSender sender;

    // Start is called before the first frame update
    void Start() {
        if (Microphone.devices.Length > 0) {
            device = Microphone.devices[0];
            int currentMinFreq;
            Microphone.GetDeviceCaps(device, out currentMinFreq, out currentFrequency);
            recorder = Microphone.Start(device, true, 1, currentFrequency);
            currentFrequency = recorder.frequency;
            bufferLength = 4096;
            buffer = new float[bufferLength];
            Debug.Log($"Using {device}  Frequency {currentFrequency} bufferLength {bufferLength}");
        }
        else
            Debug.LogError("No Micros detected.");

        sender = new VoiceSender(1, SocketIOServer.Instance);
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


        if (available> bufferLength) {
            recorder.GetData(buffer, lastPosition);
//            for (int i = 0; i < buffer.Length; ++i)
//                buffer[i] = Random.value * 2 - 1;
            sender.Send(buffer);
            lastPosition = (lastPosition + bufferLength) % currentFrequency;
        }
    }

    void OnDestroy()
    {
        SocketIOServer.Instance.Close();
    }
}
