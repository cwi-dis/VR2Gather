using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
using VRT.UserRepresentation.Voice;
using VRT.Orchestrator.Wrapping;
using Cwipc;

public class TestAudio : MonoBehaviour
{
    public VoiceSender sender;
    public VoiceReceiver receiver;
    public string microphoneName;
    public int audioFps;
    public bool useTCP = false;
    public string audioCodec;
    public string audioUrl = "tcp://127.0.0.1:9998";
    public User user;
    
    // Start is called before the first frame update
    void Start()
    {
        foreach(var d in Microphone.devices)
        {
            Debug.Log($"Supported audio input device name: {d}");
        }
        // Copy parameters
        if (audioCodec != "") SessionConfig.Instance.voiceCodec = audioCodec;
        audioCodec = SessionConfig.Instance.voiceCodec;
        if (audioFps != 0) VRTConfig.Instance.Voice.audioFps = audioFps;
        audioFps = VRTConfig.Instance.Voice.audioFps;
        

        user = new User();
        user.userId = "testAudioUser";
        user.userName = "testAudioUser";
        user.userData = new UserData();
        user.userData.microphoneName = microphoneName;
        user.userData.userAudioUrl = audioUrl;
        QueueThreadSafe queue = new QueueThreadSafe("NullVoiceNetworkQueue", 4, true);
        if (useTCP)
        {
            SessionConfig.Instance.protocolType = SessionConfig.ProtocolType.TCP;
            sender.Init(user, "testAudio", 1000, 10000);
            receiver.Init(user, "testAudio", 0);
        } else
        {
            sender.Init(user, queue);
            receiver.Init(user, queue);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
