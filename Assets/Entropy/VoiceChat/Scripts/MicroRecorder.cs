using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicroRecorder : MonoBehaviour {
    public static MicroRecorder Instance { get; private set; }

    string device;
    int samples;
    int bufferLength;
    AudioClip recorder;
    float[] buffer;
    VoiceSender sender;
    BaseCodec codec;

    // Start is called before the first frame update
    void Awake() {
        MicroRecorder.Instance = this;
        NTPTools.GetNetworkTime();
        codec = new SpeeX();// new RawFloats(11025 * 2);
    }


    public void Init(int userID, bool useEcho, bool useSocket=true) {
        if (Microphone.devices.Length > 0) {
            device = Microphone.devices[0];
            int currentMinFreq;
            Microphone.GetDeviceCaps(device, out currentMinFreq, out samples);
            samples = codec.recorderFrequency;
            recorder = Microphone.Start(device, true, 1, samples);
            samples = recorder.samples;

            bufferLength = codec.bufferLeght;
            buffer = new float[bufferLength];
            Debug.Log($"Using {device}  Frequency {samples} bufferLength {bufferLength} {samples}");
        }
        else
            Debug.LogError("No Micros detected.");

        sender = new VoiceSender(userID, codec, useEcho, useSocket);
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
            sender.Send(buffer);
        }
    }

    void OnDestroy() {
        if(sender!=null) sender.Close();
    }
}
