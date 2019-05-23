using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoicePlayer : MonoBehaviour {
    VoiceReceiver   receiver;
    AudioSource     audioSource;
    SocketIOServer fakeServer;
    AudioClip audioClip;

    void Start() {
        audioSource = GetComponent<AudioSource>();
        receiver = new VoiceReceiver(1);
        fakeServer = new SocketIOServer(receiver);
        audioClip = AudioClip.Create("clip0", 44100, 1, 44100, true, OnAudioRead, OnAudioSetPosition);

        audioSource.clip = audioClip;
        audioSource.loop = true;
        audioSource.Play();

    }


    int position;
    void OnAudioRead(float[] data)
    {
        receiver.GetBuffer(data, data.Length);
    }

    void OnAudioSetPosition(int newPosition)
    {
        position = newPosition;
    }
}
