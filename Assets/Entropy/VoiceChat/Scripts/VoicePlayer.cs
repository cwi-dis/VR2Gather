using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoicePlayer : MonoBehaviour {
    public VoiceReceiver    receiver { get; private set; }
    AudioSource             audioSource;
    AudioClip               audioClip;

    public void Init() {
        audioSource = gameObject.AddComponent<AudioSource>();
        receiver = new VoiceReceiver();
        audioSource.clip = AudioClip.Create("clip0", BaseCodec.Instance.bufferLeght, 1, BaseCodec.Instance.playerFrequency, true, OnAudioRead);
        audioSource.loop = true;
        audioSource.Play();
    }

    void OnAudioRead(float[] data) {
        receiver.GetBuffer(data, data.Length);
    }

}
