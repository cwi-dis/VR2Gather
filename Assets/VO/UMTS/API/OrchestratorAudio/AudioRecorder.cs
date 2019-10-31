using UnityEngine;

public class AudioRecorder : MonoBehaviour
{
    public void StartRecordAudio()
    {
        #if TEST_BED
        StartRecord();
        #endif
    }

    public void StopRecordAudio()
    {
        #if TEST_BED
        StopRecord();
        #endif
    }

    #if TEST_BED

    private Workers.BaseWorker reader;
    private Workers.BaseWorker codec;
    private Workers.BaseWorker writer;

    private void OnDestroy()
    {
        StopRecordAudio();
    }

    private void StartRecord()
    {
        codec = new Workers.VoiceEncoder();
        reader = new Workers.VoiceReader(this, ((Workers.VoiceEncoder)codec).bufferSize);

        writer = new Workers.SocketIOWriter(null, 0);
        reader.AddNext(codec).AddNext(writer).AddNext(reader);
        reader.token = new Workers.Token(1);
    }

    private void StopRecord()
    {
        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }
    #endif
}