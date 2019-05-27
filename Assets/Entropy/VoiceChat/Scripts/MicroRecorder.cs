using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicroRecorder : MonoBehaviour{
    public bool useEcho=false;
    string device;
    int samples;
    int bufferLength;
    AudioClip recorder;
    float[] buffer;
    VoiceSender sender;


    // Start is called before the first frame update
    void Start() {
        NTPTools.GetNetworkTime();
        Init();
    }


    public void Init() {
        if (Microphone.devices.Length > 0) {
            device = Microphone.devices[0];
            int currentMinFreq;
            Microphone.GetDeviceCaps(device, out currentMinFreq, out samples);
            samples = 11025 * 2;
            recorder = Microphone.Start(device, true, 1, samples);
            samples = recorder.samples;

            bufferLength = 512;
            buffer = new float[bufferLength];
            Debug.Log($"Using {device}  Frequency {samples} bufferLength {bufferLength} {samples}");
        }
        else
            Debug.LogError("No Micros detected.");

        sender = new VoiceSender((ushort)samples);

    }

    int readPosition=0;
    // Update is called once per frame
    void Update() {
        int writePosition = Microphone.GetPosition(device);
        int available;
        if (writePosition < readPosition ) {
            // Loop!
            available = (samples - readPosition) + writePosition;
        }else
            available =  writePosition - readPosition;

        if (available > bufferLength) {
            recorder.GetData(buffer, readPosition);
            readPosition = (readPosition + bufferLength) % samples;
            sender.Send(buffer, useEcho );
        }
    }

    void OnDestroy() {
        sender.Close();
    }
}
