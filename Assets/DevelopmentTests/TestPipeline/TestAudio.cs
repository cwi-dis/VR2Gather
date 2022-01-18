using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
using VRT.UserRepresentation.Voice;
using VRT.Orchestrator.Wrapping;

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
        if (audioCodec != "") Config.Instance.audioCodec = audioCodec;
        audioCodec = Config.Instance.audioCodec;
        if (audioFps != 0) Config.Instance.audioFps = audioFps;
        audioFps = Config.Instance.audioFps;
        

        user = new User();
        user.userId = "testAudioUser";
        user.userName = "testAudioUser";
        user.userData = new UserData();
        user.userData.microphoneName = microphoneName;
        user.userData.userAudioUrl = audioUrl;
        QueueThreadSafe queue = new QueueThreadSafe("NullVoiceNetworkQueue", 4, true);
        if (useTCP)
        {
            sender.Init(user, "testAudio", 1000, 10000, VRT.Core.Config.ProtocolType.TCP);
            receiver.Init(user, "testAudio", 0, VRT.Core.Config.ProtocolType.TCP);
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
