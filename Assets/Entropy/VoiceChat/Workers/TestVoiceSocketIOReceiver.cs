using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVoiceSocketIOReceiver : MonoBehaviour
{
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWorker preparer;

    public SocketIOConnection socketIOConnection;
    public int                 userID;

    AudioSource audioSource;

    // Start is called before the first frame update
    IEnumerator Start() {
        yield return socketIOConnection.WaitConnection();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = AudioClip.Create("clip0", 320, 1, 16000, true, OnAudioRead);

        audioSource.loop = true;
        audioSource.Play();
                
        reader = new Workers.SocketIOReader(socketIOConnection, userID);
        codec = new Workers.VoiceDecoder();
        preparer = new Workers.AudioPreparer();
        reader.AddNext(codec).AddNext(preparer).AddNext(reader);
        reader.token = new Workers.Token();

    }


    void OnDestroy() {
        reader?.Stop();
        codec?.Stop();
        preparer?.Stop();
    }

    void OnAudioRead(float[] data) {
        if (preparer == null || !preparer.GetBuffer(data, data.Length) )
            System.Array.Clear(data, 0, data.Length);
        
    }

}
