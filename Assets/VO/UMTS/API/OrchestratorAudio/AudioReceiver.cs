using UnityEngine;

public class AudioReceiver : MonoBehaviour
{
    public string userID;

    public void StartListeningAudio(string pUserID)
    {
        #if TEST_BED
        StartListeningAudio(pUserID);
        #endif
    }

    public void StopListeningAudio()
    {
        #if TEST_BED
        StopListeningAudio();
        #endif
    }

    #if TEST_BED

    private Workers.BaseWorker reader;
    private Workers.BaseWorker codec;
    private Workers.AudioPreparer preparer;
    private Workers.Token token;

    private float[] tmpBuffer;

    private AudioSource audioSource;

    private void StartListening(string pUserID)
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.clip = AudioClip.Create("clip_" + pUserID, 320, 1, 16000, false);
        audioSource.loop = true;
        audioSource.Play();

        reader = new Workers.SocketIOReader(null, pUserID);
        codec = new Workers.VoiceDecoder();
        preparer = new Workers.AudioPreparer();
        reader.AddNext(codec).AddNext(preparer).AddNext(reader);
        reader.token = token = new Workers.Token();

        userID = pUserID;
    }

    private void StopListening()
    {
        reader?.Stop();
        codec?.Stop();
        preparer?.Stop();
    }

    // Buffer is filled 2.5 times per second (every 400ms). 
    private void OnAudioRead(float[] data)
    {
        if (preparer == null || !preparer.GetAudioBuffer(data, data.Length))
            System.Array.Clear(data, 0, data.Length);
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (tmpBuffer == null)
        {
            tmpBuffer = new float[data.Length];
        }
        if (preparer != null && preparer.GetAudioBuffer(tmpBuffer, tmpBuffer.Length))
        {
            int cnt = 0;
            do { data[cnt] += tmpBuffer[cnt]; } while (++cnt < data.Length);
        }
    }

    private void OnDestroy()
    {
        StopListeningAudio();
    }

#endif
}