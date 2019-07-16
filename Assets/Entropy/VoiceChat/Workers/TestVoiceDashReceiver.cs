using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVoiceDashReceiver : MonoBehaviour
{
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWorker preparer;

    Workers.Token token;

    public int                 userID;

    AudioSource audioSource;

    // Start is called before the first frame update
    IEnumerator Start() {
        
        yield return new WaitForSeconds(2);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = AudioClip.Create("clip0", 320, 1, 16000, true, OnAudioRead);

        audioSource.loop = true;
        audioSource.Play();
        
        reader = new Workers.SUBReader(Config.Instance.PCs[userID].AudioSUBConfig);
        codec = new Workers.VoiceDecoder();
        preparer = new Workers.AudioPreparer();
        reader.AddNext(codec).AddNext(preparer).AddNext(reader);
        reader.token = token = new Workers.Token();
    }

    string nname;
    void Update()
    {
        name = nname;
    }


    void OnDestroy() {
        reader?.Stop();
        codec?.Stop();
        preparer?.Stop();
    }

    void OnAudioRead(float[] data) {
        if (preparer == null || !preparer.GetBuffer(data, data.Length) )
            System.Array.Clear(data, 0, data.Length);
        if (token != null && token.latency.time != 0) nname = $"Player{userID}({ NTPTools.GetNTPTime().time - token.latency.time})";

    }

}
