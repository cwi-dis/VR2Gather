using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVoiceReceiver : MonoBehaviour
{
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWorker preparer;

    public SocketIOConnection socketIOConnection;
    public int                 userID;

    AudioSource audioSource;

    string nname;

    // Start is called before the first frame update
    IEnumerator Start() {
        nname = name;
        if (socketIOConnection != null) yield return socketIOConnection.WaitConnection();
        else  yield return new WaitForSeconds(2);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = AudioClip.Create("clip0", 320, 1, 16000, true, OnAudioRead);

        audioSource.loop = true;
        audioSource.Play();
        
        if (socketIOConnection == null) reader = new Workers.SUBReader(Config.Instance.PCs[0].AudioSUBConfig);
        else                            reader = new Workers.SocketIOReader(socketIOConnection, userID);
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
