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
    public User user;
    public bool useTCP = false;

    // Start is called before the first frame update
    void Start()
    {
        foreach(var d in Microphone.devices)
        {
            Debug.Log($"Device name: {d}");
        }
        user = new User();
        user.userId = "testAudioUser";
        user.userName = "testAudioUser";
        user.userData = new UserData();
        user.userData.microphoneName = microphoneName;
        user.userData.userAudioUrl = "tcp://127.0.0.1:9998";
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
