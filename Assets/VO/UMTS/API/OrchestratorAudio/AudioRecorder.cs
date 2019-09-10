using UnityEngine;

public class AudioRecorder : MonoBehaviour
{
    private Workers.BaseWorker reader;
    private Workers.BaseWorker codec;
    private Workers.BaseWorker writer;

    private void OnDestroy()
    {
        StopRecordAudio();
    }

    public void StartRecordAudio()
    {
        codec = new Workers.VoiceEncoder();
        reader = new Workers.VoiceReader(this, ((Workers.VoiceEncoder)codec).bufferSize);

        writer = new Workers.SocketIOWriter(null, 0);
        reader.AddNext(codec).AddNext(writer).AddNext(reader);
        reader.token = new Workers.Token(1);
    }

    public void StopRecordAudio()
    {
        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }
}