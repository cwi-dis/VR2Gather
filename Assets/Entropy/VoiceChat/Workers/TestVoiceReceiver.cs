using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVoiceReceiver : MonoBehaviour
{
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.AudioPreparer preparer;


    AudioSource audioSource;

    // Start is called before the first frame update
    IEnumerator Start() {
        yield return new WaitForSeconds(2);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = AudioClip.Create("clip0", 320, 1, 16000, true, OnAudioRead);
        audioSource.loop = true;
        audioSource.Play();

        reader = new Workers.SUBReader(Config.Instance.PCs[0].AudioSUBConfig);
        codec = new Workers.VoiceDecoder();
        preparer = new Workers.AudioPreparer();

        reader.AddNext(codec).AddNext(preparer).AddNext(reader);
        reader.token = new Workers.Token();
    }

    void OnAudioRead(float[] data) {
        preparer?.GetBuffer(data, data.Length);
    }


    void OnDestroy()
    {
        reader?.Stop();
        codec?.Stop();
        preparer?.Stop();
    }
}
